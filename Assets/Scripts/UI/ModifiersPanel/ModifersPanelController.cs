using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifersPanelController : MonoBehaviour
{
    [SerializeField]
    private TherapistUi therapistUi;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchToTherapistPanel() {
        therapistUi.SwitchPanel(PanelChoice.TherapistPanel);
    }
}
