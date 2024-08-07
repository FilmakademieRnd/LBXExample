using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using NaughtyAttributes;
using NetMQ;
using tracer;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Quaternion = UnityEngine.Quaternion;
using SceneManager = UnityEngine.SceneManagement.SceneManager;
using Vector3 = UnityEngine.Vector3;


public class MinoGameManager: SceneObject
{
    private const int SOMID_START_OFFSET = 300;   //so we do never interfere with the player id (0-255)
    //Debug
    public bool showPlayerColorsInPlaymodeTint = false;
    public bool logNetworkCalls = true;     //implemented to debug the Player join behaviour
    public bool logInitCalls = true;        //implemented to debug all objects init process
    public TextMesh debugLogIngameText;
    public List<LogType> debugLogTypesToShowIngame = new();

    public static bool Ingame_Debug = false;

    private RPCParameter<int> m_Debug;
    private int debugCount = 0;
    private int oldDebugCount = 0;

    public bool isDebugServer = false;
    
    // User Interface
    public GameObject tracerCore = null;

    //GameManager Singleton
    public static MinoGameManager Instance { get; private set; }
    
    [SerializeField] private GameObject minoSpectator;
    [SerializeField] private GameObject minoPlayerCharacter;
    [SerializeField] private GameObject minoNetworkCharacter;
                     public GameObject minoNetworkSpecator;
    [SerializeField] private bool isSpectator = false;
    public bool IsSpectator(){ return isSpectator; }

    #if UNITY_EDITOR
    public void EditorCallToSetSpectator(bool b){ isSpectator = b; }
    #endif

    private int spectatorSOMIDOffset = 0;

    [HideInInspector] public GameObject handplayer;

    public CalibrationVolume calibrationVolume;

    public UnityEvent startGame;
    
    [Label("Buttons")] public MinoButton[] minoButtons;
    public UnityEvent buttonEvent;
    
    public int numberPlayers{
        // Added + 1 else we would be missing ourselves
        get { 
            return GetPlayerNumber(true);
            //return m_networkCharacters.Count + 1; 
        }
    }

    private RPCParameter<int> m_startGame;
    private RPCParameter<int> m_loadSceneForPlayerJoin;
    private RPCParameter<int> m_loadSceneForSpectator;
    private RPCParameter<UnityEngine.Vector4> m_parentValuesForLaterJoin;   //to use the same parent and the offset values

    private NetworkManager m_networkManager;

    [HideInInspector] public MinoSpectator m_spectator;
    public MinoPlayerCharacter m_playerCharacter = null;
    private GameObject m_minoPlayerCharacterGO;
    public SortedDictionary<short, MinoNetworkCharacter> m_networkCharacters;
    
    // PLAYER COLORS
    public Color[] colors;

    //all SceneObjectMino objects to find via id (e.g. for the ZoneStuff)
    private Dictionary<short, SceneObjectMino> allMinoSceneObjectWithID;

    public MinoWeapon[] GetAllWeapons()
    {
        if (allMinoSceneObjectWithID != null)
        {
            var weapons = allMinoSceneObjectWithID
                .Where(d => d.Value != null && d.Value.GetComponentInChildren<MinoWeapon>() != null)
                .Select(d => d.Value.GetComponentInChildren<MinoWeapon>())
                .ToArray();

            return weapons;
        }
        else
        {
            Debug.LogError("allMinoSceneObjectWithID ist null.");
            return new MinoWeapon[0];
        }
    }


    public static bool SceneObjectsInitialized = false;     //Security check before we use the JitterHotfix

    // CLEANUP
    private byte m_deleteNetworkCharacter = 0;
    private bool sceneIsCurrentlyLoading = false;

    #region PLAYER_JOIN
    private float ourAwakeTime = 0f; //dont let other players load our state if we are player one BUT just joined!

    //i would need a Vector3Int or three ints, send our networkId and number
    public UnityEvent<int> onPlayerNumberUpdated;
    public UnityEvent onBecameMasterClient;

    public float callBecameMasterAfterInitSeconds = 5f;
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
        public GameObject networkCharGo;
        public MinoCharacter minoChar;
        public int playerNumber;
        public bool isClientSpectator;
        public Transform GetHead(){ return minoChar != null ? minoChar.head : networkCharGo.transform.GetChild(0);}

        public NetworkClientDataClass(int _networkId, GameObject _networkCharGo, int _playerNumber, bool _isClientSpectator){
            networkId = _networkId;
            networkCharGo = _networkCharGo;
            playerNumber = _playerNumber;
            isClientSpectator = _isClientSpectator;
            minoChar = networkCharGo.GetComponent<MinoCharacter>();
            if(minoChar)
                minoChar.Setup((byte)playerNumber, 253, (byte)networkId);
        }

        public int IncreasePlayerNumber(){
            if(!minoChar)    //Spectator!
                return -1;

            playerNumber++;
            minoChar.setPlayerNumber((byte)playerNumber);
            return playerNumber;
        }
        public int UpdatePlayerNumber(int _playerNumber){
            if(!minoChar)
                return -1;

            playerNumber = _playerNumber;
            minoChar.setPlayerNumber((byte)playerNumber);
            return playerNumber;
        }

        public UnityEngine.Vector2 GetV2Data(){
            return new UnityEngine.Vector2(networkId, isClientSpectator ? 1 : -1);
        }
        public Vector3 GetV3Data(){
            return new Vector3(networkId, playerNumber, isClientSpectator ? 1 : -1);
        }
    }

    private List<NetworkClientDataClass> networkClientList = new(); //first element are ourself!    
    private RPCParameter<UnityEngine.Vector2> m_sayHello;           //simply say hello with our network id and whether we are the specator to all
    private RPCParameter<Vector3> m_replyImHere;                    //reply to hello with our network id, our player # and whether we are a specator
    private RPCParameter<Vector3> m_replyIveChanged;                //network id, our # changed and if we are spec, so tell all others
    
    public List<NetworkClientDataClass> GetNetworkClientList(){ return networkClientList; }

    //already existing attendees receive this of a new client
    private void ReceivedHello(UnityEngine.Vector2 nId_spec){   //its the networkIdAndIfWeAreSpecator
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "from "+nId_spec.x));

        if(connectionState != ClientConnectionStateEnum.saidHello){
            Debug.Log("-------- IGNORE -------");
            return;
        }

        if(WeKnowThisClient((int)nId_spec.x)){
            //another call below got received first
            Debug.Log("-------- WE ALREADY KNOW HIM! -------");
            return;
        }

        //create network player
        InitiateNetworkPlayer((int)nId_spec.x, 1, nId_spec.y > 0);

        if(!AreWeOrNetworkClientSpecator((int)nId_spec.x) && DoOurPlayerNumbersMatch(1)){
            if(OurNetworkNumberIsHigher((int)nId_spec.x)){
                IncreaseOurPlayerNumberAndEmit();
            }
        }

        if(isSpectator){                                //if we are spectator
            m_spectator.InitialLookAtPlayer();
        }else{
            if(nId_spec.y > 0 && WeAreTheLowestPlayerNumberPlayer()){   //client is spectator
                StartCoroutine(SendAllActiveScenesToSpectator());
            }else if(Time.time - ourAwakeTime > 10f && WeAreTheLowestPlayerNumberPlayer(1)){
                //SEND ALL ACTIVE SCENES TO EVERYONE (... TO SPECTATOR)
                StartCoroutine(SendAllActiveScenesToEveryone());

                Debug.Log(">>>>>>>>>>> WE SEND ROOT UPDATE");
                m_parentValuesForLaterJoin.Call(m_playerCharacter.GetNetworkData());
            }
            
            //send player pos once? not neccessary in build, since people are always moving enough to send the change over the network
            //--- NO
            
            //but send current player offset if we are going down the elevator or on the balcony and parent if so
            
            //neet do start coroutine of platform parenting if we are there too
        }
        
        //reply (whom we greet, our networkId, our player number)
        Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_replyImHere"));
        m_replyImHere.Call(networkClientList[0].GetV3Data());

    }

    private void ReceivedImHere(Vector3 nId_pNr_spec){ //everyone receives this from already existing attendees
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, nId_pNr_spec.ToString()));
        if(connectionState != ClientConnectionStateEnum.saidHello){
            Debug.Log("-------- IGNORE -------");
            return;
        }
        //we should already know this attendee, but if not (e.g. if we join simultaneously), create the player and do further checks
        //if networkClientIds does not contain greetId_nId_pNr.y
        if(!WeKnowThisClient((int)nId_pNr_spec.x)){
            //create networkplayer, add greetId_nId_pNr.y to our networkPlayerIds
            //check if networkPlayerIds.z is the same as our # playerNumber
            //if so, check if ourNetworkId is > networkPlayerIds.y
                //if so, increase our # playerNumber and emit this to the network via m_replyIveChanged
            CreateNewNetworkPlayer(nId_pNr_spec);

            if(isSpectator){
                m_spectator.InitialLookAtPlayer();
            }
        }else{
            Debug.Log("-------- WE ALREADY KNOW HIM! -------");
        }
    }

    private void ReceivedIveChanged(Vector3 nId_pNr_spec){
        Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, nId_pNr_spec.ToString()));
        if(connectionState != ClientConnectionStateEnum.saidHello){
            Debug.Log("-------- IGNORE -------");
            return;
        }

        //if we do not know this client, we could a) create it b) ignore it
        if(!WeKnowThisClient((int)nId_pNr_spec.x)){
            //create networkplayer, add greetId_nId_pNr.y to our networkPlayerIds
            //check if networkPlayerIds.z is the same as our # playerNumber
            //if so, check if ourNetworkId is > networkPlayerIds.y
                //if so, increase our # playerNumber and emit this to the network via m_replyIveChanged
            CreateNewNetworkPlayer(nId_pNr_spec);
        }else{
            //updates their player number, do player number checks
            UpdatePlayerNumber((int)nId_pNr_spec.x, (int)nId_pNr_spec.y);
            
            if(!AreWeOrNetworkClientSpecator((int)nId_pNr_spec.x) && DoOurPlayerNumbersMatch((int)nId_pNr_spec.y)){
                if(OurNetworkNumberIsHigher((int)nId_pNr_spec.x)){
                    IncreaseOurPlayerNumberAndEmit();
                }else{
                    //tell oter player, that he has the same number as ours - do we an "IveCHangedEvent"
                    m_replyIveChanged.Call(networkClientList[0].GetV3Data());
                }
            }
        }
    }

    private void CreateNewNetworkPlayer(Vector3 nId_pNr_spec){
        int networkClientId = (int)nId_pNr_spec.x;
        int networkPlayerNumber = (int)nId_pNr_spec.y;
        bool isClientSpectator = nId_pNr_spec.z > 0;

        InitiateNetworkPlayer(networkClientId, networkPlayerNumber, isClientSpectator);
        
        //check if networkPlayerIds.z is the same as our # playerNumber
        if(!AreWeOrNetworkClientSpecator((int)nId_pNr_spec.x) && DoOurPlayerNumbersMatch(networkPlayerNumber)){
            //if so (do the same as below or as joining player always increase? would be more straightforward, but a very late joining player could change all players color...
            //on the other hand the first increase variant could end in problems...?)
            if(OurNetworkNumberIsHigher(networkClientId)){  //if this happens at "Hellow" and our number is smaller - none updates their player number, thats why we call this at hellow too!
                IncreaseOurPlayerNumberAndEmit();
            }
        }
    }

    private void InitiateNetworkPlayer(int networkClientId, int networkPlayerNumber, bool isClientSpectator){
        GameObject minoNetworkCharacterGO = Instantiate(isClientSpectator ? minoNetworkSpecator : minoNetworkCharacter, transform.parent);
        networkClientList.Add(new NetworkClientDataClass(networkClientId, minoNetworkCharacterGO, networkPlayerNumber, isClientSpectator));
        minoNetworkCharacterGO.GetComponent<MinoCharacter>().UpdateRigScales();

        if(minoNetworkCharacterGO.GetComponent<MinoNetworkCharacter>()){
            m_networkCharacters.Add((short)networkClientId, minoNetworkCharacterGO.GetComponent<MinoNetworkCharacter>());
            AddSOMID(minoNetworkCharacterGO.GetComponent<SceneObjectMino>(), (short)networkClientId);
            SetupButton();
        }

        //SEND 
        if(!isSpectator)
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
        m_replyIveChanged.Call(networkClientList[0].GetV3Data());
        Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_replyIveChanged: "+networkClientList[0].GetV3Data().ToString()));
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

    private bool AreWeOrNetworkClientSpecator(int networkClientId){
        if(isSpectator)
            return true;
        return IsNetworkClientSpectator(networkClientId);
    }

    private bool IsNetworkClientSpectator(int networkClientId){
        for(int x = 0; x < networkClientList.Count; x++){
            if(networkClientList[x].networkId == networkClientId){
                return networkClientList[x].isClientSpectator;
            }
        }
        return false;
    }

    private int GetPlayerNumber(bool ignoreSpectator){
        int pn = 0;
        foreach(NetworkClientDataClass client in networkClientList){
            pn += client.isClientSpectator ? 0 : 1;
        }
        return pn;
    }

    public bool WeAreTheLowestPlayerNumberPlayer(int playerNumberToIgnore = -1){   //if player 1 drops and joins later, we "are not the lowest player number"
        if(isSpectator || networkClientList == null || networkClientList.Count == 0)
            return false;

        //because it could happen we lost p1 and then a p# with 2 can be the "lowest"
        //and I dont want to send certain events (e.g. sceneload for the spectator) from every client, but only this one
        int ourPlayerNumber = networkClientList[0].playerNumber;
        int lowestPlayerNumber = ourPlayerNumber;
        foreach(NetworkClientDataClass client in networkClientList){
            if(client.isClientSpectator || client.playerNumber == playerNumberToIgnore)
                continue;
            if(client.playerNumber < lowestPlayerNumber)
                lowestPlayerNumber = client.playerNumber;
        }
        return ourPlayerNumber == lowestPlayerNumber;
    }

    public void DisableNetworkHead(int networkId){
        //if < 0, activate all
        //else activate all but deactivate this
        foreach(NetworkClientDataClass client in networkClientList){
            if(client.isClientSpectator)
                continue;
            client.GetHead().parent.Find("Avatar/Avatar_eyes_01").gameObject.SetActive(client.networkId != networkId);
            client.GetHead().parent.Find("Avatar/Avatar_head_01").gameObject.SetActive(client.networkId != networkId);
        }
    }

    //TODO if we join in any time
    //needs id, pos, rot, parents/jitterpos, enabled, etc
    
    //or maybe just force trigger all SOM to emit once? if we get an hellow any later time
    //need to update every value (also lock, if we locked in on everyone else, otherwise we need to call this on the locking player!)
    public void GlobalEvent_ReceiveStateOfAllSOM(){

    }

    //TEST
    public bool testTriggerEmitAllStates = false;
    private void TriggerEmitOfAllSOM(){

    }

    #endregion
    
    // REVIEW
    public override void Awake()
    {
        base.Awake();

        ourAwakeTime = Time.time;

        // Check if it's the only instance
        if (Instance != null && Instance != this) 
        { 
            Destroy(this); 
            return; //dont execute andy further if we destroy ourself
        } 
        else 
        { 
            Instance = this; 
        }

        #if UNITY_EDITOR
        if (showPlayerColorsInPlaymodeTint)
            SettingsHelper.PlaymodeTint = Color.Lerp(Color.white, Color.white, 0.25f);   //lerp so the tint is not too annoying
        #endif

        SceneObjectsInitialized = false;
        LogNetworkCalls.logCalls = logNetworkCalls;
        ParameterObject.Output_Log = logInitCalls;

        Setup(254, 1000);

        if (!isSpectator){
            m_minoPlayerCharacterGO = Instantiate(minoPlayerCharacter, tr.parent);
            m_playerCharacter = m_minoPlayerCharacterGO.GetComponent<MinoPlayerCharacter>();
        }else{
            GameObject minoSpectatorGO = Instantiate(minoSpectator, transform.parent);
            m_spectator = minoSpectatorGO.GetComponent<MinoSpectator>();

            spectatorSOMIDOffset = minoPlayerCharacter.GetComponentsInChildren<SceneObjectMino>().Length;
            spectatorSOMIDOffset -= minoSpectatorGO.GetComponentsInChildren<SceneObjectMino>().Length;
        }
        
        m_networkCharacters = new SortedDictionary<short, MinoNetworkCharacter>();
        m_networkManager = core.getManager<NetworkManager>();

        m_sayHello = new RPCParameter<UnityEngine.Vector2>(UnityEngine.Vector2.zero, "SayHello", this);
        m_sayHello.hasChanged += UpdateRPC;
        m_sayHello.setCall(ReceivedHello);

        m_replyImHere = new RPCParameter<Vector3>(Vector3.zero, "ReplyImHere", this);
        m_replyImHere.hasChanged += UpdateRPC;
        m_replyImHere.setCall(ReceivedImHere);

        m_replyIveChanged = new RPCParameter<Vector3>(Vector3.zero, "SayHello", this);
        m_replyIveChanged.hasChanged += UpdateRPC;
        m_replyIveChanged.setCall(ReceivedIveChanged);

        m_networkManager.networkReady += Init;
        m_networkManager.clientLost += OnClientLost;

        m_startGame = new RPCParameter<int>(0, "StartGame", this);
        m_startGame.hasChanged += UpdateRPC;
        m_startGame.setCall(StartGame);

        m_loadSceneForPlayerJoin = new RPCParameter<int>(0, "GlobalEvent_ForPlayerJoin", this);
        m_loadSceneForPlayerJoin.hasChanged += UpdateRPC;
        m_loadSceneForPlayerJoin.setCall(GlobalEvent_ForPlayerJoin);
        
        m_loadSceneForSpectator = new RPCParameter<int>(0, "GlobalEvent_LoadLevelForSpectator", this);
        m_loadSceneForSpectator.hasChanged += UpdateRPC;
        m_loadSceneForSpectator.setCall(GlobalEvent_LoadLevelForSpectator);

        m_parentValuesForLaterJoin = new RPCParameter<UnityEngine.Vector4>(UnityEngine.Vector4.zero, "GlobalEvent_CheckAndUpdateParent", this);
        m_parentValuesForLaterJoin.hasChanged += UpdateRPC;
        m_parentValuesForLaterJoin.setCall(GlobalEvent_CheckAndUpdateParent);

        m_Debug = new RPCParameter<int>(debugCount, "Debug", this);
        m_Debug.hasChanged += UpdateRPC;
        m_Debug.setCall(CallDebug);

        /*if(isDebugServer)
            StartCoroutine(CallEmitDebugRPC());*/
        connectionState = ClientConnectionStateEnum.inited;

        // User Interface
        if(tracerCore.GetComponent<Core>().showUserInterface){
            if(!isSpectator){
                m_playerCharacter.getTr.position = new Vector3(50, m_playerCharacter.getTr.position.y, m_playerCharacter.getTr.position.z); // Teleport Player to the side
                m_playerCharacter.SetRayInteractorState(true); // Activate
                LoadScene("Menu"); // Load Scene
            }else{
                tracerCore.GetComponent<Core>().showUserInterface = false;
                tracerCore.GetComponent<Core>().InitTracer();
            }
        }
    }

    private void CallDebug(int obj)
    {
        if(obj - oldDebugCount > 1)
            Debug.Log("DebugCount: " + obj + " -> PACKAGE LOST!");

        oldDebugCount = obj;

    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if(Debug.isDebugBuild || MinoGameManager.Ingame_Debug){
            Application.logMessageReceived -= LogDebuMsgsToIngame;
        }

        m_networkManager.RemoveSceneObject(this);

        m_sayHello.hasChanged -= UpdateRPC;
        m_replyImHere.hasChanged -= UpdateRPC;
        m_replyIveChanged.hasChanged -= UpdateRPC;
        
        m_networkManager.networkReady -= Init;
        m_networkManager.clientLost -= OnClientLost;
        m_startGame.hasChanged -= UpdateRPC;
    }

    private void Init(object o, EventArgs e)
    {
        if(Debug.isDebugBuild || MinoGameManager.Ingame_Debug){
            Application.logMessageReceived += LogDebuMsgsToIngame;
        }


        #if UNITY_EDITOR
        if (showPlayerColorsInPlaymodeTint)
            SettingsHelper.PlaymodeTint = Color.Lerp(Color.cyan, Color.white, 0.25f);   //lerp so the tint is not too annoying
        #endif

        InitPersistentSOM();

        byte cID = core.getManager<NetworkManager>().cID;

        if (isSpectator){
            //INIT SHIP
            UnityEngine.Object.FindObjectOfType<CalibrationVolume>().calibrationDone?.Invoke();
            Debug.Log("Spectator instantiated!");
        }else{
            
            //setup player character
            //if the cID is between above 300, it will override the current persistent SOMID
            m_playerCharacter.Setup(1, 253, cID);
            
            Debug.Log("<color=purple>CALL TO ADD OURSELF</color>");
            AddSOMID(m_playerCharacter, m_playerCharacter.id);
            
            Debug.Log("Player instantiated!");
            
            //needed for Bossbattle
            handplayer = m_playerCharacter.handPlayer;
            Debug.Log("Handplayer: " + handplayer);
            
            // needed for debugger TODO
            //debugger.transform.SetParent(m_playerCharacter.); 
        }
        
        
        //add ourself
        networkClientList.Add(new NetworkClientDataClass(cID, isSpectator ? m_spectator.gameObject : m_playerCharacter.gameObject, 1, isSpectator));
        Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_sayHello"));
        m_sayHello.Call( networkClientList[0].GetV2Data());
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
    private void UpdateRPC(object sender, UnityEngine.Vector4 input){
        emitHasChanged((AbstractParameter)sender);
    }
    

    private void OnClientLost(object sender, byte cID)
    {
        Debug.Log("Lost Client " + cID);
        m_deleteNetworkCharacter = cID;
    }

    public void ClientCleanup(){

        //delete all network characters
        foreach(KeyValuePair<short, MinoNetworkCharacter> pair in m_networkCharacters){
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
        
        sceneIsCurrentlyLoading = false;
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
            if (m_networkCharacters.TryGetValue(m_deleteNetworkCharacter, out MinoNetworkCharacter character)){

                if(isSpectator)
                    m_spectator.RemoveFromPlayerIfWeDeleteHim(character);
                
                //DONT USE DestroyImmediate
                Destroy(character.gameObject);
                m_networkCharacters.Remove(m_deleteNetworkCharacter);
            }
            allMinoSceneObjectWithID.Remove(m_deleteNetworkCharacter);
            networkClientList.RemoveAll(x => x.networkId == m_deleteNetworkCharacter);
            m_deleteNetworkCharacter = 0;
            
            SetupButton();

            if(WeAreTheLowestPlayerNumberPlayer()){
                if(!becameMasterCalled){
                    onBecameMasterClient.Invoke();
                    becameMasterCalled = true;
                }
            }else{
                becameMasterCalled = false;
            }
        }

        #if UNITY_EDITOR
        if(testTriggerEmitAllStates){
            testTriggerEmitAllStates = false;
            TriggerEmitOfAllSOM();
        }

        //Debug amount of send RPCs
        currentRPCAmount = RPCMsgSendCount;
        rpcAmountToPrint = (currentRPCAmount-lastRPCAmountWeHade);
        rpcAmountOverTheLast60FramesList.Add(rpcAmountToPrint);
        if(rpcAmountOverTheLast60FramesList.Count > 60){
            rpcAmountOverTheLast60FramesList.RemoveAt(0);
            rpcAmountOverLast60Frames = 0f;
            foreach(int i in rpcAmountOverTheLast60FramesList)
                rpcAmountOverLast60Frames += i;
            rpcAmountOverLast60Frames /= 60f;
        }
        lastRPCAmountWeHade = currentRPCAmount;

        //Debug amount of send Parameter Updates
        currentParaAmount = ParameterMsgSendCount;
        paraAmountToPrint = (currentParaAmount-lastParaAmount);
        paraAmountOverTheLast60FramesList.Add(paraAmountToPrint);
        if(paraAmountOverTheLast60FramesList.Count > 60){
            paraAmountOverTheLast60FramesList.RemoveAt(0);
            paraAmountOverLast60Frames = 0f;
            foreach(int i in paraAmountOverTheLast60FramesList)
                paraAmountOverLast60Frames += i;
            paraAmountOverLast60Frames /= 60f;
        }
        paraAmountOverTheLast600FramesList.Add(paraAmountToPrint);
        if(paraAmountOverTheLast600FramesList.Count > 600){
            paraAmountOverTheLast600FramesList.RemoveAt(0);
            paraAmountOverLast600Frames = 0f;
            foreach(int i in paraAmountOverTheLast600FramesList)
                paraAmountOverLast600Frames += i;
            paraAmountOverLast600Frames /= 600f;
        }
        lastParaAmount = currentParaAmount;

        //Debug amount of received Parameter Updates
        currentParaReceivedAmount = ParameterMsgReceivedCount;
        paraAmountReceivedToPrint = (currentParaReceivedAmount-lastParaReceivedAmount);
        paraAmountReceivedOverTheLast60FramesList.Add(paraAmountReceivedToPrint);
        if(paraAmountReceivedOverTheLast60FramesList.Count > 60){
            paraAmountReceivedOverTheLast60FramesList.RemoveAt(0);
            paraAmountReceivedOverLast60Frames = 0f;
            foreach(int i in paraAmountReceivedOverTheLast60FramesList)
                paraAmountReceivedOverLast60Frames += i;
            paraAmountReceivedOverLast60Frames /= 60f;
        }
        paraAmountReceivedOverTheLast600FramesList.Add(paraAmountReceivedToPrint);
        if(paraAmountReceivedOverTheLast600FramesList.Count > 600){
            paraAmountReceivedOverTheLast600FramesList.RemoveAt(0);
            paraAmountReceivedOverLast600Frames = 0f;
            foreach(int i in paraAmountReceivedOverTheLast600FramesList)
                paraAmountReceivedOverLast600Frames += i;
            paraAmountReceivedOverLast600Frames /= 600f;
        }
        lastParaReceivedAmount = currentParaReceivedAmount;

        #endif
    }

    #if UNITY_EDITOR
    void OnGUI(){
        GUI.Label(new Rect(Screen.width/2.5f, 10, 300, 25), rpcAmountToPrint.ToString("F2")+" RPC calls/frame ("+rpcAmountOverLast60Frames.ToString("F2")+" med/60 frames)");
        GUI.Label(new Rect(Screen.width/2.5f, 30, 300, 25), paraAmountOverLast60Frames.ToString("F2")+" send med/60 frames, "+paraAmountOverLast600Frames.ToString("F2")+" med/600frames");
        GUI.Label(new Rect(Screen.width/2.5f, 50, 300, 25), paraAmountReceivedOverLast60Frames.ToString("F2")+" received med/60 frames, "+paraAmountReceivedOverLast600Frames.ToString("F2")+" med/600frames");
    }
    private int currentRPCAmount = 0;
    private int lastRPCAmountWeHade = 0;
    private List<int> rpcAmountOverTheLast60FramesList = new();
    private float rpcAmountOverLast60Frames = 0;
    private int rpcAmountToPrint = 0;

    private int currentParaAmount, lastParaAmount = 0;
    private List<int> paraAmountOverTheLast60FramesList = new();
    private float paraAmountOverLast60Frames = 0;
    private List<int> paraAmountOverTheLast600FramesList = new();
    private float paraAmountOverLast600Frames = 0;
    private int paraAmountToPrint = 0;

    private int currentParaReceivedAmount, lastParaReceivedAmount = 0;
    private List<int> paraAmountReceivedOverTheLast60FramesList = new();
    private float paraAmountReceivedOverLast60Frames = 0;
    private List<int> paraAmountReceivedOverTheLast600FramesList = new();
    private float paraAmountReceivedOverLast600Frames = 0;
    private int paraAmountReceivedToPrint = 0;
    #endif



    private int PackIDs(byte playerNbr, byte sID, short oID)
    {
        byte[] array = new byte[]
        {
            playerNbr,
            sID,
            (byte)(oID >> (8)),
            (byte)(oID >> (0))
        };

        return BitConverter.ToInt32(array);
    }

    private (byte, byte, short) UnpackIDs(int data)
    {
        byte[] array = BitConverter.GetBytes(data);

        return (array[0], array[1], (short)((array[2] << (8)) | (array[3] << (0))));
    }

    private byte nextfreePlayer(){
        Debug.Log(LogNetworkCalls.LogFunctionFlowCall(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name));

        //BECOME PLAYER 1 IF NONE EXIST
        if (m_networkCharacters.Count == 0){
            Debug.Log("P1");
            return 1;
        }

        //GET ANY GAP OR HIGHEST PLAYER NUMBER
        short highestPlayerNumber = 1;
        List<short> allPlayerNumbers = new();
        foreach(KeyValuePair<short, MinoNetworkCharacter> pair in m_networkCharacters){
            allPlayerNumbers.Add(pair.Value.playerNumber);
            if(allPlayerNumbers[^1] > highestPlayerNumber)
                highestPlayerNumber = pair.Value.playerNumber;
        }
        //IS ANY PLAYER NUMBER MISSING UNTIL THE HIGHTEST ONE?
        for(short x = 1; x<highestPlayerNumber; x++){
            if(!allPlayerNumbers.Contains(x)){
                Debug.Log("GAP P"+x);
                return (byte)x;
            }
        }
        Debug.Log("NEW P"+(highestPlayerNumber+1));
        return (byte)(highestPlayerNumber+1);
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

        if(isSpectator)
            ParameterObject.IncreaseSIDForSpectator(spectatorSOMIDOffset);
        
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
    //receive this from player nr 1 after we'Ve connected and initialized
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
        KeyValuePair<short, SceneObjectMino> pair = allMinoSceneObjectWithID.First(x => x.Key == _somid);
        if(pair.Value != null){
            SceneObjectMino som = pair.Value;
            allMinoSceneObjectWithID.Remove(pair.Key);
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
        if(_sceneObjectMino == null || (object)_sceneObjectMino == null)    //Unity overload the ==
            return;

        if(allMinoSceneObjectWithID.Contains(new KeyValuePair<short, SceneObjectMino>(_itsSomid, _sceneObjectMino))){
            //NO NEED TO UPDATE
            Debug.Log("<color=gray>... no need to up update. "+_itsSomid+" already in dict</color>");
            return;
        }
            
        /*if(allMinoSceneObjectWithID.ContainsKey(_itsSomid) && allMinoSceneObjectWithID.ContainsValue(_sceneObjectMino)){
            //if object and id is the same - fine
            //if id exist and is another object, this will fail - we would need gather another id and update it in the networt
            //  and delete the existing object
            //Debug.Log("<color=purple>REPLACED SOM at "+_itsSomid+" with "+_sceneObjectMino.gameObject.name+"</color>");
        }*/
        
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
        CleanDebugText();
        Debug.Log(debugInitString);
    }

    public void RemoveEmptySOMIDS(){
        var badKeys = allMinoSceneObjectWithID.Where(pair => pair.Value == null).Select(pair => pair.Key).ToList();
        foreach (var badKey in badKeys){
            allMinoSceneObjectWithID.Remove(badKey);
        }
    }

    public void LogDebuMsgsToIngame(string txt, string stackTrace, LogType type){
        if((Debug.isDebugBuild || MinoGameManager.Ingame_Debug) && debugLogIngameText && (debugLogTypesToShowIngame.Count == 0 || debugLogTypesToShowIngame.Contains(type))){
            if(debugLogIngameText.text.Length+txt.Length > 15000)
                CleanDebugText();
            if(txt.Length > 15000){
                txt = "TXT CUTTED. WAS TOO LONG: "+txt.Length;
            }
            debugLogIngameText.text += "\n"+txt;
        }
    }
    public void CleanDebugText(){
        if(debugLogIngameText)
            debugLogIngameText.text = "";
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

    public void GlobalEvent_CheckAndUpdateParent(UnityEngine.Vector4 posAndID){
        //data is pos and id
        if(Time.time - ourAwakeTime > 10 || isSpectator)   //ONLY EXECUTE ON JOIN!
            return;

        //update root to that players root, if we are e.g. on the balcony!
        m_playerCharacter.UpdateNetworkDataOnPlayerOnJoin(posAndID);
    }

    public void LoadScene(string sceneName){
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public bool IsSceneUnloaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName && scene.isLoaded)
            {
                return false;
            }
        }
        return true;
    }

    public void GlobalEvent_ForPlayerJoin(int buildIndex){
        Debug.Log("<<<<<<<<<<<<<<<<<<< RECEIVED SCENE TO LOAD AT "+buildIndex);
        StartCoroutine(LoadSceneAsync(buildIndex, false));
    }

    public void GlobalEvent_LoadLevelForSpectator(int buildIndex){
        Debug.Log("<<<<<<<<<<<<<<<<<<< RECEIVED SCENE TO LOAD AT "+buildIndex);
        if (isSpectator)
            StartCoroutine(LoadSceneAsync(buildIndex, false));
    }
    
    //on player join!
    private IEnumerator SendAllActiveScenesToEveryone(){
        for(int x = 0; x<SceneManager.sceneCountInBuildSettings; x++){
            Scene loadedScene = SceneManager.GetSceneByBuildIndex(x);
            if(loadedScene.IsValid()){
                Debug.Log("----------------- EMIT SCENE TO LOAD AT "+loadedScene.buildIndex);
                m_loadSceneForPlayerJoin.Call(loadedScene.buildIndex);
                yield return new WaitForSeconds(0.25f);
            }
        }
    }

    private IEnumerator SendAllActiveScenesToSpectator(){
        for(int x = 0; x<SceneManager.sceneCountInBuildSettings; x++){
            Scene loadedScene = SceneManager.GetSceneByBuildIndex(x);
            if(loadedScene.IsValid()){
                Debug.Log("----------------- EMIT SCENE TO LOAD AT "+loadedScene.buildIndex);
                m_loadSceneForSpectator.Call(loadedScene.buildIndex);
                yield return new WaitForSeconds(0.25f);
            }
        }
    }

    //Needed overload for int, because I send an int over the network to load (string seemed not to work because of signs...)
    private IEnumerator LoadSceneAsync(int buildIndex, bool emitIntoNetwork = true){
        if(buildIndex < 0){
            Debug.LogWarning("CANNOT LOAD SCENE < 0");
            yield break;
        }
        
        //DONT LOAD MULTIPLE SCENES AT ONCE
        while(sceneIsCurrentlyLoading){
            yield return null;
        }

        //RETURNS A SCENE THAT IS ALREADY LOADED, OTHERWISE: INVALID
        Scene loadedScene = SceneManager.GetSceneByBuildIndex(buildIndex);

        if(loadedScene.IsValid()){
            //SCENE ALREADY LOADED
        }else{
            //LOAD SCENE
            sceneIsCurrentlyLoading = true;
            Application.backgroundLoadingPriority = tracerCore.GetComponent<Core>().backgroundLoadingPriority;
            AsyncOperation loadDone = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);
            while (!loadDone.isDone){
               yield return null;
            }
            loadedScene = SceneManager.GetSceneByBuildIndex(buildIndex);
            sceneIsCurrentlyLoading = false;
        }
        //SEND TO SPECTATOR
        if(emitIntoNetwork && WeAreTheLowestPlayerNumberPlayer()){
            //that will cause loading of levels on 
            Debug.Log("----------------- SET SCENE TO LOAD AT "+loadedScene.buildIndex);
            m_loadSceneForSpectator.Call(loadedScene.buildIndex);
        }

        UpdateRenderSettingsAndUnload(loadedScene.name, loadedScene);
    }

    private IEnumerator LoadSceneAsync(string sceneName, bool emitIntoNetwork = true){
        if(string.IsNullOrEmpty(sceneName)){
            Debug.LogWarning("CANNOT LOAD EMPTY SCENE");
            yield break;
        }
        //DONT LOAD MULTIPLE SCENES AT ONCE
        while(sceneIsCurrentlyLoading){
            yield return null;
        }

        //RETURNS A SCENE THAT IS ALREADY LOADED, OTHERWISE: INVALID
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);

        if(loadedScene.IsValid()){
            //SCENE ALREADY LOADED
        }else{
            //LOAD SCENE
            sceneIsCurrentlyLoading = true;
            Application.backgroundLoadingPriority = tracerCore.GetComponent<Core>().backgroundLoadingPriority;
            AsyncOperation loadDone = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!loadDone.isDone){
               yield return null;
            }
            loadedScene = SceneManager.GetSceneByName(sceneName);
            sceneIsCurrentlyLoading = false;
        }
        //SEND TO SPECTATOR
        if(emitIntoNetwork && WeAreTheLowestPlayerNumberPlayer()){
            //that will cause loading of levels on 
            Debug.Log("----------------- SET SCENE TO LOAD AT "+loadedScene.buildIndex);
            m_loadSceneForSpectator.Call(loadedScene.buildIndex);
        }

        UpdateRenderSettingsAndUnload(sceneName, loadedScene);
    }

    private void UpdateRenderSettingsAndUnload(string sceneName, Scene loadedScene){
        //CHANGES RENDER SETTINGS
        switch(sceneName){
            case "Chapter_2A":
                RenderSettings.ambientSkyColor          = Color.black;
                RenderSettings.ambientMode              = AmbientMode.Flat;
                RenderSettings.defaultReflectionMode    = DefaultReflectionMode.Custom;
                break;
            case "Chapter_3":
                RenderSettings.skybox                   = Resources.Load<Material>("Skybox_Chapter_03");
                RenderSettings.ambientSkyColor          = new Color(1f/255*43, 1f/255*22, 1f/255*23);
                RenderSettings.ambientGroundColor       = new Color(1f/255*12, 1f/255*11, 1f/255*9);
                RenderSettings.ambientEquatorColor      = new Color(1f/255*26, 1f/255*25, 1f/255*22);
                RenderSettings.ambientMode              = AmbientMode.Trilight;
                SceneManager.SetActiveScene(loadedScene);
                break;
            case "Chapter_4A":
                UnloadScene("Chapter_1");
                UnloadScene("Chapter_1_Transition");
                UnloadScene("Chapter_2A");
                UnloadScene("Chapter_2B");
                UnloadScene("Chapter_2_Distant");
                UnloadScene("Chapter_3");
                RenderSettings.skybox                   = Resources.Load<Material>("Skybox_Chapter_04A");
                RenderSettings.ambientSkyColor          = Color.white;
                RenderSettings.ambientMode              = AmbientMode.Skybox;
                RenderSettings.defaultReflectionMode    = DefaultReflectionMode.Skybox;
                SceneManager.SetActiveScene(loadedScene);
                break;
            case "Chapter_4B":
                UnloadScene("Chapter_1");
                UnloadScene("Chapter_1_Transition");
                UnloadScene("Chapter_2A");
                UnloadScene("Chapter_2B");
                UnloadScene("Chapter_2_Distant");
                UnloadScene("Chapter_3");
                RenderSettings.skybox                   = Resources.Load<Material>("Skybox_Chapter_04B");
                RenderSettings.ambientSkyColor          = Color.white;
                RenderSettings.ambientMode              = AmbientMode.Skybox;
                RenderSettings.defaultReflectionMode    = DefaultReflectionMode.Skybox;
                SceneManager.SetActiveScene(loadedScene);
                break;
            case "Chapter_5":
                UnloadScene("Chapter_1");
                UnloadScene("Chapter_1_Transition");
                UnloadScene("Chapter_2A");
                UnloadScene("Chapter_2B");
                UnloadScene("Chapter_2_Distant");
                UnloadScene("Chapter_3");
                UnloadScene("Chapter_4A");
                UnloadScene("Chapter_4B");
                SceneManager.SetActiveScene(loadedScene);
                break;
        }
        
        if (isSpectator)
            m_spectator.UpdateCameraList();
    }
    
    public void UnloadScene(string sceneName){
        if(SceneManager.GetSceneByName(sceneName).isLoaded){
            Debug.Log("<<<<< UnLoaded Scene " + sceneName);
            
            if(isSpectator) // Check if Camera is in unloaded scene before scene is getting unloaded
                m_spectator.CheckIfCameraIsInUnloadedScene(sceneName);

            SceneManager.UnloadSceneAsync(sceneName);

            //THIS WILL MOST LIKELY BE CALLED BEFORE THE SCENE IS UNLOADED
            if (isSpectator)
                m_spectator.UpdateCameraList();
        }

    }

    // Wait till HeadDome Fade Out to start Game
    public void StartGame(int input)
    {
        if (m_playerCharacter || isSpectator)
        {
            (isSpectator ? m_spectator : m_playerCharacter as CharacterOverlay)?.FadeHeadToBlack(true);
            //m_playerCharacter.FadeHeadToBlack(true);
        }

        //CorrectPlayerNumbers();

        StartCoroutine(WaitTillFadeOut(3f));
    }

    private void CorrectPlayerNumbers(){
        //SORT AND CREATE PLAYER COLORS BY PLAYER ID
        List<MinoCharacter> characters = new();
        List<int> playerNumbersWeHave = new();
        if(m_playerCharacter != null){   //null for spectator
            characters.Add(m_playerCharacter);
            playerNumbersWeHave.Add(m_playerCharacter.playerNumber);
        }
        foreach(KeyValuePair<short, MinoNetworkCharacter> pair in m_networkCharacters){
            characters.Add(pair.Value);
            playerNumbersWeHave.Add(pair.Value.playerNumber);
        }

        //CHECK IF WE HAVE DUPLICATES, IF SO - USE THIS ONE
        if(playerNumbersWeHave.Count == playerNumbersWeHave.Distinct().Count())
            return;

        characters = characters.OrderBy(c=>c.id).ToList();
        for(int x = 0; x<characters.Count; x++){
            characters[x].setPlayerNumber((byte)(x+1));
        }
    }

    private IEnumerator WaitTillFadeOut(float duration)
    {
        yield return new WaitForSeconds(duration);
        startGame.Invoke();
    }

    public void FadeIn()
    {
        (isSpectator ? m_spectator : m_playerCharacter as CharacterOverlay)?.FadeHeadToBlack(false);

        //if (m_playerCharacter != null)
            //m_playerCharacter.FadeHeadToBlack(false);
    }

    public void FadeOut()
    {
        (isSpectator ? m_spectator : m_playerCharacter as CharacterOverlay)?.FadeHeadToBlack(true);

        //if (m_playerCharacter)
            //m_playerCharacter.FadeHeadToBlack(true);
    }
    public void FadeColorWhite(bool white){
        //if(MinoGameManager.Instance.IsSpectator())
            //return;
            
        if (m_playerCharacter || isSpectator)
        {
            switch (white)
            {
                case true:
                    (isSpectator ? m_spectator : m_playerCharacter as CharacterOverlay)?.FadeHeadChangeColor(new Color(0.75f, 0.75f, 0.75f));
                    //m_playerCharacter.FadeHeadChangeColor(new Color(0.75f, 0.75f, 0.75f));
                    break;
                case false:
                    (isSpectator ? m_spectator : m_playerCharacter as CharacterOverlay)?.FadeHeadChangeColor(new Color(0f, 0f, 0f));
                    //m_playerCharacter.FadeHeadChangeColor(new Color(0f, 0f, 0f));
                    break;
            }
        }

        (isSpectator ? m_spectator : m_playerCharacter as CharacterOverlay)?.FadeHeadToBlack(true);
        //m_playerCharacter.FadeHeadToBlack(true);
    }

    public void RestoreFade()
    {
        //if(MinoGameManager.Instance.IsSpectator())
            //return;
            
        (isSpectator ? m_spectator : m_playerCharacter as CharacterOverlay)?.ActivateNormalFade();
        //m_playerCharacter.ActivateNormalFade();
    }
    
    // MinoButton state check
    public void CheckButtonStates(){
        int activeButtons = 0;

        for (int i = 0; i < minoButtons.Length; i++){
            if (minoButtons[i].active) activeButtons++;
        }
        

        if (numberPlayers * 2 == activeButtons){
            // Debug.Log("All Buttons active!");
            RedoGlobalEvent(buttonEvent, true);
        }
    }
    
    public bool CheckPlayerReadiness(){
        if(MinoGameManager.Instance.IsSpectator())
            return true;
            
        if (numberPlayers <= 1) 
            return m_playerCharacter.isReady;
        foreach (KeyValuePair<short, MinoNetworkCharacter> networkCharacter in m_networkCharacters){
            if (!networkCharacter.Value.isReady) 
                return false;
        }
        // Debug.Log("All Ready");
        return true;

    }

    public void SetHoldingWeapon(bool value)
    {
        if(!MinoGameManager.Instance.IsSpectator())
            m_playerCharacter.SetHoldingWeapon(value);
    }
    public void StartGame()
    {
        StartGame(0);
        Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_startGame"));
        m_startGame.Call(0);
        
    }

    // Deactivate Buttons dependent on PlayerAmount
    public void SetupButton(){
        //also call this on client lost and join
        foreach(MinoButton mb in minoButtons)
            mb.gameObject.SetActive(true);

        for (int i = numberPlayers*2; i < minoButtons.Length; i++){
            minoButtons[Mathf.Clamp(i,0,minoButtons.Length)].gameObject.SetActive(false);
        }            
    }

    //!
    //! OBSOLETE.
    //!
    public void SetTrackingOffsetLock(bool value)
    {
        /* OBSOLETE*/
    }

    public void ParentCharactersToObject(Transform newParent)
    {
        //m_oldParent = m_playerCharacter.getTr.parent;
        if(!isSpectator)
            m_playerCharacter.transform.SetParent(newParent);

        var characters = m_networkCharacters.Values;
        foreach (MinoNetworkCharacter character in characters)
        {
            character.transform.SetParent(newParent);
        }
    }

    public void ParentPlayerToObject(Transform newParent){
        if(!isSpectator)
            m_playerCharacter.transform.SetParent(newParent);

        //Should tell all NetworkPlayers that our parent changed and update it accordingly (would be cleaner)
    }

    public void LockButtons()
    {
            foreach (MinoButton button in minoButtons)
            {
                button.lockObject(true);
                //Debug.Log("lock buttons");
            }
    }


    public void SetPlayerMeshVisibility(bool visible)
    {
        if(MinoGameManager.Instance.IsSpectator())
            return;

        m_playerCharacter.SetMeshVisibility(true);
    }

    public void ForceReleasePlayerHands()
    {
        if(MinoGameManager.Instance.IsSpectator())
            return;
            
        m_playerCharacter.ForceRelease();
    }

    public void Vibrate(float intensity)
    {
        if(isSpectator)
            return;
            
        bool leftControllerBool = true;
        bool rightControllerBool = true;
        float duration = 0.5f;

        m_playerCharacter.GetComponent<MinoVibrationManager>().TriggerVibration(intensity, leftControllerBool, rightControllerBool, duration);
    }

    public interface CharacterOverlay
    {
        void FadeHeadToBlack(bool fadeOut);
        void FadeHeadChangeColor(Color color);
        void ActivateNormalFade();
    }

    #region SIGGRAPH HOTFIXING
    public void RedoGlobalEvent(UnityEvent e, bool triggerInstantly = true){
        if(e != null){
            e.Invoke();
            StartCoroutine(RedoGlobalEventCoro(e));
        }
    }

    /// <summary>
    /// Spams specific global events into the network
    /// does not regtrigger our own events and also not on clients that already executed them!
    /// - waits for a specific amount of frames, fires, waits again
    /// - setting x<10: waits 3,4,5,6,7,8,9,10,11 = overall 63 Frames (can be 1-2 or 3 seconds)
    /// </summary>
    /// <param name="e">this invokes several MinoGlobalEvent or MinoGlobalTrigger</param>
    /// <returns></returns>
    private IEnumerator RedoGlobalEventCoro(UnityEvent e){
        int waitForFrames = 3;
        
        for(int x = 0; x < 10; x++){
            for(int frame = 0; frame<waitForFrames; frame++)
                yield return new WaitForEndOfFrame();

            waitForFrames++;
            e?.Invoke();    
        }
    }
    #endregion
}

