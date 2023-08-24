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
    AverageSpeed,
    MaxSpeed,
    Distance,
    Time
}

// Data Structures
[System.Serializable]
public class PerfData {
    public Queue<float> lastVals = new Queue<float>();
    public Queue<float> lastJudges = new Queue<float>();
    public float movingAverage = -1f;
    public float upperThresholdAction = -1f;
    public float lowerThresholdAction = -1f;
    public Queue<float> meanMemoryVals = new Queue<float>();
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

    public Vector3 actionStartPos = Vector3.zero;
    public Vector3 actionEndPos = Vector3.zero;
    public float actionStartTimestamp = -1f;
    public float actionEndTimestamp = -1f;
}

public class PerformanceManager : MonoBehaviour
{
    [SerializeField]
    private LoggingManager loggingManager;

    [SerializeField]
    private JudgementType judgementType = JudgementType.AverageSpeed;

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
            {"CtrRInstantPerformance", perfR.perf},
            {"CtrRInstantPerformanceFraction", perfR.perfFraction},
            {"CtrRInstantUpperThreshold", perfR.upperThresholdInstant},
            {"CtrRInstantLowerThreshold", perfR.lowerThresholdInstant},
            {"CtrRtInstantPerformanceBest", perfR.perfBest},
            {"CtrRInstantPerformanceWorst", perfR.perfWorst},
            {"CtrLInstantJudgement", perfL.judge},
            {"CtrLInstantPerformance", perfL.perf},
            {"CtrLInstantPerformanceFraction", perfL.perfFraction},
            {"CtrLInstantUpperThreshold", perfL.upperThresholdInstant},
            {"CtrLInstantLowerThreshold", perfL.lowerThresholdInstant},
            {"CtrLInstantPerformanceBest", perfL.perfBest},
            {"CtrLInstantPerformanceWorst", perfL.perfWorst}
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
            }
        }

        // If no mole was hit, exit the method early.
        if (!moleHit) return;

        // Retrieve performance data for the given controller name from the dictionary.
        PerfData perf = perfData[controllerName];

        // Update the performance data with new values from the shoot event.
        perf.dwelltime = shootData.dwell;
        perf.actionEndPos = perf.actionStartPos;
        perf.actionStartPos = hit.point;
        perf.actionStartTimestamp = perf.actionEndTimestamp;
        perf.actionEndTimestamp = Time.time;

        // Initialize variables to hold new value and judgement value for the action.
        float newVal;
        float judgement;

        // Depending on the judgement type, calculate the new value and judgement for the action.
        if (judgementType == JudgementType.AverageSpeed) {
            newVal = CalculateActionSpeed(perf);
            UpdateActionMovingAverage(newVal, perf);
            judgement = MakeActionJudgement(newVal, perf);
        } else if (judgementType == JudgementType.MaxSpeed) {
            newVal = CalculateActionSpeed(perf);
            UpdateActionThresholds(newVal, perf);
            judgement = MakeActionJudgement(newVal, perf);
        } else if (judgementType == JudgementType.Distance) {
            newVal = CalculateActionDistance(perf);
            UpdateActionThresholds(newVal, perf, thresholdMax: false);
            judgement = MakeActionJudgement(newVal, perf, thresholdMax: false);
        } else if (judgementType == JudgementType.Time) {
            newVal = CalculateActionTime(perf);
            UpdateActionThresholds(newVal, perf, thresholdMax: false);
            judgement = MakeActionJudgement(newVal, perf, thresholdMax: false);
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
            {"ActionThresholdUpper", perf.upperThresholdAction},
            {"ActionThresholdLower", perf.lowerThresholdAction},
        });
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
        perf.posPrev = perf.pos;
        perf.pos = moveData.cursorPos;
        perf.perfPrev = perf.perf;

        // Initialize variables to hold new performance value and judgement value.
        float newPerf;
        float judgement;

        // Depending on the judgement type, calculate the new performance value and judgement.
        if (judgementType == JudgementType.AverageSpeed)
        {
            newPerf = CalculateInstantAvgSpeed(perf);
            UpdateInstantAvgSpeedThresholds(newPerf, perf);
            judgement = MakeInstantJudgement(newPerf, perf);
        }
        else if (judgementType == JudgementType.MaxSpeed)
        {
            newPerf = CalculateInstantMaxSpeed(perf);
            UpdateInstantThresholds(newPerf, perf);
            judgement = MakeInstantJudgement(newPerf, perf);
        }
        else if (judgementType == JudgementType.Distance)
        {
            newPerf = CalculateInstantDistance(perf);
            UpdateInstantThresholds(newPerf, perf, thresholdMax: false);
            judgement = MakeInstantJudgement(newPerf, perf, thresholdMax: false);
        }
        else if (judgementType == JudgementType.Time)
        {
            newPerf = CalculateInstantTime(perf);
            UpdateInstantThresholds(newPerf, perf, thresholdMax: false);
            judgement = MakeInstantJudgement(newPerf, perf, thresholdMax: false);
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
    }

    #endregion

    // Average Calculator
    #region Calculators
    // Updates the moving average for action performance.
    private void UpdateActionMovingAverage(float val, PerfData perf)
    {
        // Add the new value to the memory queue.
        perf.meanMemoryVals.Enqueue(val);
        // If the queue exceeds the memory limit, remove the oldest value.
        if (perf.meanMemoryVals.Count > meanMemoryLimit)
        {
            perf.meanMemoryVals.Dequeue();
        }
        // Compute the moving average.
        perf.movingAverage = perf.meanMemoryVals.Average();
        // Set upper and lower thresholds based on the moving average.
        perf.upperThresholdAction = MultiplierUp * perf.movingAverage;
        perf.lowerThresholdAction = MultiplierDown * perf.movingAverage;
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
        Debug.Log(val + " " + perf.perfBestAction);

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
    private float CalculateInstantMaxSpeed(PerfData perf) {

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
        float distance = Vector3.Distance(perf.pos, perf.actionStartPos);
        float time = Time.time - perf.actionStartTimestamp;
        float speed = distance / time;
        return speed;
    }

    private float CalculateInstantDistance(PerfData perf) {
        // TODO: Should we calculate the instant speed (frame by frame), or should we calculate speed
        // based on the distance accumulated since the beginning?

        // if this is our first action, we don't have enough information to calculate speed.
        if (perf.actionStartPos == Vector3.zero) return -1f;

        // if we don't have a previous position, abort calculation.
        if (perf.posPrev == Vector3.zero) return -1f;

        if (perf.perf == -1f) perf.perf = 0f;
        perf.perf = perf.perf + Vector3.Distance(perf.pos, perf.posPrev);

        float distance = perf.perf;
        //Debug.Log("lastPosition: " + lastPositionSpeed);
        //float distance = Vector3.Distance(perf.actionStartPos, perf.pos);
        return distance;
    }

    private float CalculateInstantTime(PerfData perf) {
        // TODO: Should we calculate the instant speed (frame by frame), or should we calculate speed
        // based on the distance accumulated since the beginning?

        // if this is our first action, we don't have enough information to calculate speed.
        if (perf.actionStartTimestamp == -1f) return -1f;

        // if we don't have a previous position, abort calculation.
        if (perf.posPrev == Vector3.zero) return -1f;

        float time = Time.time - perf.actionStartTimestamp;
        time = time - perf.dwelltime;
        return time;
    }

    private float CalculateActionDistance(PerfData perf) {
        if (perf.actionEndTimestamp == -1f || perf.actionEndPos == Vector3.zero) {
            // if this is our first action, we don't have enough information to calculate speed.
            return -1f;
        }

        //float distance = Vector3.Distance(perf.actionStartPos, perf.actionEndPos);
        float distance = perf.perf;
        perf.perf = 0f;
        return distance;
    }

    private float CalculateActionTime(PerfData perf) {
        if (perf.actionEndTimestamp == -1f || perf.actionEndPos == Vector3.zero) {
            // if this is our first action, we don't have enough information to calculate speed.
            return -1f;
        }

        float time = perf.actionEndTimestamp - perf.actionStartTimestamp;
        time = time - perf.dwelltime;
        return time;
    }

    private float CalculateActionSpeed(PerfData perf) {
        if (perf.actionEndTimestamp == -1f || perf.actionEndPos == Vector3.zero) {
            // if this is our first action, we don't have enough information to calculate speed.
            return -1f;
        }

        float distance = Vector3.Distance(perf.actionStartPos, perf.actionEndPos);
        float time = perf.actionEndTimestamp - perf.actionStartTimestamp;
        time = time - perf.dwelltime; // subtract dwell time.
        float speed = distance / time;

        return speed;
    }
    #endregion

    #region Judgement Calculators
    /// <summary>
    /// Determines the judgement of an action based on the performance thresholds.
    /// The judgement value ranges from 0 to 1, with the interpretation depending on the 'thresholdMax' parameter.
    /// </summary>
    /// <param name="val">The value to judge against the thresholds.</param>
    /// <param name="perf">The performance data containing the action thresholds.</param>
    /// <param name="thresholdMax">Determines if higher values are judged positively (true) or negatively (false).</param>
    /// <returns>A float representing the judgement value ranging from 0 (negative) to 1 (positive).</returns>
    private float MakeActionJudgement(float val, PerfData perf, bool thresholdMax = true)
    {
        float judgement;

        // If there is less than the threshold to judge threshold, default to 100% postive feedback.
        //if (perf.meanMemoryVals.Count < minimumJudgeThreshold)
        //{
        //    judgement = 0f;
        //    return judgement;
        //}

        // If the value is a sentinel value (-1), return a neutral judgement.
        if (val == -1f)
        {
            judgement = 0f;
            return judgement;
        }

        // Determine the judgement based on comparison with the action thresholds.
        if (val <= perf.lowerThresholdAction)
        {
            judgement = thresholdMax ? 0f : 1f;
        }
        else if (val >= perf.upperThresholdAction)
        {
            judgement = thresholdMax ? 1f : 0f;
        }
        else
        {
            // If the value is between thresholds, compute a relative judgement value.
            judgement = (val - perf.lowerThresholdAction) / (perf.upperThresholdAction - perf.lowerThresholdAction);

            // Reverse judgement if not prioritizing max.
            if (!thresholdMax)
            {
                judgement = 1 - judgement;
            }
        }

        return judgement;
    }

    /// <summary>
    /// Determines the instant judgement of a value based on the instantaneous performance thresholds.
    /// The judgement value ranges from 0 to 1, with the interpretation depending on the 'thresholdMax' parameter.
    /// </summary>
    /// <param name="val">The value to judge against the instantaneous thresholds.</param>
    /// <param name="perf">The performance data containing the instantaneous thresholds.</param>
    /// <param name="thresholdMax">Determines if higher values are judged positively (true) or negatively (false).</param>
    /// <returns>A float representing the judgement value ranging from 0 (negative) to 1 (positive).</returns>
    private float MakeInstantJudgement(float val, PerfData perf, bool thresholdMax = true)
    {
        float judgement;

        // If the value is a sentinel value (-1), return a neutral judgement.
        if (val == -1f)
        {
            judgement = 0f;
            return judgement;
        }

        // Determine the judgement based on comparison with the instantaneous thresholds.
        if (val <= perf.lowerThresholdInstant)
        {
            judgement = thresholdMax ? 0f : 1f;
        }
        else if (val >= perf.upperThresholdInstant)
        {
            judgement = thresholdMax ? 1f : 0f;
        }
        else
        {
            // If the value is between thresholds, compute a relative judgement value.
            judgement = (val - perf.lowerThresholdInstant) / (perf.upperThresholdInstant - perf.lowerThresholdInstant);

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
