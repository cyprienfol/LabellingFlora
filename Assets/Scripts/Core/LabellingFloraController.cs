using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using Valve.VR;
using LabellingFlora.Data;

namespace LabellingFlora.Core
{
    public class LabellingFloraController : MonoBehaviour
    {
        // Private fields.
        [Header("HMD Info")]
        [SerializeField]
        private GameObject _player;
        [SerializeField]
        private SteamVR_Input_Sources _rightHand;
        [SerializeField]
        private SteamVR_Input_Sources _leftHand;
        [SerializeField]
        private GameObject _cursor;

        [Header("Point Cloud Info")]
        [SerializeField]
        private PointCloudManager _loader;
        // Public fields. 
        public TextMeshPro microhabitatText;

        [Header("List of SteamVR Actions")]
        public SteamVR_ActionSet EgocentricActionSet;
        public SteamVR_Action_Boolean RightTriggerPressed;
        public SteamVR_Action_Boolean RightMenuButtonPressed;
        public SteamVR_Action_Boolean LeftMenuButtonPressed;
        public SteamVR_Action_Boolean RightTrackpadPressed;
        public SteamVR_Action_Vector2 RightTrackpadPosition;
        public SteamVR_Action_Boolean LeftTrackpadPressed;
        public SteamVR_Action_Vector2 LeftTrackpadPosition;


        // Start is called before the first frame update.
        void OnEnable()
        {
            //ChangePointCloudColor.AddOnStateDownListener(MenuButtonDown, handType);
            //ScalePointCloud.AddOnAxisListener(TrackPadDown, handType);
        }

        private void OnDisable()
        {
            //if (ChangePointCloudColor != null)
            //    ChangePointCloudColor.RemoveOnStateDownListener(MenuButtonDown, handType);
        }

        void Start()
        {
            EgocentricActionSet.Activate();

        }

        /// <summary>
        /// Get current position of the spherical cursor
        /// </summary>
        public Vector3 GetCursorPosition()
        {
            return _cursor.GetComponent<Transform>().position;
        }

        /// <summary>
        /// Get current radius of the spherical cursor
        /// </summary>
        public float GetCursorRadius()
        {
            return _cursor.GetComponent<Transform>().lossyScale.x / 2f;
        }

        /// <summary>
        /// Get current color of the spherical cursor
        /// </summary>
        public Vector3 GetCursorColor()
        {
            Color cursorColor = _cursor.GetComponent<MeshRenderer>().material.color;
            return new Vector3(cursorColor.r, cursorColor.g, cursorColor.b);
        }

        /// <summary>
        /// Create the logic to change the cursor size with the Right Trackpad 
        /// </summary>
        public void ChangeCursorRadius()
        {
            if (RightTrackpadPosition.GetAxis(_rightHand).y > 0.1f)
            {
                if (_cursor.GetComponent<Transform>().localScale.x < 0.15)
                    _cursor.GetComponent<Transform>().localScale += new Vector3(0.01f, 0.01f, 0.01f);
            }
            else if (RightTrackpadPosition.GetAxis(_rightHand).y < -0.1f)
            {
                if (_cursor.GetComponent<Transform>().localScale.x > 0.05)
                    _cursor.GetComponent<Transform>().localScale -= new Vector3(0.01f, 0.01f, 0.01f);
            }
        }

        /// <summary>
        /// Create the logic to change the tree height with the Left Trackpad 
        /// </summary>
        public void ChangeTreeStage()
        {
            if (LeftTrackpadPosition.GetAxis(_leftHand).y > 0.1f)
            {

                if (_player.GetComponent<Transform>().position.y < 4.0f)
                {
                    _player.transform.position = new Vector3(_player.transform.position.x,
                        _player.transform.position.y + 1.0f, _player.transform.position.z);
                }
            }
            else if (LeftTrackpadPosition.GetAxis(_leftHand).y < -0.1f)
            {
                if (_player.GetComponent<Transform>().position.y > 0.5)
                {
                    _player.transform.position = new Vector3(_player.transform.position.x,
                        _player.transform.position.y - 1.0f, _player.transform.position.z);
                }
            }
        }

        //public void RotateTree()
        //{
        //    if (LeftTrackpadPosition.GetAxis(_leftHand).x > 0.1f)
        //    {
        //        _loader.RotateClockwisePointCloud(15.0f);
        //    }
        //    else if (LeftTrackpadPosition.GetAxis(_leftHand).x < -0.1f)
        //    {
        //        _loader.RotateCounterClockwisePointCloud(15.0f);
        //    }
        //}

        public void ChangeCursorColor(MicrohabitatLabel label)
        {
            switch (label)
            {
                case MicrohabitatLabel.None:
                    _cursor.GetComponent<MeshRenderer>().material.color = Color.grey;
                    microhabitatText.text = "None";
                    break;
                case MicrohabitatLabel.Cavity:
                    _cursor.GetComponent<MeshRenderer>().material.color = Color.yellow;
                    microhabitatText.text = "Cavity";
                    break;
                case MicrohabitatLabel.Bark:
                    _cursor.GetComponent<MeshRenderer>().material.color = Color.cyan;
                    microhabitatText.text = "Bark";
                    break;
                case MicrohabitatLabel.Epiphytic:
                    _cursor.GetComponent<MeshRenderer>().material.color = Color.magenta;
                    microhabitatText.text = "Epiphytic";
                    break;
                case MicrohabitatLabel.Fungi:
                    _cursor.GetComponent<MeshRenderer>().material.color = Color.green;
                    microhabitatText.text = "Fungi";
                    break;
            }
        }
        // Activate the labelling logic if right trigger is pressed
        public bool IsLabelling()
        {
            return RightTriggerPressed.GetState(_rightHand);
        }

        /// <summary>
        /// Create the logic to change the cursor color with the Right menu button 
        /// </summary>
        // Switch label
        public bool ChangeLabel()
        {
            return RightMenuButtonPressed.GetStateDown(_rightHand);
        }

        /// <summary>
        /// Check is the right trackpad has been pressed 
        /// </summary>
        public bool ChangeCursorSize()
        {
            return RightTrackpadPressed.GetStateDown(_rightHand);
        }

        /// <summary>
        /// Check is the left trackpad has been pressed 
        /// </summary>
        public bool ChangeHeightTree()
        {
            return LeftTrackpadPressed.GetStateDown(_leftHand);
        }

        //public bool isRotatingTree()
        //{
        //    return LeftTrackpadPressed.GetStateDown(_leftHand);
        //}

        /// <summary>
        /// Check if the left menu button has been pressed 
        /// </summary>
        public bool ChangeMask()
        {
            return LeftMenuButtonPressed.GetStateDown(_leftHand);
        }

        /// <summary>
        /// Attempt of implementation for scalling the point cloud with a controller
        /// </summary>
        //private void TrackPadDown(SteamVR_Action_Vector2 fromAction, SteamVR_Input_Sources fromSource, Vector2 axis, Vector2 delta)
        //{
  
        //    if (axis.y > 0.5)
        //        _loader.ScaleUpPointCloud(0.05f);
        //    else
        //        _loader.ScaleDownPointCloud(0.05f);

        //} 

    }
}
