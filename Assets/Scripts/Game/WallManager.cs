using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*
Spawns, references and activates the moles. Is the only component to directly interact with the moles.
*/

public class WallManager : MonoBehaviour
{
    // The Mole object to be loaded
    [SerializeField]
    private Mole moleObject;

    // The count of rows to generate
    [SerializeField]
    private int rowCount;

    // The count of columns to generate
    [SerializeField]
    private int columnCount;

    // Offest of the height of the wall
    [SerializeField]
    private float heightOffset;

    // The size of the wall
    [SerializeField]
    private Vector3 wallSize;

    // Coefficient of the X curvature of the wall. 1 = PI/2, 0 = straight line
    [SerializeField]
    [Range(0.1f, 1f)]
    private float xCurveRatio = 1f;

    // Coefficient of the Y curvature of the wall. 1 = PI/2, 0 = straight line
    [SerializeField]
    [Range(0.1f, 1f)]
    private float yCurveRatio = 1f;

    // The angle of the edge moles if a curve ratio of 1 is given
    [SerializeField]
    [Range(0f, 90f)]
    private float maxAngle = 90f;

    // The scale of the Mole. Idealy shouldn't be scaled on the Z axis (to preserve the animations)
    [SerializeField]
    private Vector3 moleScale = Vector3.one;

    private class StateUpdateEvent: UnityEvent<bool, List<Mole>> {};
    private StateUpdateEvent stateUpdateEvent = new StateUpdateEvent();
    private WallGenerator wallGenerator;
    private Vector3 wallCenter;
    private List<Mole> moles = new List<Mole>();
    private bool active = false;
    private bool isInit = false;
    private float updateCooldownDuration = .1f;

    void Start()
    {
        wallGenerator = gameObject.GetComponent<WallGenerator>();
        wallCenter = new Vector3(wallSize.x/2f, wallSize.y/2f, 0);
        isInit = true;
    }

    void OnValidate()
    {
        UpdateWall();
    }

    public void Enable()
    {
        active = true;

        if (moles.Count == 0)
        {
            GenerateWall();
        }
    }

    public void Disable()
    {
        active = false;
        disableMoles();
    }

    public void Clear()
    {
        active = false;
        DestroyWall();
        stateUpdateEvent.Invoke(false, moles);
    }

    // Activates a random Mole for a given lifeTime and s fake or not
    public void ActivateMole(float lifeTime, float moleExpiringDuration, bool isFake)
    {
        if (!active) return;

        GetRandomMole().Enable(lifeTime, moleExpiringDuration, isFake);
    }

    // Pauses/unpauses the moles
    public void SetPauseMole(bool pause)
    {
        foreach(Mole mole in moles)
        {
            mole.SetPause(pause);
        }
    }

    public UnityEvent<bool, List<Mole>> GetUpdateEvent()
    {
        return stateUpdateEvent;
    }

    // Returns a random, inactive Mole
    private Mole GetRandomMole()
    {
        Mole mole;
        do
        {
            mole = moles[Random.Range(0, moles.Count)];
        }
        while (mole.IsActive());
        return mole;
    }

    private void disableMoles()
    {
        foreach(Mole mole in moles)
        {
            mole.Reset();
        }
    }

    private void DestroyWall()
    {
        foreach(Mole mole in moles)
        {
            Destroy(mole.gameObject);
        }
        moles.Clear();
    }

    // Generates the wall of Moles
    private void GenerateWall()
    {
        wallGenerator.InitPointsLists(columnCount, rowCount);
        // Updates the wallCenter value
        wallCenter = new Vector3(wallSize.x/2f, wallSize.y/2f, 0);

        // For each row and column:
        for (int x = 0; x < columnCount; x++)
        {
            for (int y = 0; y < rowCount; y++)
            {
                if((x == 0 || x == columnCount - 1) && (y == rowCount - 1 || y == 0))
                {
                    wallGenerator.AddPoint(x, y, DefineMolePos(x, y), DefineMoleRotation(x, y));
                    continue;
                }

                // Instanciates a Mole object
                Mole mole = Instantiate(moleObject, transform);
                // Get the Mole object's local position depending on the current row, column and the curve coefficient
                Vector3 molePos = DefineMolePos(x, y);

                // Sets the Mole local position, rotates it so it looks away from the wall (affected by the curve)
                mole.transform.localPosition = molePos;
                mole.transform.localRotation = DefineMoleRotation(x, y);
                // Sets the Mole ID, scale and references it
                mole.SetId(GetMoleId(x, y));
                mole.transform.localScale = moleScale;
                moles.Add(mole);

                wallGenerator.AddPoint(x, y, molePos, mole.transform.localRotation);
            }
        }
        stateUpdateEvent.Invoke(true, moles);
        wallGenerator.GenerateWall();
    }

    // Updates the wall
    private void UpdateWall()
    {
        if (!(active && isInit)) return;
        StopAllCoroutines();
        StartCoroutine(WallUpdateCooldown());
    }

    // Gets the Mole position depending on its index, the wall size (x and y axes of the vector3), and also on the curve coefficient (for the z axis).
    private Vector3 DefineMolePos(int xIndex, int yIndex)
    {
        float angleX = ((((float)xIndex/(columnCount - 1)) * 2) - 1) * ((Mathf.PI * xCurveRatio) / 2);
        float angleY = ((((float)yIndex/(rowCount - 1)) * 2) - 1) * ((Mathf.PI * yCurveRatio) / 2);

        return new Vector3(Mathf.Sin(angleX) * (wallSize.x / (2 * xCurveRatio)), Mathf.Sin(angleY) * (wallSize.y / (2 * yCurveRatio)), ((Mathf.Cos(angleY) * (wallSize.z)) + (Mathf.Cos(angleX) * (wallSize.z))));
    }

    private int GetMoleId(int xIndex, int yIndex)
    {
        return ((xIndex + 1) * 100) + (yIndex + 1);
    }

    // Gets the Mole rotation so it is always looking away from the wall, depending on its X local position and the wall's curvature (curveCoeff)
    private Quaternion DefineMoleRotation(int xIndex, int yIndex)
    {
        Quaternion lookAngle = new Quaternion();
        lookAngle.eulerAngles = new Vector3(-((((float)yIndex/(rowCount - 1)) * 2) - 1) * (maxAngle * yCurveRatio), ((((float)xIndex/(columnCount - 1)) * 2) - 1) * (maxAngle * xCurveRatio), 0f);
        return lookAngle;
    }

    private IEnumerator WallUpdateCooldown()
    {
        yield return new WaitForSeconds(updateCooldownDuration);

        if(active)
        {
            Clear();
            Enable();
        }
    }
}
