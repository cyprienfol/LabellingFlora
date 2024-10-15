using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace LabellingFlora.Data
{
    public class PointCloudViewer
    {
        private float3[] _unityPositions;
        private float3[] _colors;
        public uint[] Categories;
        private uint _pointTotal;

        public PointCloudViewer(float3[] unityPositions, float3[] colors, uint[] categories)
        {
            // TODO: check consistency in the arrays size 
            _pointTotal = (uint)unityPositions.Length;
            _unityPositions = new float3[_pointTotal];
            _colors = new float3[_pointTotal];
            Categories = new uint[_pointTotal];

            for (int index = 0; index < _pointTotal; index++)
            {
                _unityPositions[index] = unityPositions[index];
                _colors[index] = colors[index];
                Categories[index] = categories[index];
            }
        }

        public uint GetTotalOfPoints()
        {
            return _pointTotal;
        }

        public float3[] GetUnityPosistions()
        {
            return _unityPositions;
        }

        public float3[] GetColors()
        {
            return _colors;
        }
        public uint[] GetCategories()
        {
            return Categories;
        }

        // Calculate the barycenter of the pointcloud
        public float3 BoxCenter()
        {
            float3 center = new float3(0.0f, 0.0f, 0.0f);
            foreach (float3 coordinates in _unityPositions)
                center += coordinates * (float)(1.0f / _pointTotal);

            return center;
        }
        public Bounds BoundingBox(float scaleFactor)
        {
            float negInf = System.Single.NegativeInfinity, posInf = System.Single.PositiveInfinity;
            float minx = posInf, miny = posInf, minz = posInf, maxx = negInf, maxy = negInf, maxz = negInf;
            foreach (float3 coordinates in _unityPositions)
            {
                // Find limits for the bounding boxes  
                if (coordinates.x < minx)
                    minx = coordinates.x;
                if (coordinates.y < miny)
                    miny = coordinates.y;
                if (coordinates.z < minz)
                    minz = coordinates.z;
                if (coordinates.x > maxx)
                    maxx = coordinates.x;
                if (coordinates.y > maxy)
                    maxy = coordinates.y;
                if (coordinates.z > maxz)
                    maxz = coordinates.z;
            }

            // Bounding box enable the visualisation of the point cloud.
            // Thus, the extents are magnified by an arbitrary factor of 20.
            Vector3 extents = scaleFactor * new Vector3(maxx - minx, maxy - miny, maxz - minz);
            return new Bounds(BoxCenter(), extents);
        }

    }
}
