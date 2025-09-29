using System.Collections.Generic;
using UnityEngine;

// Abstract class for data providers (For e.g. SampleLogger)
public abstract class DataProvider : MonoBehaviour
{
    public abstract Dictionary<string, object> GetData();
}
