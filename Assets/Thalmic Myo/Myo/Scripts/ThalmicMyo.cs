using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Arm = Thalmic.Myo.Arm;
using XDirection = Thalmic.Myo.XDirection;
using VibrationType = Thalmic.Myo.VibrationType;
using Pose = Thalmic.Myo.Pose;
using UnlockType = Thalmic.Myo.UnlockType;
using StreamEmg = Thalmic.Myo.StreamEmg;

public class ThalmicMyo : MonoBehaviour
{
    public Thalmic.Myo.Myo _myo;

    public bool armSynced;
    public bool unlocked;
    public Arm arm;
    public XDirection xDirection;
    public Pose pose = Pose.Unknown;

    public static Vector3 accelerometer;
    public static Vector3 gyroscope;

    [SerializeField]
    public Thalmic.Myo.Result streamEmg;

    public Queue<int[]> emgBuffer = new Queue<int[]>();
    private object emgLock = new object();

    private bool isCollecting = false;
    private Coroutine emgCoroutine;

    [SerializeField]
    public static int[] emg;

    private Object _lock = new Object();

    private bool _myoArmSynced = false;
    private Arm _myoArm = Arm.Unknown;
    public XDirection _myoXDirection = XDirection.Unknown;
    public Thalmic.Myo.Quaternion _myoQuaternion = null;
    public Thalmic.Myo.Vector3 _myoAccelerometer = null;
    public Thalmic.Myo.Vector3 _myoGyroscope = null;
    public int[] _myoEmg = new int[7];
    public Pose _myoPose = Pose.Unknown;
    private bool _myoUnlocked = false;

    public StreamEmg _myoStreamEmg = StreamEmg.Enabled;

    public bool isPaired
    {
        get { return _myo != null; }
    }

    public void Vibrate(VibrationType type)
    {
        _myo.Vibrate(type);
    }

    public void Unlock(UnlockType type)
    {
        _myo.Unlock(type);
    }

    public void Lock()
    {
        _myo.Lock();
    }

    public void NotifyUserAction()
    {
        _myo.NotifyUserAction();
    }

    void Start()
    {
        if (isPaired)
        {
            streamEmg = _myo.SetStreamEmg(_myoStreamEmg);
            StartEmgCoroutine();
        }
    }

    void OnDestroy()
    {
        StopEmgCoroutine();
    }

    private void OnApplicationQuit()
    {
        StopEmgCoroutine();
    }

    void StartEmgCoroutine()
    {
        isCollecting = true;
        emgCoroutine = StartCoroutine(CollectEmgData());
    }

    public void StopEmgCoroutine()
    {
        isCollecting = false;
        if (emgCoroutine != null)
        {
            StopCoroutine(emgCoroutine); // Stop the coroutine when it's no longer needed
        }
    }

    IEnumerator CollectEmgData()
    {
        while (isCollecting)
        {
            if (_myo != null && streamEmg == Thalmic.Myo.Result.Success)
            {
                int[] emgData = _myo.emgData;

                if (emgData != null)
                {
                    lock (emgLock)
                    {
                        emgBuffer.Enqueue(emgData);
                        /*if (emgBuffer.Count > targetFrequency)
                        { // Limit buffer size to 1 second of data
                            emgBuffer.Dequeue();
                        }*/
                    }
                }
            }

            yield return new WaitForSecondsRealtime(0.005f); // 5ms delay to match 200Hz (1000ms / 200Hz = 5ms per sample)
        }
    }

    public int[] GetLatestEmgData()
    {
        lock (emgLock)
        {
            if (emgBuffer.Count > 0)
            {
                return emgBuffer.Peek(); // Peek at the latest EMG data in the buffer
            }
            return null; // Return null if no data is available yet
        }
    }

    void Update()
    {
        lock (_lock)
        {
            armSynced = _myoArmSynced;
            arm = _myoArm;
            xDirection = _myoXDirection;
            if (_myoQuaternion != null)
            {
                transform.localRotation = new Quaternion(_myoQuaternion.Y, _myoQuaternion.Z, -_myoQuaternion.X, -_myoQuaternion.W);
            }
            if (_myoAccelerometer != null)
            {
                accelerometer = new UnityEngine.Vector3(_myoAccelerometer.Y, _myoAccelerometer.Z, -_myoAccelerometer.X);
            }
            if (_myoGyroscope != null)
            {
                gyroscope = new UnityEngine.Vector3(_myoGyroscope.Y, _myoGyroscope.Z, -_myoGyroscope.X);
            }
            if (isPaired && streamEmg == Thalmic.Myo.Result.Success)
            {
                emg = _myo.emgData;
            }
            /*if (emgBuffer.Count > 0)
            {
                latestEmgData = emgBuffer.Dequeue();
            }*/

            pose = _myoPose;
            unlocked = _myoUnlocked;
        }
    }

    void myo_OnArmSync(object sender, Thalmic.Myo.ArmSyncedEventArgs e)
    {
        lock (_lock)
        {
            _myoArmSynced = true;
            _myoArm = e.Arm;
            _myoXDirection = e.XDirection;
        }
    }

    void myo_OnArmUnsync(object sender, Thalmic.Myo.MyoEventArgs e)
    {
        lock (_lock)
        {
            _myoArmSynced = false;
            _myoArm = Arm.Unknown;
            _myoXDirection = XDirection.Unknown;
        }
    }

    void myo_OnOrientationData(object sender, Thalmic.Myo.OrientationDataEventArgs e)
    {
        lock (_lock)
        {
            _myoQuaternion = e.Orientation;
        }
    }

    void myo_OnAccelerometerData(object sender, Thalmic.Myo.AccelerometerDataEventArgs e)
    {
        lock (_lock)
        {
            _myoAccelerometer = e.Accelerometer;
        }
    }

    void myo_OnGyroscopeData(object sender, Thalmic.Myo.GyroscopeDataEventArgs e)
    {
        lock (_lock)
        {
            _myoGyroscope = e.Gyroscope;
        }
    }

    private void myo_OnReceiveData(object sender, Thalmic.Myo.EmgDataEventArgs data)
    {
        lock (_lock)
        {
            _myoEmg = data.Emg;
        }
    }

    void myo_OnPoseChange(object sender, Thalmic.Myo.PoseEventArgs e)
    {
        lock (_lock)
        {
            //_myoPose = e.Pose;
        }
    }

    void myo_OnUnlock(object sender, Thalmic.Myo.MyoEventArgs e)
    {
        lock (_lock)
        {
            _myoUnlocked = true;
        }
    }

    void myo_OnLock(object sender, Thalmic.Myo.MyoEventArgs e)
    {
        lock (_lock)
        {
            _myoUnlocked = false;
        }
    }

    public Thalmic.Myo.Myo internalMyo
    {
        get { return _myo; }
        set
        {
            if (_myo != null)
            {
                _myo.EmgData -= myo_OnReceiveData;
                _myo.ArmSynced -= myo_OnArmSync;
                _myo.ArmUnsynced -= myo_OnArmUnsync;
                _myo.OrientationData -= myo_OnOrientationData;
                _myo.AccelerometerData -= myo_OnAccelerometerData;
                _myo.GyroscopeData -= myo_OnGyroscopeData;
                _myo.PoseChange -= myo_OnPoseChange;
                _myo.Unlocked -= myo_OnUnlock;
                _myo.Locked -= myo_OnLock;
            }
            _myo = value;
            if (value != null)
            {
                value.EmgData += myo_OnReceiveData;
                value.ArmSynced += myo_OnArmSync;
                value.ArmUnsynced += myo_OnArmUnsync;
                value.OrientationData += myo_OnOrientationData;
                value.AccelerometerData += myo_OnAccelerometerData;
                value.GyroscopeData += myo_OnGyroscopeData;
                value.PoseChange += myo_OnPoseChange;
                value.Unlocked += myo_OnUnlock;
                value.Locked += myo_OnLock;
            }
        }
    }
}
