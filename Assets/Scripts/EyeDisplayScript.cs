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
    public Transform reticleL;
    [SerializeField]
    public Transform reticleR;

    [SerializeField]
    private HTCGazeLogger tracker;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Dictionary<string, object> data = tracker.GetData();
        rightField.text = "hell" + "o!";// $"GazeValid: {data["GazeValid1"]}\n" +
        leftField.text = $"GazeValid: {data["GazeValid0"]}\n" +
            $"Pos: {data["LocalEyePosition0X"]}, {data["LocalEyePosition0Y"]}, {data["LocalEyePosition0Z"]}\n" +
            $"Rot: {data["LocalEyeRotation0X"]}, {data["LocalEyeRotation0Y"]}, {data["LocalEyeRotation0Z"]}, {data["LocalEyeRotation0W"]}\n" +
            $"Hit: {data["GazeHit0"]} - {data["GazeHitObject0"]}\n" +
            $"At : {data["GazeHitPosition0X"]}, {data["GazeHitPosition0Y"]}, {data["GazeHitPosition0Z"]}\n";
        rightField.text = $"GazeValid: {data["GazeValid1"]}\n" +
            $"Pos: {data["LocalEyePosition1X"]}, {data["LocalEyePosition1Y"]}, {data["LocalEyePosition1Z"]}\n" +
            $"Rot: {data["LocalEyeRotation1X"]}, {data["LocalEyeRotation1Y"]}, {data["LocalEyeRotation1Z"]}, {data["LocalEyeRotation1W"]}\n" +
            $"Hit: {data["GazeHit1"]} - {data["GazeHitObject1"]}\n" +
            $"At : {data["GazeHitPosition1X"]}, {data["GazeHitPosition1Y"]}, {data["GazeHitPosition1Z"]}\n";
        reticleL.position = new Vector3(
            (float)data["GazeHitPosition0X"],
            (float)data["GazeHitPosition0Y"],
            (float)data["GazeHitPosition0Z"]
            );
        reticleR.position = new Vector3(
            (float)data["GazeHitPosition1X"],
            (float)data["GazeHitPosition1Y"],
            (float)data["GazeHitPosition1Z"]
            );
    }
}
