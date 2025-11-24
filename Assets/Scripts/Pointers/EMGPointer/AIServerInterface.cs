using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class AIServerInterface
{
    private ThalmicMyo thalmicMyo;
    private int memorySize = 6;
    private int bufferSize = 50;
    private string currentGesture = "Unknown";
    private string currentGestureProb = "Uncertain";
    private List<PredictionResponse> gestureResponses = new List<PredictionResponse>();
    private Queue<PredictionResponse> previousGesture = new Queue<PredictionResponse>();

    // Batch prediction settings
    private int batchSize = 10;
    private List<int[]> emgBatch = new List<int[]>();
    private bool isProcessingRequest = false;

    public AIServerInterface(ThalmicMyo myo)
    {
        thalmicMyo = myo;
    }

    public void StartPredictionRequestCoroutine()
    {
        if (gestureResponses.Count >= bufferSize) { GestureProcess(); }

        // Collect EMG samples into batch
        int[] currentEmgData = new int[8];
        System.Array.Copy(thalmicMyo._myoEmg, currentEmgData, 8);
        emgBatch.Add(currentEmgData);

        // Send batch when it reaches the batch size
        if (emgBatch.Count >= batchSize && !isProcessingRequest)
        {
            _ = SendBatchPredictionRequest(new List<int[]>(emgBatch));
            emgBatch.Clear();
        }
    }

    private void GestureProcess()
    {
        if (gestureResponses == null || gestureResponses.Count == 0) return;

        // Get the most common label from the responses
        string mostCommonLabel = gestureResponses.GroupBy(i => i)
                .OrderByDescending(grp => grp.Count())
                .Select(grp => grp.Key).First().label;

        // Compute mean probability for the most common label
        float meanProb = gestureResponses.Where(r => r.label == mostCommonLabel).Average(r => r.prob);

        // Clear collected responses for next round
        gestureResponses.Clear();

        PredictionResponse newResponse = new PredictionResponse
        {
            label = mostCommonLabel,
            prob = meanProb,
            topk = null,
        };

        // Update the current gesture in memory
        if (previousGesture.Count >= memorySize) { previousGesture.Dequeue(); }
        previousGesture.Enqueue(newResponse);

        // If a gesture appears more than half the time in the memory, update the current gesture
        if (previousGesture.Count(i => i.label == newResponse.label) >= memorySize / 2)
        {
            currentGesture = mostCommonLabel;
            currentGestureProb = meanProb.ToString();
        }
        else
        {
            currentGestureProb = "Uncertain";
            currentGesture = "Unknown";
        }
    }

    private async Task SendBatchPredictionRequest(List<int[]> emgBatch)
    {
        if (isProcessingRequest) return;
        isProcessingRequest = true;

        // Build the batch JSON
        List<string> batchFeatures = new List<string>();
        foreach (int[] emgs in emgBatch)
        {
            string features = $@"{{
                ""EMG1"": {emgs[0]},
                ""EMG2"": {emgs[1]},
                ""EMG3"": {emgs[2]},
                ""EMG4"": {emgs[3]},
                ""EMG5"": {emgs[4]},
                ""EMG6"": {emgs[5]},
                ""EMG7"": {emgs[6]},
                ""EMG8"": {emgs[7]}
            }}";
            batchFeatures.Add(features);
        }
        
        string json = $@"{{""batch"": [{string.Join(",", batchFeatures)}]}}";

        try
        {
            using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/predict_batch", "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = 10; // Increased timeout for batch requests

                UnityWebRequestAsyncOperation operation = www.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[AIServerInterface] Batch prediction request error: " + www.error);
                }
                else
                {
                    string responseText = www.downloadHandler.text;
                    BatchPredictionResponse result = null;
                    try
                    {
                        result = JsonUtility.FromJson<BatchPredictionResponse>(responseText);
                    }
                    catch
                    {
                        Debug.LogWarning("[AIServerInterface] Failed to parse batch prediction response: " + responseText);
                    }
                    if (result != null && result.predictions != null)
                    {
                        gestureResponses.AddRange(result.predictions);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AIServerInterface] Exception during batch prediction request: " + e.Message);
        }
        finally
        {
            isProcessingRequest = false;
        }
    }

    public string GetCurrentGesture() => currentGesture;
    public string GetCurrentGestureProb() => currentGestureProb;

    // Helper classes for JSON parsing
    [System.Serializable]
    private class PredictionResponse
    {
        public string label;
        public float prob;
        public List<TopKItem> topk;
    }

    [System.Serializable]
    private class BatchPredictionResponse
    {
        public List<PredictionResponse> predictions;
    }

    [System.Serializable]
    private class TopKItem
    {
        public string label;
        public float prob;
    }
}
