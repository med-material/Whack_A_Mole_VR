using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotorSpaceMirror : MonoBehaviour
{

    private LaserMapper laserMapper;

    [SerializeField]
    private LaserMapper laserMapperToMirror;

    void Awake() {
        laserMapper = this.GetComponent<LaserMapper>();
    }

    // Start is called before the first frame update
    void Start()
    {
        laserMapperToMirror.onMotorSpaceChanged.AddListener(onMotorSpaceChanged);
        UpdateMotorSpaceToMirror(laserMapperToMirror);
    }

    // Update is called once per frame
    public void onMotorSpaceChanged(MotorSpaceInfo info)
    {
        info.pos = new Vector3(-info.pos.x, info.pos.y, info.pos.z);
        laserMapper.SetMotorSpace(info);
    }
    
    public void UpdateMotorSpaceToMirror(LaserMapper m) {
        laserMapperToMirror = m;
        MotorSpaceInfo motorspace = laserMapperToMirror.GetMotorSpace();
        motorspace.pos = new Vector3(-motorspace.pos.x, motorspace.pos.y, motorspace.pos.z);
        laserMapper.SetMotorSpace(motorspace);
    }
}
