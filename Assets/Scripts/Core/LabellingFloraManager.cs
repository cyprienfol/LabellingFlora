using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LabellingFlora.Data;


namespace LabellingFlora.Core
{
    public class LabellingFloraManager : MonoBehaviour
    {
        [Tooltip("VR Controller Manager")]
        [SerializeField]
        private LabellingFloraController controllerManager;

        [Tooltip("Point Cloud Manager")]
        [SerializeField]
        private PointCloudManager pointCloudLoader;

        /// <summary>
        /// General Update loop of the application
        /// </summary>
        
        void Update()
        {
            // Check if the user is labelling
            if (controllerManager.IsLabelling())
            {
                    pointCloudLoader.Labelling(controllerManager.GetCursorPosition(),
                        controllerManager.GetCursorRadius(), controllerManager.GetCursorColor());
            }

            // Check if the user wants to change color channel for the point cloud
            if (controllerManager.ChangeMask())
            {
                //pointCloudLoader.ChangeLayer();
                pointCloudLoader.SwitchLayer();
            }

            // Check if the user wants to change label
            if (controllerManager.ChangeLabel())
            {
                PointCloudManager.CurrentLabel++;
                if ((int)PointCloudManager.CurrentLabel >= 5)
                {
                    PointCloudManager.CurrentLabel = MicrohabitatLabel.None;
                }
                controllerManager.ChangeCursorColor(PointCloudManager.CurrentLabel);

            }

            // Check if the user wants to modify the size of the spherical sphere
            if (controllerManager.ChangeCursorSize())
            {
                controllerManager.ChangeCursorRadius();
            
            }

            // Check if the user wants to move the tree up or down
            if (controllerManager.ChangeHeightTree())
            {
                controllerManager.ChangeTreeStage();

            }

            // Check if the user wants to rotate the point cloud 
            //if (controllerManager.isRotatingTree())
            //{
            //    controllerManager.RotateTree();
            //}
        }
    }
}