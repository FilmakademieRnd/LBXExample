using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tracer;
//using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class MinoPlatformManager : SceneObjectMino{
    //Review
    private RPCParameter<int> m_stopTimelines;
    private RPCParameter<int> m_startAllTimers;

    private Parameter<float> m_timelineTime;
    private Parameter<float> m_secondTimelineTime;

    public MinoGlobalEvent globalEventToParentPlayer;
    public List<MinoPlatform> platforms = new List<MinoPlatform>();
    [SerializeField] private List<MinoPlatform> _collectedPlatforms = new List<MinoPlatform>();
    [SerializeField] private int platformIndex = 0;
    private PlayableDirector _director;
    public Transform finalHeight;
    public float timerValue = 10;
    private bool timerStarted = false;
    public float restartTime = 7.4333f;
    public float varyTime = 1;
    public PlayableDirector _storyDirector;
    public float idleTime;
    public float fallTime;
    public float finalTime;
    public UnityEvent playSoundEvent;

    public UnityEvent winstateEvent;

    private bool isMasterExecutioneer = false;

    public bool IsMaster(){ return isMasterExecutioneer; }

    public override void Awake()
    {
        base.Awake();
        m_timelineTime = new Parameter<float>(0, "timelineTime", this);
        m_timelineTime.hasChanged += UpdateTimelineTime;

        m_secondTimelineTime = new Parameter<float>(0, "timelineTime", this);
        m_secondTimelineTime.hasChanged += UpdateSecondTimelineTime;

        m_stopTimelines = new RPCParameter<int>(0, "stopTimelines", this);
        m_stopTimelines.hasChanged += StopTimelines;
        m_stopTimelines.setCall(StopOtherTimelines);

        m_startAllTimers = new RPCParameter<int>(0, "timerFinished", this);
        m_startAllTimers.hasChanged += StartAllTimers;
        m_startAllTimers.setCall(SetStartAllTimers);
    }

    private void SetStartAllTimers(int obj)
    {
        timerStarted = true;
    }

    private void StartAllTimers(object sender, int e)
    {
        emitHasChanged((AbstractParameter)sender);
    }


    private void StopOtherTimelines(int obj)
    {
        _director.Stop();
        Debug.Log("Timeline STOPPED");
    }

    private void StopTimelines(object sender, int e)
    {
        emitHasChanged((AbstractParameter)sender);
    }

    private void UpdateTimelineTime(object sender, float e)
    {
        _director.time = e;
        emitHasChanged((AbstractParameter)sender);
        //Debug.Log("Update Timeline: " + e);
    }

    private void UpdateSecondTimelineTime(object sender, float e)
    {
        _storyDirector.time = e;
        emitHasChanged((AbstractParameter)sender);
        _storyDirector.Play();
    }

    void Start()
    {
        GetComponentsInChildren(platforms);

        /*foreach (MinoPlatform obj in platforms)
        {
            obj.platformManager = this;
        }*/
        
        for (int i = 0; i < platforms.Count; i++)
        {
            platforms[i].platformManager = this;
            
        }

        _director = GetComponent<PlayableDirector>();
    }

    private void Update()
    {
        // DEBUG ONLY
        ////////////////////////////////////////
        #if UNITY_EDITOR
        if(true){
        #else
        if(Debug.isDebugBuild || MinoGameManager.Ingame_Debug){
        #endif
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.T)){
                ChangeState();
            }

            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D)){
                MoveToEndDestination();
            }
        }
        ////////////////////////////////////////

        if (timerStarted && timerValue > 0)
        {
            timerValue -= Time.deltaTime;
        }

    }

    public void ChangeState()
    {
        MakePlatformsUnavailable();

        if (timerValue > 0)
            _director.Play();
        else
        {
            MoveToEndDestination();
        }
    }

    public void MakePlatformsAvailable(){
        SetPlatformAvailability(true);
    }

    private void SetPlatformAvailability(bool isAvailable){
        for (int i = 0; i < platforms.Count; i++){
            platforms[i].available = isAvailable;
            platforms[i].lockObject(!isAvailable);  //we should have always locked the positions on our master, but enable the triggers...
        }
    }

    public void MakePlatformsUnavailable(){
        playSoundEvent?.Invoke();
        
        SetPlatformAvailability(false);
    }


    /// <summary>
    /// Public so we can call it via MinoGlobalEvent on every player
    /// ATTENTION: it _could_ be possible that a local player has another parent as at the other players
    /// because it could stand slightly somewhere else
    /// </summary>
    public void ParentPlayersToNearestPlatform()
    {
        if(MinoGameManager.Instance.IsSpectator())
            return;

        MinoPlayerCharacter player = MinoGameManager.Instance.m_playerCharacter;
        Transform closestParent = GetClosestObjectForParenting(new Vector3(player.headTarget.position.x, player.headTarget.position.y - player.playerHeight,
                player.head.position.z), player.getTr.parent);

        if(closestParent == player.getTr){
            //Still check, because, why not!
            player.ExecuteNeverBelowOrAboveParentCheck();
            return;
        }

        player.getTr.SetParent(closestParent);

        player.ExecuteNeverBelowOrAboveParentCheck();

        // if(MinoGameManager.Instance.debugSystem.debugMode){
        //     //HEAVILIY VISUALIZE THE PARENT!
        //     StartCoroutine(HeavyParentVisualization(player.getTr.parent));
        // }

        //instead make global trigger repeatable!
        //StartCoroutine(ResetIsUsedOnGlobalParentingTrigger());
    }

    private IEnumerator HeavyParentVisualization(Transform parentTr){
        float t = 0f;
        Transform sphereViz = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<Transform>();
        sphereViz.parent = parentTr;
        sphereViz.localPosition = Vector3.zero;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = sphereViz.localScale*8f;
        if(Shader.Find("Unlit/Color") != null)
            sphereViz.GetComponent<MeshRenderer>().material.shader = Shader.Find("Unlit/Color");

        sphereViz.GetComponent<MeshRenderer>().material.color = Color.red;
        while(t<1f){
            t += Time.deltaTime/4f;
            sphereViz.localScale = Vector3.Lerp(startScale, endScale, Mathf.PingPong(t*2f, 1f));
            yield return null;
        }
        Destroy(sphereViz.gameObject);
        
    }


    private Transform GetClosestObjectForParenting(Vector3 pos, Transform oldTransform)
    {
        float dist = Mathf.Infinity;
        Transform closestObj = oldTransform; //dont use this, return old parent instead new GameObject().transform;

        for (int i = 0; i < platforms.Count; i++){
            float currentDist = Vector3.Distance(pos, platforms[i].getTr.position);

            if (currentDist < dist){
                dist = currentDist;
                closestObj = platforms[i].getTr;
                // Debug.Log("Closest Object (Distance) is: " + closestObj + " (" + dist + ")");
            }
        }

        return closestObj;
    }

    /// <summary>
    /// not implemented yet, may be nec if we choose to kill the mino at the end and need the elevator platform
    /// </summary>
    /// <param name="parentTarget"></param>
    public void ParentAllPlayersToSpecificTransform(Transform parentTarget)
    {
        if(!MinoGameManager.Instance.IsSpectator())
            MinoGameManager.Instance.m_playerCharacter.getTr.SetParent(parentTarget);
    }

    public void MoveToEndDestination()
    {

        if (platformIndex >= 8)
        {
            if (_collectedPlatforms[8].getTr.position.y != finalHeight.position.y)
            {
                Debug.Log("Final Move");
                //Coroutine instead of async!
                //MovePlatformToNextPlatform(platforms[platformIndex].getTr.position.y, finalHeight.position.y);
                StartCoroutine(MovePlatformToNextPlatform(platforms[platformIndex].getTr.position.y, finalHeight.position.y));

            }

            return;
        }

        SortList();
        if (_collectedPlatforms.Count == 0)
            _collectedPlatforms.Add(platforms[platformIndex]);

        if (platforms[platformIndex].getTr.position.y == platforms[platformIndex + 1].getTr.position.y)
        {
            platformIndex++;
            _collectedPlatforms.Add(platforms[platformIndex]);
            MoveToEndDestination();
        }
        else
        {
            //Coroutine instead of async!
            //MovePlatformToNextPlatform(platforms[platformIndex].getTr.position.y,platforms[platformIndex + 1].getTr.position.y);
            StartCoroutine(MovePlatformToNextPlatform(platforms[platformIndex].getTr.position.y,platforms[platformIndex + 1].getTr.position.y));
        }
    }

    private void SortList()
    {
        platforms = platforms.OrderBy(pl => pl.getTr.position.y).ToList();
    }

    private IEnumerator /*async void*/ MovePlatformToNextPlatform(float platform1y, float platform2y){
        
        float elapsedTime = 0;
        float timeMult = Mathf.Abs(Mathf.Max(platform1y, platform2y) / Mathf.Min(platform1y, platform2y)) * varyTime;

        while (elapsedTime <= 1){

            for (int i = 0; i < _collectedPlatforms.Count; i++)
            {
                platforms[i].getTr.position = new Vector3( platforms[i].getTr.position.x,
                    Mathf.Lerp(platform1y, platform2y, elapsedTime),  platforms[i].getTr.position.z);
                
            }

            elapsedTime += Time.deltaTime * timeMult;
            //await Task.Yield();
            yield return null;
        }

        if (platformIndex >= 8){
            StartCoroutine(WinState_Coro());
            yield break;
        }

        platformIndex++;
        _collectedPlatforms.Add(platforms[platformIndex]);
        MoveToEndDestination();
    }

    public void SyncTimelineTime()
    {
        m_timelineTime.value = (float)_director.time;
    }

    public void ResetTimeline()
    {
        _director.time = restartTime;
        SyncTimelineTime();
    }

    public void StartTimer(){
        //CALL FROM LEVER (NOT AS GLOBAL EVENT!)
        //Lock them on our side (we will be the master for all platform stuff)
        isMasterExecutioneer = true;
        for (int i = 0; i < platforms.Count; i++){
            platforms[i].lockObject(true);
        }

        timerStarted = true;
        m_startAllTimers.Call(0);
    }

    public void SetStoryTimeline(float newTime)
    {
        _storyDirector.time = newTime;
        m_secondTimelineTime.value = newTime;

    }

    private bool winStateTriggered = false;

    private IEnumerator WinState_Coro(){
        if (winStateTriggered)
            yield break;

        winStateTriggered = true;
        
        SetStoryTimeline(finalTime);
        _storyDirector.Play();
        Debug.Log("MinoTime!");
        //MinoGameManager.Instance.SetupMinotaur();   //mino does not have an ID and therefore tracer makes an error on the network client
        winstateEvent.Invoke();

        SetPlatformAvailability(false);

        yield return new WaitForFixedUpdate();
    }
}