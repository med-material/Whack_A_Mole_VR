using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMole : MonoBehaviour
{
    private Mole mole;
    // Start is called before the first frame update
    void Start()
    {
        mole = gameObject.GetComponent<Mole>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            Debug.Log("Enable");
            mole.Enable(2);
        }
        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("HoverEnter");
            mole.OnHoverEnter();
        }
        if (Input.GetButtonDown("Fire2"))
        {
            Debug.Log("HoverLeave");
            mole.OnHoverLeave();
        }
        if (Input.GetButtonDown("Submit"))
        {
            Debug.Log("Pop");
            mole.Pop();
        }
    }
}
