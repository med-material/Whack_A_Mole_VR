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
        this.transform.position = new Vector3(info.pos.x * -1f, info.pos.y, info.pos.z);
        laserMapper.SetMotorSpaceWidth(info.width);
        laserMapper.SetMotorSpaceHeight(info.height);
    }
}
