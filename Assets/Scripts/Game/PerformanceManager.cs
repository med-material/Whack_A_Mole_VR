using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using static WallManager;

public enum JudgementType {
    None,
    MaxConstant,
    Random,
    MaxSpeed,
    Distance,
    Time
}

public enum JudgementLevel {
    Operation,
    Action,
}

// Data Structures
[System.Serializable]
public class PerfData {
    public Queue<float> lastVals = new Queue<float>();
    public Queue<float> lastJudges = new Queue<float>();
    public Dictionary<int, List<float>> lastJudgesByMole = new Dictionary<int, List<float>>();
    public Dictionary<int, int> moleShootOrder = new Dictionary<int, int>(); // order number, if order number is missing, it's the other controller.
    public int maxShot = -1;
    public float movingAverage = -1f;
    public float upperThresholdAction = -1f;
    public float lowerThresholdAction = -1f;
    public float[] actionMemoryWorstVals = new float[] { -1, -1, -1, -1, -1 };
    public float[] actionMemoryBestVals = new float[] { -1, -1, -1, -1, -1 };
    public float actionMemoryWorstVal = -1f;
    public float actionMemoryBestVal = -1f;
    public float actionMemoryClock = 0.0f;
    public int actionMemoryIndex = 0;
    public float actionMemoryThreshold = 5f; // Each action memory pocket corresponds to 5s.
    public float[] instantMemoryWorstVals = new float[] { -1, -1, -1, -1, -1 };
    public float[] instantMemoryBestVals = new float[] { -1, -1, -1, -1, -1 };
    public float instantMemoryWorstVal = -1f;
    public float instantMemoryBestVal = -1f;
    public float instantMemoryClock = 0.0f;
    public int instantMemoryIndex = 0;
    public float instantMemoryThreshold = 1f; // Each instant memory pocket corresponds to 1s.
    public Queue<float> meanMemoryVals = new Queue<float>();
    public List<float> actionPeakSpeedVals = new List<float>();
    public float perfBestAction = -1f;
    public float perfWorstAction = -1f;
    public float perfActionFraction = -1f;
    public float dwelltime = 0f;

    // State
    public float perfPrev = -1f;
    public float perf = -1f;
    public float perfBest = -1f;
    public float perfWorst = -1f;
    public float perfFraction = -1f;
    public float judge = -1f;
    public float upperThresholdInstant = -1f;
    public float lowerThresholdInstant = -1f;
    public Vector3 posPrev = Vector3.zero;
    public Vector3 pos = Vector3.zero;
    public float traveldist = -1f;
    public float idealdist = -1f;
    public float speedClock = -1f;
    public float speedClockFrequency = 0.01f;
    public float speeddist = -1f;
    public float speedtime = -1f;
    // GIZMO DEBUG
    //public List<List<Vector3>> travel = new List<List<Vector3>>();

    public Vector3 actionStartPos = Vector3.zero;
    public Vector3 actionEndPos = Vector3.zero;
    public float instantStartTimestamp = -1f;
    public float actionStartTimestamp = -1f;
    public float actionEndTimestamp = -1f;
}

public class PerformanceManager : MonoBehaviour
{
    [SerializeField]
    private LoggingManager loggingManager;

    [SerializeField]
    private JudgementType judgementType = JudgementType.MaxSpeed;

    // Action data
    // Dictionary holding performance data for different controllers.
    private Dictionary<ControllerName, PerfData> perfData = new Dictionary<ControllerName, PerfData>();
    public PerfData perfR = new PerfData();
    public PerfData perfL = new PerfData();

    // Average configuration
    // we stopped using a memory limit based on "n", because counting operations is not equal
    // to counting actions. instead we now use time, which acts the same in both temporal dimensions.
    private float meanMemoryLimit = 20; // use the last 20 values for calculating mean
    private int minimumJudgeThreshold = 5;
    // using multipliers is not a fair. speed is one kind of unit, time is another kind of unit.
    private float MultiplierUp = 2f; // Upper/Lower Threshold multipliers
    private float MultiplierDown = 0.50f;
    private float fadingFraction = 0.01f; // how much the max should fade over time (1%)
    private GameDirector.GameState gameState;

    // Awake is called when the script instance is being loaded.
    private void Awake()
    {
        perfData[ControllerName.Left] = perfL;
        perfData[ControllerName.Right] = perfR;
    }

    // Data Consumption
    #region Data Consumption
    // Retrieve performance data for a given controller.
    public PerfData GetPerfData(ControllerName controllerName)
    {
        if (perfData.ContainsKey(controllerName))
            return perfData[controllerName];
        else
        {
            Debug.LogWarning($"PerfData for controller '{controllerName}' not found!");
            return null;
        }
    }

    // Retrieve instant judgment value for a given controller.
    public float GetInstantPerformance(ControllerName controllerName)
    {
        if (perfData.ContainsKey(controllerName))
            return perfData[controllerName].perf;
        else
        {
            Debug.LogWarning($"Instant performance for controller '{controllerName}' not found!");
            return 0.0f;
        }
    }

    // Retrieve instant judgment value for a given controller.
    public float GetInstantJudgement(ControllerName controllerName)
    {
        if (perfData.ContainsKey(controllerName))
            return perfData[controllerName].judge;
        else
        {
            Debug.LogWarning($"Instant judgement for controller '{controllerName}' not found!");
            return 0.0f;
        }
    }

    // Retrieve action judgement value for a given controller.
    public float GetActionPerformance(ControllerName controllerName)
    {
        if (perfData.ContainsKey(controllerName) && perfData[controllerName].lastVals.Any())
            return perfData[controllerName].lastVals.LastOrDefault();
        else
        {
            if (!perfData.ContainsKey(controllerName))
                Debug.LogWarning($"Action judgement for controller '{controllerName}' not found!");
            else
                Debug.LogWarning($"No last judgements available for controller '{controllerName}'!");
            return 0.0f;
        }
    }

    // Retrieve action judgement value for a given controller.
    public float GetActionJudgement(ControllerName controllerName)
    {
        if (perfData.ContainsKey(controllerName) && perfData[controllerName].lastJudges.Any())
            return perfData[controllerName].lastJudges.LastOrDefault();
        else
        {
            if (!perfData.ContainsKey(controllerName))
                Debug.LogWarning($"Action judgement for controller '{controllerName}' not found!");
            else
                Debug.LogWarning($"No last judgements available for controller '{controllerName}'!");
            return 0.0f;
        }
    }
    #endregion


    // Logging
    #region Logging
    // Get performance data in a structured dictionary.
    public Dictionary<string, object> GetPerformanceData()
    {
        Dictionary<string, object> data = new Dictionary<string, object>() {
            {"CtrRInstantJudgement", perfR.judge},
            {"CtrRActionJudgement", perfR.lastJudges.LastOrDefault()},
            {"CtrRSpeedDist", perfR.speeddist},
            {"CtrRSpeedTime", perfR.speedtime},
            {"CtrRTraveldist", perfR.traveldist},
            {"CtrRIdealDist", perfR.idealdist},
            //{"CtrRInstantPerformanceFraction", perfR.perfFraction},
            {"CtrRInstantPerformance", perfR.perf},
            {"CtrRInstantUpperThreshold", perfR.upperThresholdInstant},
            {"CtrRInstantLowerThreshold", perfR.lowerThresholdInstant},
            {"CtrRActionUpperThreshold", perfR.upperThresholdAction},
            {"CtrRActionLowerThreshold", perfR.lowerThresholdAction},
            {"CtrRtInstantPerformanceBest", perfR.perfBest},
            {"CtrRInstantPerformanceWorst", perfR.perfWorst},
            {"CtrRInstantPosition", perfR.pos},
            {"CtrRInstantPositionPrev", perfR.posPrev},
            {"CtrRActionPerformanceBest", perfR.perfBestAction},
            {"CtrRActionPerformanceWorst", perfR.perfWorstAction},
            {"CtrRActionPeakSpeedVals", string.Join(" ", perfR.actionPeakSpeedVals)},
            {"CtrLInstantJudgement", perfL.judge},
            {"CtrLActionJudgement", perfL.lastJudges.LastOrDefault()},
            {"CtrLInstantPerformance", perfL.perf},
            {"CtrLSpeedDist", perfL.speeddist},
            {"CtrLSpeedTime", perfL.speedtime},
            {"CtrLTraveldist", perfL.traveldist},
            {"CtrLIdealDist", perfL.idealdist},
            //{"CtrLInstantPerformanceFraction", perfL.perfFraction},
            {"CtrLInstantUpperThreshold", perfL.upperThresholdInstant},
            {"CtrLInstantLowerThreshold", perfL.lowerThresholdInstant},
            {"CtrLActionUpperThreshold", perfL.upperThresholdAction},
            {"CtrLActionLowerThreshold", perfL.lowerThresholdAction},
            {"CtrLInstantPerformanceBest", perfL.perfBest},
            {"CtrLInstantPerformanceWorst", perfL.perfWorst},
            {"CtrLInstantPosition", perfL.pos},
            {"CtrLInstantPositionPrev", perfL.posPrev},
            {"CtrLActionPerformanceBest", perfL.perfBestAction},
            {"CtrLActionPerformanceWorst", perfL.perfWorstAction},
            {"CtrLActionPeakSpeedVals", string.Join(" ", perfL.actionPeakSpeedVals)},
        };
        return data;
    }
    #endregion

    // Data Control
    #region Data Control
    // Reset performance history for all controllers.
    public void ResetPerfHistory() {
        foreach(KeyValuePair<ControllerName, PerfData> entry in perfData) {
            entry.Value.lastVals.Clear();
            entry.Value.lastJudges.Clear();
        }       
    }

    // Reset performance data to its default state.
    public void ResetPerfData() {
        // Resets History, but maintains moving average.
        perfR = new PerfData();
        perfL = new PerfData();
        perfData[ControllerName.Left] = perfL;
        perfData[ControllerName.Right] = perfR;
    }

    // Callback to handle game state changes.
    // Handle pointer shoot action and update performance metrics accordingly.
    public void OnGameStateChanged(GameDirector.GameState gameState)
    {
        if (gameState == GameDirector.GameState.Playing)
        {
            // The performance resets when game starts playing (changing to play, from any other state).
            // Otherwise our calculations dont make sense.
            ResetPerfData();
        }
    }

    #endregion

    // Data Feeders
    #region Data Feeders
    // BasicPointer: OnPointerShoot and OnPointerMove
    /// <summary>
    /// Handles the shooting action of a pointer, evaluates the user's performance based on the action,
    /// and logs relevant metrics.
    /// </summary>
    /// <param name="shootData">Data related to the shooting action, including hit target and controller details.</param>
    public void OnPointerShoot(ShootData shootData)
    {
        // Extract relevant data from the shooting event.
        RaycastHit hit = shootData.hit;
        ControllerName controllerName = shootData.name;

        // Flag to track if a mole was hit.
        bool moleHit = false;
        Mole mole;

        // Check if the shot hit a collider.
        if (hit.collider)
        {
            // Try to get the 'Mole' component from the hit game object.
            if (hit.collider.gameObject.TryGetComponent<Mole>(out mole))
            {
                moleHit = true;  // A mole was hit.

                // Retrieve performance data for the given controller name from the dictionary.
                PerfData perf = perfData[controllerName];

                // Update the performance data with new values from the shoot event.
                perf.dwelltime = shootData.dwell;
                perf.actionEndPos = hit.point;
                //float nextActionStartTimestamp = perf.actionEndTimestamp;
                perf.actionEndTimestamp = Time.time;

                // Initialize variables to hold new value and judgement value for the action.
                float newVal;
                float judgement;

                // Depending on the judgement type, calculate the new value and judgement for the action.
                if (judgementType == JudgementType.MaxSpeed) {
                    newVal = CalculateActionMaxSpeed(perf);
                    //UpdateActionThresholds(newVal, perf);
                    UpdateActionMovingAverage(newVal, perf);
                    judgement = MakeJudgement(newVal, perf, level:JudgementLevel.Action);
                } else if (judgementType == JudgementType.Distance) {
                    newVal = CalculateActionDistance(perf);
                    //UpdateActionThresholds(newVal, perf, thresholdMax: false);
                    UpdateActionMovingAverage(newVal, perf);
                    judgement = MakeJudgement(newVal, perf, level:JudgementLevel.Action);
                } else if (judgementType == JudgementType.Time) {
                    newVal = CalculateActionTime(perf);
                    //UpdateActionThresholds(newVal, perf, thresholdMax: false);
                    UpdateActionMovingAverage(newVal, perf, thresholdMax: false);
                    judgement = MakeJudgement(newVal, perf, thresholdMax: false, level:JudgementLevel.Action);
                } else if (judgementType == JudgementType.MaxConstant) {
                    newVal = 1f;
                    judgement = 1f;
                } else if (judgementType == JudgementType.Random)
                {
                    float rand = Random.Range(0f, 1f);
                    newVal = rand;
                    judgement = rand;
                } else if (judgementType == JudgementType.None)
                {
                    newVal = -1f;
                    judgement = 0f;
                }
                else {
                    newVal = -1f;
                    judgement = 0f;
                }

                // Store the calculated values in the performance data's queue.
                perf.lastVals.Enqueue(newVal);
                perf.lastJudges.Enqueue(judgement);

                if (!perf.lastJudgesByMole.ContainsKey(mole.GetId())) {
                    perf.lastJudgesByMole[mole.GetId()] = new List<float>();
                }
                perf.lastJudgesByMole[mole.GetId()].Add(judgement);
                // keeps track of the order in which moles were shot.
                if (perfR.maxShot == -1) perfR.maxShot = 0;
                if (perfL.maxShot == -1) perfL.maxShot = 0;
                perfR.maxShot++; // update number of shots to max.
                perfL.maxShot++;
                perf.moleShootOrder.Add(perf.maxShot, mole.GetId());
                

                // Log results
                // Log the event for entering the MotorSpace.
                loggingManager.Log("Event", new Dictionary<string, object>()
                {
                    {"Event", "Action Performance"},
                    {"JudgementType", System.Enum.GetName(typeof(JudgementType), judgementType)},
                    {"ActionJudgement", judgement},
                    {"ActionValue", newVal},
                    {"ActionDwellTime", perf.dwelltime},
                    {"ActionControllerName", controllerName},
                    {"ActionTimeStart", perf.actionStartTimestamp},
                    {"ActionTimeEnd", perf.actionEndTimestamp},
                    {"ActionPositionStart", perf.actionStartPos},
                    {"ActionPositionEnd", perf.actionEndPos},
                    {"ActionPerformanceBest", perf.perfBestAction},
                    {"ActionPerformanceWorst", perf.perfWorstAction},
                    {"ActionPerformanceFraction", perf.perfActionFraction},
                    {"ActionMemoryThreshold", perf.actionMemoryThreshold},
                    {"InstantMemoryThreshold", perf.instantMemoryThreshold},
                    {"ActionThresholdUpper", perf.upperThresholdAction},
                    {"ActionThresholdLower", perf.lowerThresholdAction},
                    {"ActionPeakSpeedVals", string.Join(" ", perf.actionPeakSpeedVals)},
                    {"ActionMemoryBestVals", string.Join(" ", perf.actionMemoryBestVals)},
                    {"ActionMemoryWorstVals", string.Join(" ", perf.actionMemoryWorstVals)},
                });

                // Here we update perf variables to reflect the beginning of a new action.
                // Update actionStartTimestamp to reflect the beginning of a new action
                // we must update it after making calculations, otherwise the 
                // calculations dont have proper timestamps as their basis.
                perf.actionStartTimestamp = perf.actionEndTimestamp;
                perf.actionEndTimestamp = -1f;
                //perf.actionEndPos = perf.actionStartPos;
                perf.actionStartPos = perf.actionEndPos;
                perf.actionEndPos = Vector3.zero;
                perf.perf = 0f; // reset instant performance measure 
                perf.traveldist = 0f; // reset accumulated travel distance
                perf.idealdist = 0f; // reset ideal calculated distance
                perf.pos = Vector3.zero; // reset pos
                perf.posPrev = Vector3.zero; // reset pos
                perf.actionPeakSpeedVals.Clear(); // reset peak speed vals
                // GIZMO DEBUG
                //perf.travel.Clear();
            }
        }
    }

    // Set the judgement type for measuring performance DURING exec.
    public void SetJudgementType(JudgementType judgement) {
        judgementType = judgement;
        ResetPerfData();
    }

    /// <summary>
    /// Handles the movement of a pointer and evaluates the user's performance based on the movement type.
    /// </summary>
    /// <param name="moveData">Data related to the pointer's movement, including position and associated controller details.</param>
    public void OnPointerMove(MoveData moveData)
    {
        // Retrieve performance data for the given name from the dictionary.
        PerfData perf = perfData[moveData.name];

        // Update previous position and performance with current values.
        perf.pos = moveData.cursorPos;
        if (perf.posPrev == Vector3.zero) perf.posPrev = perf.actionStartPos;

        // accumulate distance in steps of 1cm difference.
        if (Vector3.Distance(perf.posPrev, perf.pos) > 0.01f) {
            // if perf.pos has no value, set it to the latest value, making perf.pos equal to perf.posPrev as starting point.
            if (perf.pos == Vector3.zero) perf.pos = moveData.cursorPos;

            if (perf.traveldist == -1f) perf.traveldist = 0f;
            
            // only calculate ideal distance if actionStartPos is initialized.
            if (perf.actionStartPos != Vector3.zero) {
                perf.traveldist += Vector3.Distance(perf.posPrev, perf.pos);
                perf.idealdist = Vector3.Distance(perf.actionStartPos, perf.pos);
                //Debug.Log("perf.traveldist: " + perf.traveldist + "perf.idealdist " + perf.idealdist);
            }
            
            // update measurements for peak speed every 1ms
            if (perf.speedClock == -1f) perf.speedClock = 0f;
            if (perf.speeddist == -1f) perf.speeddist = 0f;
            if (perf.speedtime == -1f) perf.speedtime = 0f;
            perf.speedClock += Time.deltaTime;
            perf.speeddist += Vector3.Distance(perf.posPrev, perf.pos);
            perf.speedtime += Time.deltaTime;

            // if traveldist is 0f, idealdist should be 0f.
            //if (perf.traveldist == 0f) perf.idealdist = 0f;

            // GIZMO DEBUGGING
            //if (perf.pos != perf.posPrev) { 
            //    perf.travel.Add(new List<Vector3>{perf.posPrev, perf.pos});
            //    float tempDist = 0f;
            //    foreach (List<Vector3> pos in perfR.travel) {
            //            tempDist += Vector3.Distance(pos[0],pos[1]);
            //    }
            //}
            // update previous position to current position.
            perf.posPrev = perf.pos;
        }

        perf.perfPrev = perf.perf;

        // Initialize variables to hold new performance value and judgement value.
        float newPerf;
        float judgement;

        // Depending on the judgement type, calculate the new performance value and judgement.
        if (judgementType == JudgementType.MaxSpeed)
        {
            newPerf = CalculateInstantMaxSpeed(perf);
            //UpdateInstantThresholds(newPerf, perf);
            UpdateInstantMovingAverage(newPerf, perf);
            judgement = MakeJudgement(newPerf, perf, level:JudgementLevel.Operation);
        }
        else if (judgementType == JudgementType.Distance)
        {
            newPerf = CalculateInstantDistance(perf);
            UpdateInstantMovingAverage(newPerf, perf);
            //UpdateInstantThresholds(newPerf, perf, thresholdMax: false);
            judgement = MakeJudgement(newPerf, perf, level:JudgementLevel.Operation);
        }
        else if (judgementType == JudgementType.Time)
        {
            newPerf = CalculateInstantTime(perf);
            UpdateInstantMovingAverage(newPerf, perf, thresholdMax: false);
            //UpdateInstantThresholds(newPerf, perf, thresholdMax: false);
            judgement = MakeJudgement(newPerf, perf, thresholdMax: false, level:JudgementLevel.Operation);
        }
        // Special cases with constant or random values.
        else if (judgementType == JudgementType.MaxConstant)
        {
            newPerf = 1f;  // Set to maximum constant value
            judgement = 1f;  // Set judgement to maximum
        }
        else if (judgementType == JudgementType.Random)
        {
            float rand = Random.Range(0f, 1f);
            newPerf = rand;
            judgement = rand;
        }
        else if (judgementType == JudgementType.None)
        {
            newPerf = -1f;  // Set to an invalid value
            judgement = 0f;  // No judgement
        }
        else
        {
            // Default case, if the judgement type is unknown.
            newPerf = -1f;  // Set to an invalid value
            judgement = 0f;  // No judgement
        }

        // Store the calculated performance and judgement values.
        perf.perf = newPerf;
        perf.judge = judgement;

        // reset speed clock
        if (perf.speedClock >= perf.speedClockFrequency) {
            perf.speedClock = 0f;
            perf.speeddist = 0f;
            perf.speedtime = 0f;
        }
    }

    #endregion

    // Average Calculator
    #region Calculators
    // Updates the moving average for action performance.
    private void UpdateActionMovingAverage(float val, PerfData perf, bool thresholdMax = true)
    {
        // Early exit if value is invalid.
        if (val == -1f) return;

        bool update = false;

        // Check and update the worst action value.
        if (perf.actionMemoryWorstVal == -1f)
        {
            perf.actionMemoryWorstVal = val;
            update = true;
        }
        else if (val < perf.actionMemoryWorstVal)
        {
            perf.actionMemoryWorstVal = val;
            update = true;
        }

        // Check and update the best action value.
        if (perf.actionMemoryBestVal == -1f)
        {
            perf.actionMemoryBestVal = val;
            update = true;
        }
        else if (val > perf.actionMemoryBestVal)
        {
            perf.actionMemoryBestVal = val;
            update = true;
        }

       float timePassed = -1f;
        if (perf.actionStartTimestamp == -1f) {
            perf.actionStartTimestamp = Time.time;
            timePassed = 0;
        } else {
            timePassed = Time.time - perf.actionStartTimestamp;
            perf.actionMemoryClock += timePassed;
        }

        if (perf.actionMemoryClock > perf.actionMemoryThreshold) {
            perf.actionMemoryBestVal = val;
            perf.actionMemoryWorstVal = val;
            perf.actionMemoryClock = 0f;
            perf.actionMemoryIndex++;
            update = true;
        }

        // if there is no update to any value, return early.
        if (!update) return;


        // Update memory every 100ms
        int index = perf.actionMemoryIndex % 5;
        perf.actionMemoryBestVals[index] = perf.actionMemoryBestVal;
        perf.actionMemoryWorstVals[index] = perf.actionMemoryWorstVal;

        // Determine how many of the values we can use for our average
        int averageMaxSize = perf.actionMemoryIndex < 5 ? perf.actionMemoryIndex+1 : 5;

        float bestSum = 0f;
        float worstSum = 0f;
        for (int i = 0; i < averageMaxSize; i++) {
            bestSum += perf.actionMemoryBestVals[i];
            worstSum += perf.actionMemoryWorstVals[i];
        }

        perf.perfBestAction = bestSum == 0f ? bestSum : bestSum / averageMaxSize;
        perf.perfWorstAction = worstSum == 0f ? worstSum : worstSum / averageMaxSize;

        // Set the action thresholds based on either prioritizing max or otherwise.
        if (thresholdMax)
        {
            perf.upperThresholdAction = perf.perfBestAction;
            perf.lowerThresholdAction = perf.perfWorstAction;
        }
        else
        {
            perf.upperThresholdAction = perf.perfWorstAction;
            perf.lowerThresholdAction = perf.perfBestAction;
        }
    }

   // Updates the moving average for instant performance.
   // Threshold max determines the direction of what is considered "good".
    private void UpdateInstantMovingAverage(float val, PerfData perf, bool thresholdMax = true)
    {
        // Early exit if value is invalid.
        if (val == -1f) return;

        bool update = false;

        perf.instantMemoryClock += Time.deltaTime;

        // Check and update the worst action value.
        if (perf.instantMemoryWorstVal == -1f)
        {
            perf.instantMemoryWorstVal = val;
            update = true;
        }
        else if (val < perf.instantMemoryWorstVal)
        {
            perf.instantMemoryWorstVal = val;
            update = true;
        }

        // Check and update the best action value.
        if (perf.instantMemoryBestVal == -1f)
        {
            perf.instantMemoryBestVal = val;
            update = true;
        }
        else if (val > perf.instantMemoryBestVal)
        {
            perf.instantMemoryBestVal = val;
            update = true;
        }



        if (perf.instantMemoryClock > perf.instantMemoryThreshold) {
            //perf.instantMemoryBestVal = val;
            //perf.instantMemoryWorstVal = val;
            perf.instantMemoryClock = 0f;
            perf.instantMemoryIndex++;
            update = true;
        }

        // if there is no update to any value, return early.
        if (!update) return;

        float timePassed = -1f;
        if (perf.instantStartTimestamp == -1f) {
            perf.instantStartTimestamp = Time.time;
            timePassed = 0;
        } else {
            timePassed = Time.time - perf.instantStartTimestamp;
        }

        // Update memory every 1s
        int index = perf.instantMemoryIndex % 5;
        perf.actionPeakSpeedVals.Add(perf.instantMemoryBestVal);        
        perf.instantMemoryBestVals[index] = perf.instantMemoryBestVal;
        perf.instantMemoryWorstVals[index] = perf.instantMemoryWorstVal;

        // Determine how many of the values we can use for our average
        int averageMaxSize = perf.instantMemoryIndex < 5 ? perf.instantMemoryIndex+1 : 5;

        float bestSum = 0f;
        float worstSum = 0f;
        for (int i = 0; i < averageMaxSize; i++) {
            bestSum += perf.instantMemoryBestVals[i];
            worstSum += perf.instantMemoryWorstVals[i];
        }

        perf.perfBest = bestSum == 0f ? bestSum : bestSum / averageMaxSize;
        perf.perfWorst = worstSum == 0f ? worstSum : worstSum / averageMaxSize;

        // if instantMemoryClock was reset, reset instantMemoryBestVal and worstVal as well.
        if (perf.instantMemoryClock == 0f) {
            perf.instantMemoryBestVal = val;
            perf.instantMemoryWorstVal = val;
        }

        // Set the action thresholds based on either prioritizing max or otherwise.
        if (thresholdMax)
        {
            perf.upperThresholdAction = perf.perfBestAction;
            perf.lowerThresholdAction = perf.perfWorstAction;
        }
        else
        {
            perf.upperThresholdAction = perf.perfWorstAction;
            perf.lowerThresholdAction = perf.perfBestAction;
        }

        // Set the instantaneous thresholds based on either prioritizing max or otherwise.
        if (thresholdMax)
        {
            perf.upperThresholdInstant = perf.perfBest;
            perf.lowerThresholdInstant = perf.perfWorst;
        }
        else
        {
            perf.upperThresholdInstant = perf.perfWorst;
            perf.lowerThresholdInstant = perf.perfBest;
        }
    }

    // Max-based Calculator
    /// <summary>
    /// Updates the best and worst action thresholds based on the provided value.
    /// The method can either emphasize on maximum thresholds or otherwise, depending on the 'thresholdMax' parameter.
    /// Best and worst action values are allowed to "fade" over time, altering their values based on recent performance.
    /// </summary>
    /// <param name="val">The action value to compare and update the thresholds.</param>
    /// <param name="perf">The performance data to be updated.</param>
    /// <param name="thresholdMax">Determines if the method should prioritize max thresholds (true) or not (false).</param>
    private void UpdateActionThresholds(float val, PerfData perf, bool thresholdMax = true) {

        // Early exit if value is invalid.
        if (val == -1f) return;

        // Update memory, just to ensure same number of performances are required.
        //perf.meanMemoryVals.Enqueue(val);
        //if (perf.meanMemoryVals.Count > meanMemoryLimit)
        //{
        //    perf.meanMemoryVals.Dequeue();
        //}

        // Debugging: Log the provided value and the best action for comparison.

        // Flags to indicate if the current value is the best or worst recorded action.
        bool best = false;
        bool worst = false;

        // Check and update the worst action value.
        if (perf.perfWorstAction == -1f)
        {
            perf.perfWorstAction = val;
            worst = true;
        }
        else if (thresholdMax && val < perf.perfWorstAction)
        {
            perf.perfWorstAction = val;
            worst = true;
        }
        else if (!thresholdMax && val > perf.perfWorstAction)
        {
            perf.perfWorstAction = val;
            worst = true;
        }

        // Check and update the best action value.
        if (perf.perfBestAction == -1f)
        {
            perf.perfBestAction = val;
            best = true;
        }
        else if (thresholdMax && val > perf.perfBestAction)
        {
            perf.perfBestAction = val;
            best = true;
        }
        else if (!thresholdMax && val < perf.perfBestAction)
        {
            perf.perfBestAction = val;
            best = true;
        }

        // Compute the action's range (difference between worst and best values).
        float actionRange = Mathf.Abs(perf.perfWorstAction - perf.perfBestAction);
        perf.perfActionFraction = actionRange * fadingFraction;

        // Calculate the time taken for the action.
        float time = perf.actionEndTimestamp - perf.actionStartTimestamp;

        // If the current value is not the best, "fade" the best action based on elapsed time and fading fraction.
        if (!best)
        {
            if (thresholdMax)
            {
                perf.perfBestAction -= time * fadingFraction;
            }
            else
            {
                perf.perfBestAction += time * fadingFraction;
            }
        }

        // If the current value is not the worst, "fade" the worst action based on elapsed time and fading fraction.
        if (!worst)
        {
            if (thresholdMax)
            {
                perf.perfWorstAction += time * fadingFraction;
            }
            else
            {
                perf.perfWorstAction -= time * fadingFraction;
            }
        }

        // Update the upper and lower thresholds for the action based on the priority (max or otherwise).
        if (thresholdMax)
        {
            perf.upperThresholdAction = perf.perfBestAction;
            perf.lowerThresholdAction = perf.perfWorstAction;
        }
        else
        {
            perf.upperThresholdAction = perf.perfWorstAction;
            perf.lowerThresholdAction = perf.perfBestAction;
        }

    }

    /// <summary>
    /// Updates the instantaneous best and worst performance thresholds based on the provided value.
    /// Depending on the 'thresholdMax' parameter, the method can prioritize either maximum thresholds or otherwise.
    /// Best and worst values are allowed to "fade" over time, changing their values based on recent performance.
    /// </summary>
    /// <param name="val">The value to compare and update the thresholds against.</param>
    /// <param name="perf">The performance data to be updated.</param>
    /// <param name="thresholdMax">Determines if the method should prioritize max thresholds (true) or not (false).</param>
    private void UpdateInstantThresholds(float val, PerfData perf, bool thresholdMax = true)
    {
        // Early exit if value is -1 (invalid or sentinel value).
        if (val == -1f) return;



        // Flags to determine if the provided value is the best or worst recorded performance.
        bool best = false;
        bool worst = false;

        // Check and update the worst performance value.
        if (perf.perfWorst == -1f)
        {
            perf.perfWorst = val;
            worst = true;
        }
        else if (thresholdMax && val < perf.perfWorst)
        {
            perf.perfWorst = val;
            worst = true;
        }
        else if (!thresholdMax && val > perf.perfWorst)
        {
            perf.perfWorst = val;
            worst = true;
        }

        // Check and update the best performance value.
        if (perf.perfBest == -1f)
        {
            perf.perfBest = val;
            best = true;
        }
        else if (thresholdMax && val > perf.perfBest)
        {
            perf.perfBest = val;
            best = true;
        }
        else if (!thresholdMax && val < perf.perfBest)
        {
            perf.perfBest = val;
            best = true;
        }

        // Calculate the difference between worst and best values and adjust by the fading fraction.
        float range = Mathf.Abs(perf.perfWorst - perf.perfBest);
        perf.perfFraction = range * fadingFraction;

        // If the current value isn't the best, adjust the best value based on elapsed game time and fading fraction.
        if (!best)
        {
            if (thresholdMax)
            {
                // Decrease the best value by a fraction if the value wasn't higher.
                perf.perfBest -= Time.deltaTime * fadingFraction;
            }
            else
            {
                perf.perfBest += Time.deltaTime * fadingFraction;
            }
        }

        // If the current value isn't the worst, adjust the worst value based on elapsed game time and fading fraction.
        if (!worst)
        {
            if (thresholdMax)
            {
                perf.perfWorst += Time.deltaTime * fadingFraction;
            }
            else
            {
                perf.perfWorst -= Time.deltaTime * fadingFraction;
            }
        }

        // Set the action thresholds based on either prioritizing max or otherwise.
        if (thresholdMax)
        {
            perf.upperThresholdAction = perf.perfBestAction;
            perf.lowerThresholdAction = perf.perfWorstAction;
        }
        else
        {
            perf.upperThresholdAction = perf.perfWorstAction;
            perf.lowerThresholdAction = perf.perfBestAction;
        }

        // Set the instantaneous thresholds based on either prioritizing max or otherwise.
        if (thresholdMax)
        {
            perf.upperThresholdInstant = perf.perfBest;
            perf.lowerThresholdInstant = perf.perfWorst;
        }
        else
        {
            perf.upperThresholdInstant = perf.perfWorst;
            perf.lowerThresholdInstant = perf.perfBest;
        }
    }


    private void UpdateInstantAvgSpeedThresholds(float val, PerfData perf)
    {
        perf.upperThresholdInstant = perf.upperThresholdAction;
        perf.lowerThresholdInstant = perf.lowerThresholdAction;
    }

    // Max-based Calculator
    private float CalculateInstantMaxUnitSpeed(PerfData perf) {

        // if we don't have a previous position, abort calculation.
        if (perf.actionStartPos == Vector3.zero) return -1f;

        //Debug.Log("lastPosition: " + lastPositionSpeed);
        float distance = Vector3.Distance(perf.pos, perf.posPrev);
        float speed = distance / Time.deltaTime;
        return speed;
    }

    // Calculators
    private float CalculateInstantAvgSpeed(PerfData perf) {
        // TODO: Should we calculate the instant speed (frame by frame), or should we calculate speed
        // based on the distance accumulated since the beginning?

        // if we don't have a previous position, abort calculation.
        if (perf.actionStartPos == Vector3.zero) return -1f;

        //Debug.Log("lastPosition: " + lastPositionSpeed);
        //float distance = Vector3.Distance(perf.pos, perf.actionStartPos);
        float time = Time.time - perf.actionStartTimestamp;
        float speed;
        if (perf.traveldist == 0f || time == 0f) {
            speed = 0f;
        } else {
            speed = perf.traveldist / time;
        }
        return speed;
    }

    // Calculators
    private float CalculateInstantMaxSpeed(PerfData perf) {
        // TODO: Should we calculate the instant speed (frame by frame), or should we calculate speed
        // based on the distance accumulated since the beginning?

        // if we don't have a previous position, abort calculation.
        if (perf.actionStartPos == Vector3.zero) return -1f;

        //Debug.Log("lastPosition: " + lastPositionSpeed);
        //float distance = Vector3.Distance(perf.pos, perf.actionStartPos);
        float speed;
        if (perf.speeddist == 0f || perf.speedtime == 0f) {
            speed = 0f;
        } else {
            speed = perf.speeddist / perf.speedtime;
        }
        return speed;
    }

    private float CalculateInstantDistance(PerfData perf) {
        // TODO: Should we calculate the instant speed (frame by frame), or should we calculate speed
        // based on the distance accumulated since the beginning?

        // if this is our first action, we don't have enough information to calculate speed.
        if (perf.actionStartTimestamp == -1f || perf.actionStartPos == Vector3.zero || perf.pos == Vector3.zero) return -1f;

        // if we don't have a previous position, abort calculation.
        if (perf.posPrev == Vector3.zero) return -1f;

        if (perf.perf == -1f) perf.perf = 0f;

        float distance;
        if (perf.traveldist == 0f) {
            distance = 0f;
        } else {
            distance = perf.idealdist / perf.traveldist;
        }


        //float distance = perf.perf;
        //Debug.Log("lastPosition: " + lastPositionSpeed);
        
        return distance;
    }

    //void OnDrawGizmos() {
    //    foreach (List<Vector3> pos in perfR.travel) {
    //            Gizmos.DrawLine(pos[0], pos[1]);
    //        }
    //    Gizmos.DrawLine(perfR.actionStartPos, perfR.pos);
    //    Gizmos.DrawCube(perfR.actionStartPos, new Vector3(.05f, .05f, .05f));
    //    Gizmos.DrawCube(perfR.pos, new Vector3(0.05f, 0.05f, 0.05f));
    //    Gizmos.DrawCube(perfR.posPrev, new Vector3(0.05f, 0.05f, 0.05f));
    //}

    private float CalculateInstantTime(PerfData perf) {
        // TODO: Should we calculate the instant speed (frame by frame), or should we calculate speed
        // based on the distance accumulated since the beginning?

        // if this is our first action, we don't have enough information to calculate time.
        if (perf.actionStartTimestamp == -1f) return -1f;

        // if we don't have a previous position, abort calculation.
        if (perf.posPrev == Vector3.zero) return -1f;

        float time = Time.time - perf.actionStartTimestamp;
        // dont subtract dwelltime, to ensure we dont start 
        // in negative time.
        //time = time - perf.dwelltime;

        return time;
    }

    private float CalculateActionDistance(PerfData perf) {
        if (perf.actionStartTimestamp == -1f || perf.actionEndTimestamp == -1f || 
            perf.actionEndPos == Vector3.zero || perf.actionStartPos == Vector3.zero) {
            // if this is our first action, we don't have enough information to calculate speed.
            return -1f;
        }

        float idealDistance = Vector3.Distance(perf.actionStartPos, perf.actionEndPos);
        //Debug.Log("perf.actionStartPos: " + perf.actionStartPos + "perf.actionEndPos" + perf.actionEndPos);
        float distance = -1f;
        if (perf.traveldist == 0f) {
            distance = 0f;
        } else {
            // trajectory straightness calculation
            distance = idealDistance / perf.traveldist; // perf.perf is the true distance.
        }
        //Debug.Log("perf.perf: " + perf.perf);
        //Debug.Log("idealDistance: " + idealDistance);
        //Debug.Log("newDistance: " + distance);
        return distance; 
    }

    private float CalculateActionTime(PerfData perf) {
        if (perf.actionStartTimestamp == -1f || perf.actionEndTimestamp == -1f || perf.actionEndPos == Vector3.zero) {
            // if this is our first action, we don't have enough information to calculate speed.
            return -1f;
        }

        float time = perf.actionEndTimestamp - perf.actionStartTimestamp;
        time = time - perf.dwelltime;
        return time;
    }

    private float CalculateActionAvgSpeed(PerfData perf) {
        if (perf.actionEndTimestamp == -1f || perf.actionEndPos == Vector3.zero) {
            // if this is our first action, we don't have enough information to calculate speed.
            return -1f;
        }

        //float distance = Vector3.Distance(perf.actionStartPos, perf.actionEndPos);
        float time = perf.actionEndTimestamp - perf.actionStartTimestamp;
        time = time - perf.dwelltime; // subtract dwell time.
        float speed = perf.traveldist / time;

        return speed;
    }

    private float CalculateActionMaxSpeed(PerfData perf) {
        if (perf.actionEndTimestamp == -1f || perf.actionEndPos == Vector3.zero) {
            // if this is our first action, we don't have enough information to calculate speed.
            return -1f;
        }
        
        float peakspeed;
        if (perf.actionPeakSpeedVals.Count < 1) {
            peakspeed = -1f;
        } else {
            peakspeed = perf.actionPeakSpeedVals.Max();
        }
        
        return peakspeed;
    }
    #endregion

    #region Judgement Calculators
    /// <summary>
    /// Determines the instant judgement of a value based on the instantaneous performance thresholds.
    /// The judgement value ranges from 0 to 1, with the interpretation depending on the 'thresholdMax' parameter.
    /// </summary>
    /// <param name="val">The value to judge against the instantaneous thresholds.</param>
    /// <param name="perf">The performance data containing the instantaneous thresholds.</param>
    /// <param name="thresholdMax">Determines if higher values are judged positively (true) or negatively (false).</param>
    /// <returns>A float representing the judgement value ranging from 0 (negative) to 1 (positive).</returns>
    private float MakeJudgement(float val, PerfData perf, bool thresholdMax = true, JudgementLevel level = JudgementLevel.Action)
    {
        float judgement;

        // If the value is a sentinel value (-1), return a neutral judgement.
        if (val == -1f)
        {
            judgement = 0f;
            return judgement;
        }

        float upper = -1f;
        float lower = -1f;
        if (level == JudgementLevel.Operation) {
            // Swap thresholds depending on whether max is up or down.
            lower = thresholdMax ? perf.lowerThresholdInstant : perf.upperThresholdInstant;
            upper = thresholdMax ? perf.upperThresholdInstant : perf.lowerThresholdInstant;
        } else { // if JudgementLevel.Action
            lower = thresholdMax ? perf.lowerThresholdAction : perf.upperThresholdAction;
            upper = thresholdMax ? perf.upperThresholdAction : perf.lowerThresholdAction;
        }

        // Determine the judgement based on comparison with the instantaneous thresholds.
        if (val <= lower)
        {
            judgement = thresholdMax ? 0f : 1f;
        }
        else if (val >= upper)
        {
            judgement = thresholdMax ? 1f : 0f;
        }
        else
        {
            // If the value is between thresholds, compute a relative judgement value.
            judgement = (val - lower) / (upper - lower);

            // Reverse judgement if not prioritizing max.
            if (!thresholdMax)
            {
                judgement = 1 - judgement;
            }
        }

        return judgement;
    }

    #endregion

}




//     // old code
//     public void OnPointerShoot()
//     {
//         isTimerRunning = false;
//         CalculateAction();
//     }

//     private float timeSinceLastShot = 0f;
//     private bool isTimerRunning = false;
//     private float speed = 0f;
//     private float instantSpeed = 0f;
//     private Vector3 lastPosition = Vector3.zero;
//     private Vector3 lastPositionSpeed = Vector3.zero;
//     private float lastDistance = 0f;
//     private float feedback = 0f;
//     private float averageSpeed = 0f;
//     private int nbShoot = 0;

//     private void Awake()
//     {
//     }

//     private void Update()
//     {
//         if (isTimerRunning)
//         {
//             timeSinceLastShot += Time.deltaTime;
//         }

//         CalculateSpeed();
//         CalculateInstantSpeed();
//     }

//     private void ResetShoot()
//     {
//         timeSinceLastShot = 0f;
//         speed = 0f;
//         lastDistance = 0f;
//     }



//     public void onMoleActivated()
//     {
//         isTimerRunning = true;
//         timeSinceLastShot = 0f;
//         lastDistance = 0f;
//     }


//     public void UpdatePointerData(BasicPointer pointer)
//     {
//         // Now you have access to all public variables and methods of the BasicPointer instance
//         pointerData = pointer;

//     }

//     public void CalculateSpeed()
//     {

//         Vector3 position = pointerData.MappedPosition;
//         if (lastPosition == Vector3.zero)
//         {
//             lastPosition = position;
//         }
//         if (isTimerRunning)
//         {
//             float distance = Vector3.Distance(position, lastPosition);
//             lastPosition = position;
//             lastDistance = lastDistance + distance;
//             speed = lastDistance / timeSinceLastShot;
//         }
//     }

//     public void CalculateInstantSpeed()
//     {
//         Vector3 position = pointerData.MappedPosition;
//         if (lastPositionSpeed == Vector3.zero)
//         {
//             lastPositionSpeed = position;
//         }
//         if (lastPositionSpeed != Vector3.zero)
//         {
//             Debug.Log("lastPosition: " + lastPositionSpeed);
//             float distance = Vector3.Distance(position, lastPositionSpeed);
//             instantSpeed = distance / Time.deltaTime;
//         }
//         else
//         {
//             Debug.Log("FESSE " + lastPositionSpeed);
//         }
//         lastPositionSpeed = position;
//     }


//     public float GetSpeed()
//     {
//         return speed;
//     }

//     public float GetInstantSpeed()
//     {
//         return instantSpeed;
//     }


//     public float GetFeedback()
//     {
//         return feedback;
//     }

//     public Queue<float> GetTaskFeedbacks() {
//         return taskFeedbacks;
//     }

//     public void CalculateFeedback()
//     {
//         float minDistance = 0.3f;
//         lastSpeeds.Enqueue(speed);

//         if (lastSpeeds.Count > 20)
//         {
//             lastSpeeds.Dequeue();
//         }
//         if (nbShoot < 5)
//         {
//             feedback = 1;
//             averageSpeed = speed;
//             nbShoot++;
//         }
//         else if (lastDistance <= minDistance)
//         {
//             feedback = 1;
//         }
//         else
//         {
//             averageSpeed = lastSpeeds.Average();
//             nbShoot++;
//             float thresholdUp = 1.50f * averageSpeed;
//             float thresholdDown = 0.50f * averageSpeed;

//             if (speed <= thresholdDown)
//             {
//                 feedback = 0;
//             }
//             else if (speed >= thresholdUp)
//             {
//                 feedback = 1;
//             }
//             else
//             {
//                 feedback = (speed - thresholdDown) / (thresholdUp - thresholdDown);
//             }

//         }
//         taskFeedbacks.Enqueue(feedback);
//         ResetShoot();
//     }
// }
