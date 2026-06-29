using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.HUD
{
    /// <summary>
    /// This code defines an abstract class named OutOfBoundIndicator that inherits from MonoBehaviour. It contains two abstract methods: ShowIndicator and HideIndicator.
    // These methods are used to show and hide an indicator when an object goes out of bounds. The ShowIndicator method takes in three parameters: position, motorSpaceCenter, and side. 
    // The position parameter is the position of the object that went out of bounds, motorSpaceCenter is the center of the motor space, and side is the side of the motor space where the object went out of bounds. 
    // The HideIndicator method takes no parameters.
    // This abstract class can be implemented to create different types of indicators that can be used to show when an object goes out of bounds.
    /// </summary>
    internal abstract class OutOfBoundIndicator : MonoBehaviour
    {
        internal abstract void ShowIndicator(Vector3 position, Vector3 motorSpaceCenter, Side side);
        internal abstract void HideIndicator();
    }

}
