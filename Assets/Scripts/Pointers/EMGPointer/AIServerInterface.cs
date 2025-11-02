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

    public AIServerInterface(ThalmicMyo myo)
    {
        thalmicMyo = myo;
    }

    public void StartPredictionRequestCoroutine()
    {
        if (gestureResponses.Count >= bufferSize) { GestureProcess(); }

        // Don't forget to run the python server before using this feature. Can be found in a separate repository.
        _ = SendPredictionRequest(thalmicMyo._myoEmg);
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
        else currentGestureProb = "Uncertain";
    }

    private async Task SendPredictionRequest(int[] emgs)
    {
        // Build the JSON using the actual EMG values
        string json = $@"{{
            ""features"": {{
                ""EMG1"": {emgs[0]},
                ""EMG2"": {emgs[1]},
                ""EMG3"": {emgs[2]},
                ""EMG4"": {emgs[3]},
                ""EMG5"": {emgs[4]},
                ""EMG6"": {emgs[5]},
                ""EMG7"": {emgs[6]},
                ""EMG8"": {emgs[7]}
            }}
        }}";

        try
        {
            using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/predict", "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                UnityWebRequestAsyncOperation operation = www.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error: " + www.error);
                }
                else
                {
                    string responseText = www.downloadHandler.text;
                    PredictionResponse result = null;
                    try
                    {
                        result = JsonUtility.FromJson<PredictionResponse>(responseText);
                    }
                    catch
                    {
                        Debug.Log("Failed to parse prediction response: " + responseText);
                    }
                    if (result != null)
                    {
                        gestureResponses.Add(result);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("Exception during prediction request: " + e.Message);
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
    private class TopKItem
    {
        public string label;
        public float prob;
    }
}
