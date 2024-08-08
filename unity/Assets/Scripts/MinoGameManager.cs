/*
-----------------------------------------------------------------------------------
TRACER Location Based Experience Example

Copyright (c) 2024 Filmakademie Baden-Wuerttemberg, Animationsinstitut R&D Labs
https://github.com/FilmakademieRnd/LBXExample

TRACER Location Based Experience Example is a development by Filmakademie 
Baden-Wuerttemberg, Animationsinstitut R&D Labs in the scope of the EU funded 
project EMIL (101070533).

This program is distributed in the hope that it will be useful, but WITHOUT
ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the MIT License for more details.
You should have received a copy of the MIT License along with this program;
if not go to https://opensource.org/licenses/MIT
-----------------------------------------------------------------------------------
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using tracer;
using UnityEngine;
using UnityEngine.Events;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;


public class MinoGameManager: SceneObject
{
    private const int SOMID_START_OFFSET = 300;   //so we do never interfere with the player id (0-255)
    
    public static bool SceneObjectsInitialized = false;

    //Debug
    public bool logNetworkCalls = true;     //implemented to debug the Player join behaviour
    public bool logInitCalls = true;        //implemented to debug all objects init process
    // PLAYER COLORS
    public Color[] colors;

    //GameManager Singleton
    public static MinoGameManager Instance { get; private set; }
    
    [SerializeField] private GameObject playerCharacter;
    
    public int numberPlayers{
        get { 
            return GetPlayerNumber();
        }
    }

    private NetworkManager m_networkManager;
    private MinoCharacter m_playerCharacter = null;


    public MinoCharacter GetPlayer(){ return m_playerCharacter; }

    public SortedDictionary<short, MinoCharacter> m_networkCharacters;

    private Dictionary<short, SceneObjectMino> allMinoSceneObjectWithID;    //all SceneObjectMino objects to find via id

    // CLEANUP
    private byte m_deleteNetworkCharacter = 0;

    #region PLAYER_JOIN
    [HideInInspector] public UnityEvent<int> onPlayerNumberUpdated;
    [HideInInspector] public UnityEvent onBecameMasterClient;

    private float callBecameMasterAfterInitSeconds = 3f;
    private bool becameMasterCalled = false;

    public int GetOurPlayerNumber(){ return networkClientList[0].playerNumber; }

    public bool AreWeMaster(){ return becameMasterCalled; }
    
    public enum ClientConnectionStateEnum{
        notInited = 0,
        inited = 5,
        saidHello = 10
    }
    private ClientConnectionStateEnum connectionState = ClientConnectionStateEnum.notInited;

    [System.Serializable]
    public class NetworkClientDataClass{
        public int networkId;   //ip adress number. important for pair check on same number who should update (the higher one)
        public MinoCharacter minoChar;
        public int playerNumber;
        public bool isReplicate;
        public Transform GetHead(){ return minoChar.transform;}

        public NetworkClientDataClass(int _networkId, GameObject _networkCharGo, int _playerNumber, bool _isReplicated = true){
            networkId = _networkId;
            isReplicate = _networkCharGo;
            playerNumber = _playerNumber;
            minoChar = _networkCharGo.GetComponent<MinoCharacter>();
            isReplicate = _isReplicated;
            if(isReplicate){
                minoChar.IsReplicate();
                minoChar.Setup((byte)playerNumber, 253, (byte)networkId);
            }
        }

        public int IncreasePlayerNumber(){
            playerNumber++;
            minoChar.setPlayerNumber((byte)playerNumber);
            return playerNumber;
        }
        public int UpdatePlayerNumber(int _playerNumber){
            playerNumber = _playerNumber;
            minoChar.setPlayerNumber((byte)playerNumber);
            return playerNumber;
        }

        public Vector2 GetV2Data(){
            return new Vector2(networkId, playerNumber);
        }
    }

    private List<NetworkClientDataClass> networkClientList = new(); //first element are ourself!    
    private RPCParameter<int> m_sayHello;                           //simply say hello with our network id and whether we are the specator to all
    private RPCParameter<Vector2> m_replyImHere;                    //reply to hello with our network id, our player # and whether we are a specator
    private RPCParameter<Vector2> m_replyIveChanged;                //network id, our # changed and if we are spec, so tell all others
    
    public List<NetworkClientDataClass> GetNetworkClientList(){ return networkClientList; }

    //already existing attendees receive this of a new client
    private void ReceivedHello(int networkId){   //its the networkIdAndIfWeAreSpecator
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "from "+networkId));

        if(connectionState != ClientConnectionStateEnum.saidHello){
            Debug.Log("-------- IGNORE -------");
            return;
        }

        if(WeKnowThisClient(networkId)){
            //another call below got received first
            Debug.Log("-------- WE ALREADY KNOW HIM! -------");
            return;
        }

        //create network player
        InitiateNetworkPlayer(networkId, 1);

        if(DoOurPlayerNumbersMatch(1)){
            if(OurNetworkNumberIsHigher(networkId)){
                IncreaseOurPlayerNumberAndEmit();
            }
        }
        
        //reply (whom we greet, our networkId, our player number)
        Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_replyImHere"));
        m_replyImHere.Call(networkClientList[0].GetV2Data());

    }

    private void ReceivedImHere(Vector2 nId_pNr){ //everyone receives this from already existing attendees
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, nId_pNr.ToString()));
        if(connectionState != ClientConnectionStateEnum.saidHello){
            Debug.Log("-------- IGNORE -------");
            return;
        }
        //we should already know this attendee, but if not (e.g. if we join simultaneously), create the player and do further checks
        //if networkClientIds does not contain greetId_nId_pNr.y
        if(!WeKnowThisClient((int)nId_pNr.x)){
            //create networkplayer, add greetId_nId_pNr.y to our networkPlayerIds
            //check if networkPlayerIds.z is the same as our # playerNumber
            //if so, check if ourNetworkId is > networkPlayerIds.y
                //if so, increase our # playerNumber and emit this to the network via m_replyIveChanged
            CreateNewNetworkPlayer(nId_pNr);
        }else{
            Debug.Log("-------- WE ALREADY KNOW HIM! -------");
        }
    }

    private void ReceivedIveChanged(Vector2 nId_pNr){
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, nId_pNr.ToString()));
        if(connectionState != ClientConnectionStateEnum.saidHello){
            Debug.Log("-------- IGNORE -------");
            return;
        }

        //if we do not know this client, we could a) create it b) ignore it
        if(!WeKnowThisClient((int)nId_pNr.x)){
            //create networkplayer, add greetId_nId_pNr.y to our networkPlayerIds
            //check if networkPlayerIds.z is the same as our # playerNumber
            //if so, check if ourNetworkId is > networkPlayerIds.y
                //if so, increase our # playerNumber and emit this to the network via m_replyIveChanged
            CreateNewNetworkPlayer(nId_pNr);
        }else{
            //updates their player number, do player number checks
            UpdatePlayerNumber((int)nId_pNr.x, (int)nId_pNr.y);
            
            if(DoOurPlayerNumbersMatch((int)nId_pNr.y)){
                if(OurNetworkNumberIsHigher((int)nId_pNr.x)){
                    IncreaseOurPlayerNumberAndEmit();
                }else{
                    //tell oter player, that he has the same number as ours - do we an "IveCHangedEvent"
                    m_replyIveChanged.Call(networkClientList[0].GetV2Data());
                }
            }
        }
    }

    private void CreateNewNetworkPlayer(Vector2 nId_pNr){
        int networkClientId = (int)nId_pNr.x;
        int networkPlayerNumber = (int)nId_pNr.y;

        InitiateNetworkPlayer(networkClientId, networkPlayerNumber);
        
        //check if networkPlayerIds.z is the same as our # playerNumber
        if(DoOurPlayerNumbersMatch(networkPlayerNumber)){
            //if so (do the same as below or as joining player always increase? would be more straightforward, but a very late joining player could change all players color...
            //on the other hand the first increase variant could end in problems...?)
            if(OurNetworkNumberIsHigher(networkClientId)){  //if this happens at "Hellow" and our number is smaller - none updates their player number, thats why we call this at hellow too!
                IncreaseOurPlayerNumberAndEmit();
            }
        }
    }

    private void InitiateNetworkPlayer(int networkClientId, int networkPlayerNumber){
        GameObject minoNetworkCharacterGO = Instantiate(playerCharacter, transform.parent);
        networkClientList.Add(new NetworkClientDataClass(networkClientId, minoNetworkCharacterGO, networkPlayerNumber));

        if(networkClientList[^1].isReplicate){
            m_networkCharacters.Add((short)networkClientId, minoNetworkCharacterGO.GetComponent<MinoCharacter>());
            AddSOMID(minoNetworkCharacterGO.GetComponent<SceneObjectMino>(), (short)networkClientId);
        }

        //SEND 
        m_playerCharacter.EmitCurrentNetworkDataOnPlayerJoin();
    }

    private bool DoOurPlayerNumbersMatch(int networkPlayerNumber){
        return networkClientList[0].playerNumber == networkPlayerNumber;
    }
    private bool OurNetworkNumberIsHigher(int networkClientId){
        return networkClientList[0].networkId > networkClientId;
    }

    private void IncreaseOurPlayerNumberAndEmit(){
        if(networkClientList[0].IncreasePlayerNumber() >= 0)
            onPlayerNumberUpdated.Invoke(GetOurPlayerNumber());
        m_replyIveChanged.Call(networkClientList[0].GetV2Data());
        Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_replyIveChanged: "+networkClientList[0].GetV2Data().ToString()));
    }

    private bool WeKnowThisClient(int networkClientId){
        for(int x = 1; x < networkClientList.Count; x++){
            if(networkClientList[x].networkId == networkClientId)
                return true;
        }
        return false;
    }

    private void UpdatePlayerNumber(int networkClientId, int networkPlayerNumber){
        for(int x = 1; x < networkClientList.Count; x++){
            if(networkClientList[x].networkId == networkClientId){
                networkClientList[x].UpdatePlayerNumber(networkPlayerNumber);
                return;
            }
        }
    }

    private int GetPlayerNumber(){
        int pn = 0;
        foreach(NetworkClientDataClass client in networkClientList){
            pn += 1; 
        }
        return pn;
    }

    public bool WeAreTheLowestPlayerNumberPlayer(int playerNumberToIgnore = -1){   //if player 1 drops and joins later, we "are not the lowest player number"
        if(networkClientList == null || networkClientList.Count == 0)
            return false;

        //because it could happen we lost p1 and then a p# with 2 can be the "lowest"
        //and I dont want to send certain events (e.g. sceneload for the spectator) from every client, but only this one
        int ourPlayerNumber = networkClientList[0].playerNumber;
        int lowestPlayerNumber = ourPlayerNumber;
        foreach(NetworkClientDataClass client in networkClientList){
            if(client.playerNumber == playerNumberToIgnore)
                continue;
            if(client.playerNumber < lowestPlayerNumber)
                lowestPlayerNumber = client.playerNumber;
        }
        return ourPlayerNumber == lowestPlayerNumber;
    }
    #endregion
    
    // REVIEW
    public override void Awake(){
        base.Awake();

        // Check if it's the only instance
        if (Instance != null && Instance != this) { 
            Destroy(this); 
            return;
        } 
        else
            Instance = this; 
        

        SceneObjectsInitialized = false;
        LogNetworkCalls.logCalls = logNetworkCalls;
        ParameterObject.Output_Log = logInitCalls;

        Setup(254, 1000);

        m_playerCharacter = Instantiate(playerCharacter, tr.parent).GetComponent<MinoCharacter>();
        
        m_networkCharacters = new SortedDictionary<short, MinoCharacter>();
        m_networkManager = core.getManager<NetworkManager>();

        m_sayHello = new RPCParameter<int>(-1, "SayHello", this);
        m_sayHello.hasChanged += UpdateRPC;
        m_sayHello.setCall(ReceivedHello);

        m_replyImHere = new RPCParameter<Vector2>(Vector2.zero, "ReplyImHere", this);
        m_replyImHere.hasChanged += UpdateRPC;
        m_replyImHere.setCall(ReceivedImHere);

        m_replyIveChanged = new RPCParameter<Vector2>(Vector2.zero, "SayHello", this);
        m_replyIveChanged.hasChanged += UpdateRPC;
        m_replyIveChanged.setCall(ReceivedIveChanged);

        m_networkManager.networkReady += Init;
        m_networkManager.clientLost += OnClientLost;

        connectionState = ClientConnectionStateEnum.inited;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        m_networkManager.RemoveSceneObject(this);

        m_sayHello.hasChanged -= UpdateRPC;
        m_replyImHere.hasChanged -= UpdateRPC;
        m_replyIveChanged.hasChanged -= UpdateRPC;
        
        m_networkManager.networkReady -= Init;
        m_networkManager.clientLost -= OnClientLost;
    }

    private void Init(object o, EventArgs e){
        InitPersistentSOM();

        byte cID = core.getManager<NetworkManager>().cID;

       //setup player character
        //if the cID is between above 300, it will override the current persistent SOMID
        m_playerCharacter.Setup(1, 253, cID);
        
        Debug.Log("<color=purple>CALL TO ADD OURSELF</color>");
        AddSOMID(m_playerCharacter, m_playerCharacter.id);
        
        Debug.Log("Player instantiated!");
        
        //add ourself
        networkClientList.Add(new NetworkClientDataClass(cID, m_playerCharacter.gameObject, 1, false));
        Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_sayHello"));
        m_sayHello.Call( networkClientList[0].networkId );
        connectionState = ClientConnectionStateEnum.saidHello; 

        StartCoroutine(MasterClientDelayCheck());
    }

    private void UpdateRPC(object sender, int input){
        emitHasChanged((AbstractParameter)sender);
    }
    private void UpdateRPC(object sender, UnityEngine.Vector2 input){
        emitHasChanged((AbstractParameter)sender);
    }
    private void UpdateRPC(object sender, Vector3 input){
        emitHasChanged((AbstractParameter)sender);
    }
    

    private void OnClientLost(object sender, byte cID)
    {
        Debug.Log("Lost Client " + cID);
        m_deleteNetworkCharacter = cID;
    }

    public void ClientCleanup(){

        //delete all network characters
        foreach(KeyValuePair<short, MinoCharacter> pair in m_networkCharacters){
            if(pair.Value != null){
                #if UNITY_EDITOR
                //if we exit the playmode
                DestroyImmediate(pair.Value.gameObject);
                #else
                Destroy(pair.Value.gameObject);
                #endif
            }
            allMinoSceneObjectWithID.Remove(pair.Key);
        }
        m_networkCharacters.Clear();

        networkClientList.Clear();
    }

    private IEnumerator MasterClientDelayCheck(){
        float t = callBecameMasterAfterInitSeconds;
        while(t > 0f && !becameMasterCalled){
            t -= Time.deltaTime;
            yield return null;
        }
        if(!becameMasterCalled && WeAreTheLowestPlayerNumberPlayer()){
            onBecameMasterClient.Invoke();
            becameMasterCalled = true;
        }
    }


    protected override void Update(){
        if (m_deleteNetworkCharacter > 0){
            if (m_networkCharacters.TryGetValue(m_deleteNetworkCharacter, out MinoCharacter character)){

                //DONT USE DestroyImmediate
                Destroy(character.gameObject);
                m_networkCharacters.Remove(m_deleteNetworkCharacter);
            }
            allMinoSceneObjectWithID.Remove(m_deleteNetworkCharacter);
            networkClientList.RemoveAll(x => x.networkId == m_deleteNetworkCharacter);
            m_deleteNetworkCharacter = 0;

            if(WeAreTheLowestPlayerNumberPlayer()){
                if(!becameMasterCalled){
                    onBecameMasterClient.Invoke();
                    becameMasterCalled = true;
                }
            }else{
                becameMasterCalled = false;
            }
        }
    }

    /// <summary>
    /// Grab all SceneObjectMino at Chapter0, sort them by their new GUID and init them
    /// Caveat: it these objects are not persistent over the instances, it will break (thats why the specator fix is there for now)
    /// </summary>
    private void InitPersistentSOM(){
        allMinoSceneObjectWithID = new Dictionary<short, SceneObjectMino>();

        //SORT THEM ALL BY WORLD-POS (PSEUDO PERSISTENT, THOUGH)
        SceneObjectMino[] allSceneObjectMino = UnityEngine.Object.FindObjectsOfType<SceneObjectMino>(true);
        allSceneObjectMino = allSceneObjectMino.OrderBy(x => x.ourGUID).ToArray();

        string debugInitString = "<color=green>INIT PERSISTENT SOM</color>";
        
        foreach(SceneObjectMino som in allSceneObjectMino){
            if (som && som.id == 0){
                som.Init(254, (short)(ParameterObject.getSoID()+SOMID_START_OFFSET));
                allMinoSceneObjectWithID.Add(som.id, som);
                debugInitString += "\n" + som.id + "\tid inited "+som.GetType().ToString()+" at " + som.name;
            }
        }
        SceneObjectsInitialized = true;
        Debug.Log(debugInitString);
    }

    //not implemented yet
    //receive this from player nr 1 after we've connected and initialized
    public void UpdateSOMIDsViaGUID(Dictionary<string, short> guidSomIds){
        //update ids if guid string matches
        //skip not available guids (e.g. spectator has no starting player prefab)
    }

    public int AddObjectAndInit_Sender(SceneObjectMino _toSpawn, Vector3 _spawnPos){
        //SPAWN AND RETURN ID!
        GameObject spawnGo = Instantiate(_toSpawn.gameObject, _spawnPos, Quaternion.identity);
        SceneObjectMino so = spawnGo.GetComponent<SceneObjectMino>();
        so.Init(254, (short)(ParameterObject.getSoID()+SOMID_START_OFFSET));
        //no "need" to lock, since it does not exist on any other client
        so.lockObjectLocal(false);
        AddSOMID(so, so.id);
        return so.id;
    }

    //if an object is added, initialize it with these
    public void AddObjectAndInit_Receiver(SceneObjectMino _toSpawn, int _id, Vector3 startPos){
        //have a persistent list of (scene object mino) objects that _could_ be initialized
        //if one player does so, send the new guid, its somid and the index of the list
        //instantiate the prefab/object and init it with the specific values
        GameObject spawnGo = Instantiate(_toSpawn.gameObject, startPos, Quaternion.identity);
        SceneObjectMino so = spawnGo.GetComponent<SceneObjectMino>();
        so.Init(254, (short)_id);
        so.lockObjectLocal(true);
        AddSOMID(so, so.id);

        //also we need to update the static value in case we will spawn an object
        ParameterObject.increaseSoID();
    }

    public Vector3 RemoveAndDeleteSOMID(int _somid){
        if(allMinoSceneObjectWithID.TryGetValue((short)_somid, out SceneObjectMino som)){
            allMinoSceneObjectWithID.Remove((short)_somid);
            Vector3 objPos = som.transform.position;
            Destroy(som.gameObject);
            return objPos;
        }
        return Vector3.zero;
    }


    //right now used for player itself and networkplayers
    public void AddSOMID(SceneObjectMino _sceneObjectMino, short _itsSomid){
        //When we start the game, our playerchar will have the same id on every client
        //therefore, we need to wiggle the ids
        //p1 starts, player char gets somid (e.g. 5)
        //p2 starts, player gets same somid, receives update from p1 (ReplicatePlayer) so update allMinoSceneObjectWithID
        //if we already contain the key (e.g. 5), link it with the new network player
        if(_sceneObjectMino == null || (object)_sceneObjectMino == null)    //Unity overloads the ==
            return;

        if(allMinoSceneObjectWithID.Contains(new KeyValuePair<short, SceneObjectMino>(_itsSomid, _sceneObjectMino))){
            //NO NEED TO UPDATE
            Debug.Log("<color=gray>... no need to up update. "+_itsSomid+" already in dict</color>");
            return;
        }
            
        if(allMinoSceneObjectWithID.ContainsKey(_itsSomid)){
            Debug.Log("<color=purple>REPLACED SOM at "+_itsSomid+" with "+_sceneObjectMino.gameObject.name+"</color>");
            allMinoSceneObjectWithID[_itsSomid] = _sceneObjectMino;
        }else if(allMinoSceneObjectWithID.ContainsValue(_sceneObjectMino)){
            Debug.Log("<color=purple>DELETE and ADD SOM at "+_sceneObjectMino.gameObject.name+" with"+_itsSomid+"</color>");
            //delete and add
            allMinoSceneObjectWithID.Remove(allMinoSceneObjectWithID.FirstOrDefault(x => x.Value == _sceneObjectMino).Key);
            allMinoSceneObjectWithID.Add(_itsSomid, _sceneObjectMino);
        }else{
            Debug.Log("<color=purple>ADD SOMID with "+_itsSomid+" and "+_sceneObjectMino.gameObject.name+"</color>");
            allMinoSceneObjectWithID.Add(_itsSomid, _sceneObjectMino);
        }

        string debugInitString = "<color=green>UPDATED PERSISTENT SOM</color>";
        foreach(KeyValuePair<short, SceneObjectMino> pair in allMinoSceneObjectWithID){
            debugInitString += "\n" + pair.Key + "\tid inited "+pair.Value.GetType().ToString()+" at " + pair.Value.gameObject.name;
        }

        if(logInitCalls)
            Debug.Log(debugInitString);
    }

    public void RemoveEmptySOMIDS(){
        var badKeys = allMinoSceneObjectWithID.Where(pair => pair.Value == null).Select(pair => pair.Key).ToList();
        foreach (var badKey in badKeys){
            allMinoSceneObjectWithID.Remove(badKey);
        }
    }

    public SceneObjectMino GetSceneObjectViaID(short id){
        SceneObjectMino value;
        allMinoSceneObjectWithID.TryGetValue(id, out value);
        return value;
    }

    public short GetIdViaSceneObject(SceneObjectMino som){
        if(!som)
            return -1;
        return allMinoSceneObjectWithID.FirstOrDefault(x => x.Value == som).Key;
    }

}

