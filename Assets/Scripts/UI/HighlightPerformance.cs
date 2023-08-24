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
        List<PerfData> perfData = new List<PerfData>();
        perfData.Add(performanceManager.GetPerfData(ControllerName.Right));
        perfData.Add(performanceManager.GetPerfData(ControllerName.Left));

        foreach (var data in perfData) {
            var judges = data.lastJudges;
            if (judges.Count > 0) {
                foreach (float judge in judges) {
                    progressBar.AddElement(judge, true);
                }
                progressBar.Show();
            }
        }
        performanceManager.ResetPerfData();
    }
}
