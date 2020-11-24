using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotorSpaceMirror : MonoBehaviour
{

    private LaserMapper laserMapper;

    [SerializeField]
    private LaserMapper laserMapperToMirror;

    // Start is called before the first frame update
    void Start()
    {
        laserMapper = this.GetComponent<LaserMapper>();
        laserMapperToMirror.onMotorSpaceChanged.AddListener(onMotorSpaceChanged);
    }

    // Update is called once per frame
    public void onMotorSpaceChanged(MotorSpaceInfo info)
    {
        laserMapper.SetMotorSpace(info);
    }
}
