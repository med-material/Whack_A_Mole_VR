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
    [SerializeField] private string currentGesture = "None";
    private List<string> gestureResponses = new List<string>();
    private Queue<string> previousGesture = new Queue<string>();

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
        // Get the most common gesture from the responses
        string newGesture = gestureResponses.GroupBy(i => i)
                .OrderByDescending(grp => grp.Count())
                .Select(grp => grp.Key).First();
        gestureResponses.Clear();

        // Update the current gesture in memory
        if (previousGesture.Count >= memorySize) { previousGesture.Dequeue(); }
        previousGesture.Enqueue(newGesture);

        // If a gesture appears more than half the time in the memory, update the current gesture
        if (previousGesture.Count(i => i == newGesture) >= memorySize / 2) { currentGesture = newGesture; }
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
                        gestureResponses.Add(result.label);
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
