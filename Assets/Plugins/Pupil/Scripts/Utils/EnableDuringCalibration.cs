using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PupilLabs
{
    public class EnableDuringCalibration : MonoBehaviour
    {

        public CalibrationController controller;
        [FormerlySerializedAs("renderer")]
        public MeshRenderer targetRenderer;

        void Awake()
        {
            controller.OnCalibrationStarted += EnableMePls;
            controller.OnCalibrationRoutineDone += DisableMePls;
        }

        void OnDestroy()
        {
            controller.OnCalibrationStarted -= EnableMePls;
            controller.OnCalibrationRoutineDone -= DisableMePls;
        }

        void EnableMePls()
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = true;
            }
        }

        void DisableMePls()
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = false;
            }
        }
    }
}
