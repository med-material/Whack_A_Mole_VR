using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // The size of the wall
    [SerializeField]
    private Vector2 wallSize;

    // Coefficient of the curvature of the wall. The higher the coefficient, the less curved is the wall
    [SerializeField]
    private float curveCoeff = 20f;

    // Offest of the height of the wall
    [SerializeField]
    private float heightOffset;

    private Vector3 wallCenter;

    private List<Mole> moles = new List<Mole>();
    private bool active = false;

    void Start()
    {
        wallCenter = new Vector3(wallSize.x/2f, wallSize.y/2f, 0);
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
    }

    // Activates a random Mole for a given lifeTime and s fake or not
    public void ActivateMole(float lifeTime, bool isFake)
    {
        if (!active) return;

        GetRandomMole().Enable(lifeTime, isFake);
    }

    // Pauses/unpauses the moles
    public void SetPauseMole(bool pause)
    {
        foreach(Mole mole in moles)
        {
            mole.SetPause(pause);
        }
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
        // Updates the wallCenter value
        wallCenter = new Vector3(wallSize.x/2f, wallSize.y/2f, 0);

        // For each row and column:
        for (int x = 0; x < columnCount; x++)
        {
            for (int y = 0; y < rowCount; y++)
            {
                // Instanciates a Mole object
                Mole mole = Instantiate(moleObject, transform);
                // Get the Mole object's local position depending on the current row, column and the curve coefficient
                Vector3 molePos = DefineMolePos(x, y);

                // Sets the Mole local position, rotates it so it looks away from the wall (affected by the curve)
                mole.transform.localPosition = molePos;
                mole.transform.localRotation = DefineMoleRotation(new Vector2(molePos.x, molePos.z));
                // Sets the Mole ID and references it
                mole.SetId(GetMoleId(x, y));
                moles.Add(mole);
            }
        }
    }

    // Gets the Mole position depending on its index, the wall size (x and y axes of the vector3), and also on the curve coefficient (for the z axis).
    private Vector3 DefineMolePos(int xIndex, int yIndex)
    {
        return new Vector3((((float)xIndex/(columnCount - 1)) * wallSize.x) - wallCenter.x, (((float)yIndex/(rowCount - 1)) * wallSize.y) + heightOffset - wallCenter.y, -Mathf.Pow(xIndex - (columnCount/2), 2) * wallSize.x / curveCoeff);
    }

    private int GetMoleId(int xIndex, int yIndex)
    {
        return ((xIndex + 1) * 100) + (yIndex + 1);
    }

    // Gets the Mole rotation so it is always looking away from the wall, depending on its X local position and the wall's curvature (curveCoeff)
    private Quaternion DefineMoleRotation(Vector2 molePosXZ)
    {
        Quaternion lookAngle = new Quaternion();
        lookAngle.eulerAngles = new Vector3(0f, -Vector2.SignedAngle(Vector2.right, new Vector2(molePosXZ.x - wallCenter.x, molePosXZ.y - wallCenter.z)), 0f);
        if (molePosXZ.x - wallCenter.x < 0)
        {
            lookAngle.eulerAngles += new Vector3(0f, 180, 0f);
        }
        return lookAngle;
    }
}
