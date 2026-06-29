using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.HUD
{
    internal class DynamicReversedArrowIndicator : OutOfBoundIndicator
    {
        [SerializeField]
        private WallManager wallManager;

        [SerializeField]
        private BasicPointer controller;

        [SerializeField]
        private CanvasGroup arrow;

        [SerializeField]
        public Color Color; // The color of the arrow //doing nothing with this yet


        private Coroutine coroutine;
        private bool active = false;
        private bool lastEnter = true;
        private Side lastSide;
        private WallInfo wallInfo;
        private float fadeTime = 0.35f;

        private void Awake()
        {
            //arrow = GetComponent<CanvasGroup>();
            arrow.gameObject.SetActive(false); // Ensure the arrow is disabled by default
            wallInfo = wallManager.CreateWallInfo();
        }

        private void OnEnable()
        {
            wallManager.stateUpdateEvent.AddListener(OnWallUpdated);
            OnWallUpdated(wallManager.CreateWallInfo());
        }

        
        private void OnDisable()
        {
            wallManager.stateUpdateEvent.RemoveListener(OnWallUpdated);
        }

        private void Update()
        {
            if (arrow.gameObject.activeInHierarchy) // Only update rotation if the arrow is active
            {
                // Update arrow position to the center of the wall
                arrow.transform.position = new Vector3(wallInfo.meshCenter.x, wallInfo.meshCenter.y, arrow.transform.position.z);
                Vector3 controllerPosition = controller.MappedPosition; // The position of the pointer when the method is called
                Vector3 targetPosition = new Vector3(controllerPosition.x, controllerPosition.y, arrow.transform.position.z);

                // Calculate direction from arrow to target position
                Vector3 direction = (targetPosition - arrow.transform.position).normalized;

                // Calculate angle in degrees
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                // Apply the rotation to the child GameObject
                arrow.transform.Find("RotationOffset").transform.eulerAngles = new Vector3(0, 0, angle - 90);

            }
        }




        /// <summary>Displays an arrow that points towards a target position. The arrow is positioned so that it points towards the target, and it fades in over a specified amount of time.</summary>
        /// <param name="position">The position of the target.</param>
        /// <param name="motorSpaceCenter">The center of the motor space.</param>
        /// <param name="side">The side of the wall that the target is on.</param>
        /// <remarks>This method is called by the <see cref="BubbleDisplay"/> when the user exiting the NotorSpace.</remarks>
        internal override void ShowIndicator(Vector3 position, Vector3 motorSpaceCenter, Side side)
        {
            if (!arrow.gameObject.activeInHierarchy) // Only show the indicator if it's not already shown
            {
                arrow.gameObject.SetActive(true); // Enable the arrow

                arrow.transform.rotation = Quaternion.identity; // Reset the rotation
                
                Debug.Log("SHOW INDICATOR Wall center position: " + wallInfo.meshCenter);
                
                //reset position
                arrow.transform.position = new Vector3(wallInfo.meshCenter.x, wallInfo.meshCenter.y, arrow.transform.position.z);
                Vector3 pointerPosition = position; // The position of the pointer when the method is called
                arrow.transform.right = pointerPosition - arrow.transform.position;


                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
                coroutine = FadingUtils.FadeRoutine(handler: this, Obj: arrow.gameObject, fadeTime: fadeTime, fadeDirection: FadeAction.In);
            }
        }


        internal override void HideIndicator()
        {
            if (arrow.gameObject.activeInHierarchy) // Only hide the indicator if it's currently shown
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
                coroutine = FadingUtils.FadeRoutine(handler: this, Obj: arrow.gameObject, fadeTime: fadeTime, fadeDirection: FadeAction.Out);
                arrow.gameObject.SetActive(false); // Disable the arrow
            }
        }

        private void OnWallUpdated(WallInfo w)
        {
            wallInfo = w;
            active = wallInfo.active;
            if (!lastEnter && active)
            {
                ShowIndicator(Vector3.zero, w.wallCenter, lastSide);
            }
            else
            {
                HideIndicator();
            }
        }
    }
}
