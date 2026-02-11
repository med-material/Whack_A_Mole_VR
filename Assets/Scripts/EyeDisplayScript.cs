using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class EyeDisplayScript : MonoBehaviour
{
    [SerializeField]
    public TMP_Text leftField;
    [SerializeField]
    public TMP_Text rightField;

    [SerializeField]
    private EyeTracker tracker;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        leftField.text = $"Gaze Valid: {tracker.left.validGaze}\npos: {tracker.left.pos}\nrot: {tracker.left.rot}\nGeometry valid: {tracker.left.validGeo}\nOpen: {tracker.left.openness}\nSqueeze: {tracker.left.squeeze}\nWide: {tracker.left.wide}";
        rightField.text = $"Gaze Valid: {tracker.right.validGaze}\npos: {tracker.right.pos}\nrot: {tracker.right.rot}\nGeometry valid: {tracker.right.validGeo}\nOpen: {tracker.right.openness}\nSqueeze: {tracker.right.squeeze}\nWide: {tracker.right.wide}";
    }
}
