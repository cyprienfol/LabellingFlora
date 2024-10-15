using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Mathematics;
using LASzip.Net;

namespace LabellingFlora.Data
{
    public class PointCloudWrapper
    {
        // Private fields
        private float3[] _positions;
        private float3[] _colors;
        private uint[] _categories;
        private uint _numberOfPoints = 30000000; //limit that can be changed   
        public float3 Barycenter;

        private float min_x;
        private float min_y;
        private float min_z;
        private float max_x;
        private float max_y;
        private float max_z;

        // By defautl the parameter are created with the maximum numberOfPoints.
        public PointCloudWrapper()
        {
            _positions = new float3[_numberOfPoints];
            _colors = new float3[_numberOfPoints];
            _categories = new uint[_numberOfPoints];
        }

        // Contructor: only smaller number of points than numberOfPoint.
        public PointCloudWrapper(uint numberOfPoints)
        {
            if (numberOfPoints < _numberOfPoints)
                _numberOfPoints = numberOfPoints;
            else
                Debug.LogWarning("Number of points enter is higher than " + System.Convert.ToString(this._numberOfPoints));

            _positions = new float3[_numberOfPoints];
            _colors = new float3[_numberOfPoints];
            _categories = new uint[_numberOfPoints];
        }

        public PointCloudWrapper(float3[] position, float3[] color, uint numberOfPoints)
        {
            if (numberOfPoints < _numberOfPoints)
                _numberOfPoints = numberOfPoints;
            else
                Debug.LogWarning("Number of points enter is higher than " + System.Convert.ToString(this._numberOfPoints));

            _positions = new float3[_numberOfPoints];
            _colors = new float3[_numberOfPoints];
            _categories = new uint[_numberOfPoints];

            for (int pointIndex = 0; pointIndex < _numberOfPoints; pointIndex++)
            {
                _positions[pointIndex] = position[pointIndex];
                _colors[pointIndex] = color[pointIndex];
            }

        }
        public PointCloudWrapper(float3[] position, float3[] color, uint[] category, uint numberOfPoints)
        {
            if (numberOfPoints < _numberOfPoints)
                _numberOfPoints = numberOfPoints;
            else
                Debug.LogWarning("Number of points enter is higher than " + System.Convert.ToString(this._numberOfPoints));
            _positions = new float3[_numberOfPoints];
            _colors = new float3[_numberOfPoints];
            _categories = new uint[_numberOfPoints];

            for (int pointIndex = 0; pointIndex < _numberOfPoints; pointIndex++)
            {
                _positions[pointIndex] = position[pointIndex];
                _colors[pointIndex] = color[pointIndex];
                _categories[pointIndex] = category[pointIndex];
            }

        }
        public float3[] GetPositions()
        {
            return _positions;
        }
        public float3[] GetColors()
        {
            return _colors;
        }

        public uint[] GetCategories()
        {
            return _categories;
        }

        public uint GetNumberOfPoints()
        {
            return _numberOfPoints;
        }

        // Calculate the barycenter of the pointcloud
        public float3 CalculateBarycenter()
        {
            float3 barycenter = new float3(0.0f, 0.0f, 0.0f);
            foreach (float3 coordinates in _positions)
                barycenter += coordinates * (float)(1.0f / _numberOfPoints);

            return barycenter;
        }
        public float3[] ConvertPositionToUnityFrame(Vector3 offset)
        {
            float3[] convertedPosition = new float3[_numberOfPoints];
            uint index = 0;
            foreach (var coordinates in _positions)
            {
                convertedPosition[index] = (coordinates - Barycenter) + (float3)offset;
                index++;
            }

            return convertedPosition;
        }

        // Return the number of points contained in the file located at local path.
        public static uint GetNumberOfPointFromFile(string localPath)
        {
            uint TotalOfPoints = 0;
            string fileExtenstion = Path.GetExtension(localPath);
            if (fileExtenstion == ".txt")
            {
                uint HeaderNumberOfPoints = 0, counterOfPoints = 0;
                foreach (string line in System.IO.File.ReadLines(localPath))
                {
                    if (!line.StartsWith("//"))
                    {
                        var data = line.Split(' ');
                        if (data.Length == 1)
                        {
                            HeaderNumberOfPoints = uint.Parse(data[0]);
                            Debug.Log(System.String.Format("Total points: {0}", HeaderNumberOfPoints));
                            break;
                        }
                        // Lines corresponding to points metadata
                        else
                            counterOfPoints++;
                    }
                }
                if (counterOfPoints == 0)
                    TotalOfPoints = HeaderNumberOfPoints;
                else
                    TotalOfPoints = counterOfPoints;
            }
            else if (fileExtenstion == ".las" || fileExtenstion == ".laz")
            {
                var lazReader = new laszip();
                var compressed = true;
                lazReader.open_reader(localPath, out compressed);
                TotalOfPoints = lazReader.header.number_of_point_records;
            }
            else
            {
                Debug.LogError("The file extension is either undefined or not supported by the TreeD lab");
            }
            return TotalOfPoints;
        }

        public void ReadTxtFile(string path)
        {
            uint counterLine = 0;
            foreach (string line in System.IO.File.ReadLines(path))
            {
                if (!line.StartsWith("//"))
                {
                    var data = line.Split(' ');
                    if (data.Length >= 6 && counterLine < _numberOfPoints)
                    {
                        // Get the position of the point cloud.
                        _positions[counterLine] = new float3(System.Single.Parse(data[0]),
                            System.Single.Parse(data[1]),
                            System.Single.Parse(data[2]));

                        // Get Barycenter position                    
                        Barycenter += _positions[counterLine] * (float)(1.0f / _numberOfPoints);


                        // Get Color Information. 
                        _colors[counterLine] = new float3(System.Single.Parse(data[3]),
                            System.Single.Parse(data[4]),
                            System.Single.Parse(data[5]));

                        // Get category of points in the clouds. 
                        _categories[counterLine] = (uint)System.Single.Parse(data[6]);
                        counterLine++;
                    }
                }
            }
        }
        public void ReadLasFile(string path)
        {
            var lazReader = new laszip();
            var compressed = true;
            lazReader.open_reader(path, out compressed);
            Debug.Log(compressed);

            uint classification = 0;
            var coordArray = new double[3];
            System.UInt16[] rgb;

            // Loop through number of points indicated
            for (int pointIndex = 0; pointIndex < _numberOfPoints; pointIndex++)
            {
                // Get Headers
                min_x = (float)lazReader.header.min_x;
                min_y = (float)lazReader.header.min_y;
                min_z = (float)lazReader.header.min_z;
                max_x = (float)lazReader.header.max_x;
                max_y = (float)lazReader.header.max_y;
                max_z = (float)lazReader.header.max_z;

                // Read the point
                lazReader.read_point();

                // Get precision coordinates
                lazReader.get_coordinates(coordArray);

                // Get classification value
                rgb = lazReader.point.rgb;
                classification = lazReader.point.classification;

                _positions[pointIndex] = new float3((float)coordArray[0], (float)coordArray[1], (float)coordArray[2]);
                // Get Barycenter position                    
                Barycenter += _positions[pointIndex] * (float)(1.0f / _numberOfPoints);

                _colors[pointIndex] = new float3(rgb[0] / 65535f, rgb[1] / 65535f, rgb[2] / 65535f);
                _categories[pointIndex] = classification;
            }

            // Close the reader
            lazReader.close_reader();

        }

        // TODO: Implement logic for other point cloud file format (.ply)
        public void ReadPointCloudFile(string localPath)
        {
            string fileExtension = System.IO.Path.GetExtension(localPath);

            if (fileExtension == ".txt")
                ReadTxtFile(localPath);

            else if (fileExtension == ".las" || fileExtension == ".laz")
                ReadLasFile(localPath);

            else
                Debug.LogError("The file extension is either undefined or not supported by the TreeD lab");

        }
        public void ExportPointCloud(string originalLocalpath, uint[] newLabels)
        {
            string originalFolderPath = Path.GetDirectoryName(originalLocalpath);
            string originalFileName = Path.GetFileName(originalLocalpath);
            var lasReader = new laszip();
            var compressed = true;
            lasReader.open_reader(originalLocalpath, out compressed);

            string resultsFolderName = "Results";
            string resultsFolderPath = Path.Combine(originalFolderPath, resultsFolderName);
            string resultsFileLocalPath = Path.Combine(resultsFolderPath, originalFileName + "_labelled_" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".las");
            //Create a new folder
            if (!System.IO.Directory.Exists(resultsFolderPath))
            {
                System.IO.Directory.CreateDirectory(resultsFolderPath);
            }

            //Use Laslib to write the texte file
            var lasWriter = new laszip();
            var err = lasWriter.clean();

            // Number of point records needs to be set
            lasWriter.set_header(lasReader.header);

            lasWriter.open_writer(resultsFileLocalPath, compressed);

            var lasCoordinates = new double[3];

            //// Loop through number of points indicated
            for (int pointIndex = 0; pointIndex < _numberOfPoints; pointIndex++)
            {
                // Set positions 
                lasCoordinates[0] = (double)_positions[pointIndex].x;
                lasCoordinates[1] = (double)_positions[pointIndex].y;
                lasCoordinates[2] = (double)_positions[pointIndex].z;
                lasWriter.set_coordinates(lasCoordinates);

                // Set colors
                lasWriter.point.rgb[0] = (System.UInt16)(_colors[pointIndex].x * 65535f);
                lasWriter.point.rgb[1] = (System.UInt16)(_colors[pointIndex].y * 65535f);
                lasWriter.point.rgb[2] = (System.UInt16)(_colors[pointIndex].z * 65535f);
                lasWriter.point.rgb[3] = (System.UInt16)(0.5 * 65535f);

                // Set categories
                lasWriter.point.classification = (byte)newLabels[pointIndex];

                //Write the point
                lasWriter.write_point();

            }

            // Close the writer to release the file (OS lock)
            lasWriter.close_writer();
            lasWriter = null;
        }
    }
}
