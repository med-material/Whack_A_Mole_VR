using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightPerformance : MonoBehaviour
{

    [SerializeField]
    private ProgressBar progressBar;
    
    private PerformanceManager performanceManager;

    // Start is called before the first frame update
    void Awake()
    {
        performanceManager = FindObjectOfType<PerformanceManager>();
        progressBar = this.GetComponent<ProgressBar>();
        
    }

    void Start() {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowResults() {
        progressBar.Reset();
        Queue<float> taskSpeeds = performanceManager.GetTaskFeedbacks();

        if (taskSpeeds.Count > 0) {
            foreach (float speed in taskSpeeds) {
                progressBar.AddElement(speed, true);
            }
            progressBar.Show();
        }
    }
}
