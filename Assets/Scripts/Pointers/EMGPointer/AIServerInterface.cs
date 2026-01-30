using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class AIServerInterface
{
    private ThalmicMyo thalmicMyo;
    private int memorySize = 16;  // Reduced from 20 to allow faster gesture detection
    private string currentGesture = "Unknown";
    private string currentGestureProb = "Uncertain";
    private Queue<PredictionResponse> previousGesture = new Queue<PredictionResponse>();

    // Batch windowed prediction settings
    private string sessionId;
    private int batchSize = 10;  // Collect 10 samples before sending
    private List<int[]> emgBatch = new List<int[]>();
    private bool isProcessingRequest = false;
    private bool isBuffering = true;

    // Gesture change hysteresis - require N consecutive frames of same gesture to switch
    private const int GESTURE_CHANGE_THRESHOLD = 3;  // Reduced from 5 to allow faster gesture detection
    private string pendingGesture = "Unknown";
    private int pendingGestureCount = 0;

    // Confidence threshold - ignore low-confidence predictions
    private const float MIN_CONFIDENCE_THRESHOLD = 0.5f;  // Higher bar

    public AIServerInterface(ThalmicMyo myo)
    {
        thalmicMyo = myo;
        // Generate unique session ID for this Unity instance
        sessionId = $"unity_{System.Guid.NewGuid().ToString()}";
        Debug.Log($"[AIServerInterface] Session ID: {sessionId}");
    }

    public void StartPredictionRequestCoroutine()
    {
        // Get current EMG sample
        int[] currentEmgData = new int[8];
        System.Array.Copy(thalmicMyo._myoEmg, currentEmgData, 8);
        
        // Add to batch
        emgBatch.Add(currentEmgData);

        // Send batch when it reaches the batch size
        if (emgBatch.Count >= batchSize && !isProcessingRequest)
        {
            _ = SendBatchWindowedPredictionRequest(new List<int[]>(emgBatch));
            emgBatch.Clear();
        }
    }

    private async Task SendBatchWindowedPredictionRequest(List<int[]> batch)
    {
        if (isProcessingRequest) return;
        isProcessingRequest = true;

        // Build batch JSON - samples in chronological order
        List<string> batchSamples = new List<string>();
        foreach (int[] emgs in batch)
        {
            string sample = $@"{{
                ""EMG1"": {emgs[0]},
                ""EMG2"": {emgs[1]},
                ""EMG3"": {emgs[2]},
                ""EMG4"": {emgs[3]},
                ""EMG5"": {emgs[4]},
                ""EMG6"": {emgs[5]},
                ""EMG7"": {emgs[6]},
                ""EMG8"": {emgs[7]}
            }}";
            batchSamples.Add(sample);
        }
        
        string json = $@"{{
            ""batch"": [{string.Join(",", batchSamples)}],
            ""session_id"": ""{sessionId}""
        }}";

        try
        {
            using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/batch_predict", "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = 10;

                UnityWebRequestAsyncOperation operation = www.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[AIServerInterface] Batch windowed prediction error: " + www.error);
                }
                else
                {
                    string responseText = www.downloadHandler.text;
                    BatchWindowedResponse result = null;
                    try
                    {
                        result = JsonUtility.FromJson<BatchWindowedResponse>(responseText);
                    }
                    catch
                    {
                        Debug.LogWarning("[AIServerInterface] Failed to parse batch windowed response: " + responseText);
                    }

                    if (result != null && result.predictions != null)
                    {
                        ProcessBatchWindowedResponse(result);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AIServerInterface] Exception during batch windowed prediction: " + e.Message);
        }
        finally
        {
            isProcessingRequest = false;
        }
    }

    private void ProcessBatchWindowedResponse(BatchWindowedResponse response)
    {
        if (response.predictions == null || response.predictions.Count == 0)
        {
            return;
        }

        // Process predictions in order, only use the most recent predicted samples
        List<PredictionItem> predictedSamples = response.predictions
            .Where(p => p.status == "predicted")
            .ToList();

        if (predictedSamples.Count == 0)
        {
            // All samples still buffering
            PredictionItem lastPrediction = response.predictions[response.predictions.Count - 1];
            if (lastPrediction.status == "buffering")
            {
                isBuffering = true;
                currentGesture = "Neutral";
                currentGestureProb = "Buffering";
                
                // Log buffering progress occasionally
                if (lastPrediction.buffer_size % 5 == 0)
                {
                    Debug.Log($"[AIServerInterface] Buffering... {lastPrediction.buffer_size} samples collected");
                }
            }
            return;
        }

        if (isBuffering)
        {
            isBuffering = false;
            Debug.Log("[AIServerInterface] Buffer filled, predictions active");
        }

        // Only use the most recent prediction from the batch to avoid over-updating
        PredictionItem latestPrediction = predictedSamples[predictedSamples.Count - 1];
        
        // Check if gate model detected rest state
        if (latestPrediction.detection_method == "gate")
        {
            // Gate model confidently says this is rest/neutral
            Debug.Log($"[AIServerInterface] Gate model detected rest state (rest_prob: {latestPrediction.gate_rest_prob:F2})");
            
            // Add Neutral prediction to memory with high confidence
            PredictionResponse neutralResponse = new PredictionResponse
            {
                label = "Neutral",
                prob = latestPrediction.gate_rest_prob
            };
            
            if (previousGesture.Count >= memorySize)
            {
                previousGesture.Dequeue();
            }
            previousGesture.Enqueue(neutralResponse);
        }
        // Filter out low-confidence gesture predictions - treat as Neutral
        else if (latestPrediction.prob < MIN_CONFIDENCE_THRESHOLD)
        {
            Debug.Log($"[AIServerInterface] Low gesture confidence ({latestPrediction.prob:F2}) for '{latestPrediction.label}' - treating as Neutral (gate_gesture_prob: {latestPrediction.gate_gesture_prob:F2})");
            
            // Add Neutral prediction to memory
            PredictionResponse neutralResponse = new PredictionResponse
            {
                label = "Neutral",
                prob = 0.7f  // Medium confidence - gesture detected but classifier uncertain
            };
            
            if (previousGesture.Count >= memorySize)
            {
                previousGesture.Dequeue();
            }
            previousGesture.Enqueue(neutralResponse);
        }
        else
        {
            // Normal confident prediction - add to memory
            PredictionResponse predResponse = new PredictionResponse
            {
                label = latestPrediction.label,
                prob = latestPrediction.prob
            };
            
            // Log successful gesture detection with gate info
            if (latestPrediction.gate_gesture_prob > 0)
            {
                Debug.Log($"[AIServerInterface] Gesture detected: '{latestPrediction.label}' (conf: {latestPrediction.prob:F2}, gate_gesture_prob: {latestPrediction.gate_gesture_prob:F2})");
            }
            
            // Keep "Unknown" as-is - don't convert to Neutral
            // Unknown = classifier uncertain, should pause progress and wait
            // Neutral = gate detected rest, should reset progress

            if (previousGesture.Count >= memorySize)
            {
                previousGesture.Dequeue();
            }
            previousGesture.Enqueue(predResponse);
        }

        // Confidence-weighted majority voting
        if (previousGesture.Count > 0)
        {
            // Group by gesture and calculate weighted vote (count * average confidence)
            var gestureGroups = previousGesture
                .GroupBy(p => p.label)
                .Select(g => new {
                    Gesture = g.Key,
                    Count = g.Count(),
                    AvgConfidence = g.Average(p => p.prob),
                    WeightedVote = g.Count() * g.Average(p => p.prob)
                })
                .OrderByDescending(g => g.WeightedVote)
                .ToList();

            string votedGesture = gestureGroups[0].Gesture;
            float avgConfidence = gestureGroups[0].AvgConfidence;

            // Require minimum representation in memory (at least 40% of memory size)
            int minRepresentation = Mathf.CeilToInt(memorySize * 0.4f);
            if (gestureGroups[0].Count < minRepresentation)
            {
                // Not enough consistent predictions - return to Neutral
                Debug.Log($"[AIServerInterface] Insufficient representation ({gestureGroups[0].Count}/{minRepresentation}) - returning to Neutral");
                
                // Use hysteresis for transitioning to Neutral too
                if (pendingGesture == "Neutral")
                {
                    pendingGestureCount++;
                    if (pendingGestureCount >= GESTURE_CHANGE_THRESHOLD)
                    {
                        currentGesture = "Neutral";
                        currentGestureProb = "Uncertain";
                        pendingGestureCount = 0;
                        pendingGesture = "Unknown";
                    }
                }
                else
                {
                    pendingGesture = "Neutral";
                    pendingGestureCount = 1;
                }
                return;
            }

            // Apply gesture change hysteresis to prevent rapid switching
            if (votedGesture != currentGesture)
            {
                if (votedGesture == pendingGesture)
                {
                    pendingGestureCount++;
                    
                    if (pendingGestureCount >= GESTURE_CHANGE_THRESHOLD)
                    {
                        // Confirmed gesture change
                        Debug.Log($"[AIServerInterface] Gesture change: {currentGesture} -> {votedGesture} (conf: {avgConfidence:F2})");
                        currentGesture = votedGesture;
                        currentGestureProb = avgConfidence.ToString("F2");
                        pendingGestureCount = 0;
                        pendingGesture = "Unknown";
                    }
                    else
                    {
                        Debug.Log($"[AIServerInterface] Pending gesture change to {votedGesture} ({pendingGestureCount}/{GESTURE_CHANGE_THRESHOLD})");
                    }
                }
                else
                {
                    // New pending gesture
                    pendingGesture = votedGesture;
                    pendingGestureCount = 1;
                    Debug.Log($"[AIServerInterface] New pending gesture: {votedGesture} ({pendingGestureCount}/{GESTURE_CHANGE_THRESHOLD})");
                }
            }
            else
            {
                // Same gesture, update confidence and reset pending
                currentGestureProb = avgConfidence.ToString("F2");
                pendingGestureCount = 0;
                pendingGesture = "Unknown";
            }
        }
    }

    public string GetCurrentGesture() => currentGesture;
    public string GetCurrentGestureProb() => currentGestureProb;
    public bool IsBuffering() => isBuffering;

    /// <summary>
    /// Clear the server-side buffer for this session (useful for resetting)
    /// </summary>
    public async Task ClearSession()
    {
        try
        {
            string json = $@"{{""session_id"": ""{sessionId}""}}";
            using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/session/clear", "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                
                UnityWebRequestAsyncOperation operation = www.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[AIServerInterface] Session cleared successfully");
                    isBuffering = true;
                    previousGesture.Clear();
                    currentGesture = "Unknown";
                    currentGestureProb = "Uncertain";
                    pendingGesture = "Unknown";
                    pendingGestureCount = 0;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AIServerInterface] Failed to clear session: " + e.Message);
        }
    }

    // Helper classes for JSON parsing
    [System.Serializable]
    private class PredictionResponse
    {
        public string label;
        public float prob;
    }

    [System.Serializable]
    private class PredictionItem
    {
        public string status;           // "buffering" or "predicted"
        public int sample_index;        // Index in the batch
        public int samples_needed;      // (buffering only)
        public int buffer_size;         // (buffering only)
        public string label;            // (predicted only)
        public float prob;              // (predicted only)
        public List<TopKItem> topk;     // (predicted only)
        public string detection_method; // "gate" or "gesture" - how was this prediction made
        public float gate_rest_prob;    // Gate model's rest probability (if available)
        public float gate_gesture_prob; // Gate model's gesture probability (if available)
    }

    [System.Serializable]
    private class BatchWindowedResponse
    {
        public string session_id;
        public int total_samples;
        public List<PredictionItem> predictions;
        public bool buffer_ready;
    }

    [System.Serializable]
    private class TopKItem
    {
        public string label;
        public float prob;
    }
}
