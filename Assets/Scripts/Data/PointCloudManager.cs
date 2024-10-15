using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.IO;
public enum MicrohabitatLabel {None, Cavity, Bark, Fungi, Epiphytic};
// Enumerable fields

namespace LabellingFlora.Data
{
    public class PointCloudManager : MonoBehaviour
    {
        // Serialize Fields
        [Header("Properties")]
        [SerializeField, Range(0.00001f, 0.2f)]
        private float _pointSize;
        [SerializeField, Range(0.5f, 1.5f)]
        private float _scale;
        [SerializeField]
        private float3 _axis;
        [SerializeField, Range(0f, 360f)]
        private float _theta;
        [Header("Aspect")]
        [SerializeField]
        private ComputeShader _computeShader;
        [SerializeField]
        private Material _material;
        [SerializeField]
        private Mesh _mesh;
        [Header("Location")]
        [Tooltip("relative or global path to the folder where is stored the point cloud")]
        [SerializeField]
        private string folderName;
        [Tooltip("name of the point cloud file with the extension included(.las, .laz or .txt are supported)")]
        [SerializeField]
        private string fileName;
        [Tooltip("anchor in the scene for the point cloud")]
        [SerializeField]
        private Transform _origin;

        //Private fields
        private System.Random rand = new System.Random();
        // TODOS: Create a dictionnary with the enum Microhabitats as a key
        private PointCloudWrapper _originalPointCloud;
        private PointCloudViewer _unityPointCloud;
        //private float3 _pointCloudAnchor; 
        //private float3 _worldAnchor;
        private float3[] unityPointCloudCoordinates;
        private float3[] workingLayer;
        private Bounds bounds;
        private ComputeBuffer _positionsBuffer, _colorsBuffer, _categoriesBuffer;

        // Readonly fields
        /// <summary>
        /// Variables which are used to communicate with shaders
        /// </summary>
        static readonly int
            positionsId = Shader.PropertyToID("_Positions"),
            colorsId = Shader.PropertyToID("_Colors"),
            categoriesId = Shader.PropertyToID("_Categories"),
            pointSizeId = Shader.PropertyToID("_PointSize"),
            numPointsId = Shader.PropertyToID("_NumPoints"),
            groupResolutionId = Shader.PropertyToID("_GroupResolution"),
            scaleId = Shader.PropertyToID("_Scale"),
            thetaId = Shader.PropertyToID("_Theta"),
            axisId = Shader.PropertyToID("_Axis"),
            originalBarycenterId = Shader.PropertyToID("_OriginalBarycenter"),
            unityBarycenterId = Shader.PropertyToID("_UnityBarycenter"),
            anchorId = Shader.PropertyToID("_Anchor"),
            labelId = Shader.PropertyToID("_Label"),
            cursorPositionId = Shader.PropertyToID("_CursorPosition"),
            cursorRadiusId = Shader.PropertyToID("_CursorRadius"),
            cursorColorId = Shader.PropertyToID("_CursorColor");


        // Public fields
        /// <summary>
        /// CurrentLabel tells which label is used for annotation
        /// </summary>
        public static MicrohabitatLabel CurrentLabel = MicrohabitatLabel.Cavity;
        /// <summary>
        /// CurrentLayer indicates which color is display on the pointcloud
        /// </summary>
        public static MicrohabitatLabel CurrentLayer = MicrohabitatLabel.None;

        /// <summary>
        /// Called everytime the PointCloudManager component is activated  
        /// </summary>
        void OnEnable()
        {
            // Intialize the PointCloudWrapper.

            string originalFileName = Path.Combine(folderName, fileName);
            Debug.Log(originalFileName);
            uint numberOfPointsOriginal = PointCloudWrapper.GetNumberOfPointFromFile(originalFileName);
            _originalPointCloud = new PointCloudWrapper(numberOfPointsOriginal);

            // Copy information from the file to the PointCloudWrapper object. 
            _originalPointCloud.ReadPointCloudFile(originalFileName);

            // Initialise Buffer to communicate with GPU.
            InitialiseGpuBuffer();

            // Transform Coordinates Systems to unity
            FromWorldToUnityPositions();

            // Intialise Unity Point Cloud for rendering
            _unityPointCloud = new PointCloudViewer(unityPointCloudCoordinates,
                             _originalPointCloud.GetColors(),
                             _originalPointCloud.GetCategories());

            bounds = new Bounds();
            bounds = _unityPointCloud.BoundingBox(_scale);
            // Initialise defautl mesh to Tetrahedron to lower the computation on the GPU.
            // source: https://github.com/mortennobel/ProceduralMesh/blob/master/TetrahedronUV.cs
            if (_mesh == null)
            {
                _mesh = new Mesh
                {
                    name = "Tetraheadron"
                };

                Vector3 p0 = new Vector3(0, 0, 0);
                Vector3 p1 = new Vector3(1, 0, 0);
                Vector3 p2 = new Vector3(0.5f, 0, Mathf.Sqrt(0.75f));
                Vector3 p3 = new Vector3(0.5f, Mathf.Sqrt(0.75f), Mathf.Sqrt(0.75f) / 3);

                _mesh.vertices = new Vector3[] {
                p0, p1, p2,
                p0, p2, p3,
                p2, p1, p3,
                p0, p3, p1
            };
                _mesh.triangles = new int[]{
                0,1,2,
                3,4,5,
                6,7,8,
                9,10,11
            };

                // UV is necessary to get rid of the vertical artefact (black line)
                Vector2 uv3a = new Vector2(0, 0);
                Vector2 uv1 = new Vector2(0.5f, 0);
                Vector2 uv0 = new Vector2(0.25f, Mathf.Sqrt(0.75f) / 2);
                Vector2 uv2 = new Vector2(0.75f, Mathf.Sqrt(0.75f) / 2);
                Vector2 uv3b = new Vector2(0.5f, Mathf.Sqrt(0.75f));
                Vector2 uv3c = new Vector2(1, 0);

                _mesh.uv = new Vector2[]{
                uv0,uv1,uv2,
                uv0,uv2,uv3b,
                uv0,uv1,uv3a,
                uv1,uv2,uv3c
            };

                _mesh.RecalculateNormals();
                _mesh.RecalculateBounds();
                _mesh.Optimize();
            }

        }

        /// <summary>
        /// Called everytime the PointCloudManager component is deactivated  
        /// </summary>
        void OnDisable()
        {
            _positionsBuffer.Release();
            _positionsBuffer = null;
            _colorsBuffer.Release();
            _colorsBuffer = null;
            _categoriesBuffer.Release();
            _categoriesBuffer = null;
        }

        /// <summary>
        /// Called every frame
        /// </summary>
        void Update()
        {
            // TODO: Link Save Function to UI 
            if (Input.GetKeyDown(KeyCode.Space))
            {
                string resultsLocalPath = folderName + "\\" + fileName;
                _originalPointCloud.ExportPointCloud(resultsLocalPath, _unityPointCloud.Categories);
            }
            DrawPointCloudOnGpu();
        }

        /// <summary>
        /// Debugging Shader 
        /// </summary>
        //float RandomFloat(float min, float max)
        //{
        //    return (float)rand.NextDouble() * (max - min) + min;
        //}

        /// <summary>
        /// Initialisation of Tensor size to store Point Cloud on the GPU 
        /// </summary>
        void InitialiseGpuBuffer()
        {
            // 3 times 4 bytes (= float or uint) 
            _positionsBuffer = new ComputeBuffer((int)_originalPointCloud.GetNumberOfPoints(), 3 * 4);
            _positionsBuffer.SetData(_originalPointCloud.GetPositions());

            _colorsBuffer = new ComputeBuffer((int)_originalPointCloud.GetNumberOfPoints(), 3 * 4);
            _colorsBuffer.SetData(_originalPointCloud.GetColors());

            _categoriesBuffer = new ComputeBuffer((int)_originalPointCloud.GetNumberOfPoints(), 1 * 4);
            _categoriesBuffer.SetData(_originalPointCloud.GetCategories());
        }

        /// <summary>
        /// Convert Point cloud 3D position from World to Unity Coordinate System 
        /// </summary>
        void FromWorldToUnityPositions()
        {
            Vector4 axis = new Vector4(_axis.x, _axis.y, _axis.z);
            var kernelIndex = _computeShader.FindKernel("FromWorldToUnityPositionsGpu");
            int NumberOfThreadGroup = Mathf.CeilToInt(Mathf.Sqrt(_originalPointCloud.GetNumberOfPoints() / 512f));
            _computeShader.SetVector(originalBarycenterId, (Vector3)_originalPointCloud.Barycenter);
            _computeShader.SetVector(unityBarycenterId, _origin.position);

            _computeShader.SetBuffer(kernelIndex, positionsId, _positionsBuffer);
            _computeShader.SetInt(numPointsId, (int)_originalPointCloud.GetNumberOfPoints());
            _computeShader.SetInt(groupResolutionId, NumberOfThreadGroup);
            _computeShader.SetFloat(pointSizeId, _pointSize);
            _computeShader.SetFloat(scaleId, _scale);
            _computeShader.SetFloat(thetaId, _theta * Mathf.Deg2Rad);
            _computeShader.SetVector(axisId, axis);
            _computeShader.SetInt(labelId, 1);
            _computeShader.SetVector(cursorPositionId, new Vector3(1.0f, 0.0f, 1.0f));
            _computeShader.SetFloat(cursorRadiusId, 0.1f);

            _computeShader.SetBuffer(kernelIndex, colorsId, _colorsBuffer);
            _computeShader.SetBuffer(kernelIndex, categoriesId, _categoriesBuffer);

            _computeShader.Dispatch(kernelIndex, NumberOfThreadGroup, NumberOfThreadGroup, 1);

            unityPointCloudCoordinates = new float3[_originalPointCloud.GetNumberOfPoints()];
            _positionsBuffer.GetData(unityPointCloudCoordinates);

            workingLayer = new float3[_originalPointCloud.GetNumberOfPoints()];
            _colorsBuffer.GetData(workingLayer);
            //float3[] tmpPositionsBuffer = new float3[8];
            //_positionsBuffer.GetData(tmpPositionsBuffer);
            //for (int index = 0; index < 8; index++)
            //{
            //    Debug.Log("TLS Pos: " + _originalPointCloud.GetPositions()[index].ToString());
            //    Debug.Log("Unity Pos: " + unityPointCloudCoordinates[index].ToString());
            //}

        }


        /// <summary>
        /// 
        /// </summary>
        void DrawPointCloudOnGpu()
        {
            // Create axis to visuale points
            Vector4 axis = new Vector4(_axis.x, _axis.y, _axis.z);
            _material.SetBuffer(positionsId, _positionsBuffer);
            _material.SetBuffer(colorsId, _colorsBuffer);
            _material.SetFloat(pointSizeId, _pointSize);
            _material.SetFloat(scaleId, _scale);
            _material.SetFloat(thetaId, _theta * Mathf.Deg2Rad);
            _material.SetVector(anchorId, _origin.position);
            _material.SetVector(axisId, axis);

            // Unity function to draw the point cloud stored in the GPU 
            Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, bounds, (int)_unityPointCloud.GetTotalOfPoints());
        }

        /// <summary>
        /// Find point contain in the Spherical Cursor and label them accordingly 
        /// </summary>
        public void Labelling(Vector3 cursorPosition, float cursorRadius, Vector3 cursorColor)
        {
            uint dimx, dimy, dimz;
            Vector4 axis = new Vector4(_axis.x, _axis.y, _axis.z);
            // Update the bounds if scale action perform 
            var kernelIndex = _computeShader.FindKernel("LabellingOnGpu");
            _computeShader.GetKernelThreadGroupSizes(kernelIndex, out dimx, out dimy, out dimz);
            int NumberOfThreadGroup = Mathf.CeilToInt(Mathf.Sqrt(_unityPointCloud.GetTotalOfPoints() / 512f));
            _computeShader.SetInt(numPointsId, (int)_unityPointCloud.GetTotalOfPoints());
            _computeShader.SetInt(groupResolutionId, NumberOfThreadGroup);
            _computeShader.SetFloat(pointSizeId, _pointSize);
            _computeShader.SetFloat(scaleId, _scale);
            _computeShader.SetFloat(thetaId, _theta * Mathf.Deg2Rad);
            _computeShader.SetVector(axisId, axis);
            _computeShader.SetInt(labelId, (int)CurrentLabel);
            //_computeShader.SetInt(labelId, (int) CurrentLabel+1);
            _computeShader.SetVector(cursorPositionId, cursorPosition);
            _computeShader.SetFloat(cursorRadiusId, cursorRadius);
            _computeShader.SetVector(cursorColorId, cursorColor);
            _computeShader.SetBuffer(kernelIndex, positionsId, _positionsBuffer);
            _computeShader.SetBuffer(kernelIndex, colorsId, _colorsBuffer);
            _computeShader.SetBuffer(kernelIndex, categoriesId, _categoriesBuffer);
            // DEBUG
            //Run  the .compute file
            //Debug.Log("InstanceID: " + _computeShader.GetInstanceID().ToString());
            //Debug.Log("NumberOfThreadGroup: " + NumberOfThreadGroup.ToString());
            //Debug.Log("ThreadGroupSizes: " + dimx.ToString() + "," + dimy.ToString() + "," + dimz.ToString());

            _computeShader.Dispatch(kernelIndex, NumberOfThreadGroup, NumberOfThreadGroup, 1);
            // DEBUG
            //uint[] tmpCategoriesBuffer = new uint[8];
            //_categoriesBuffer.GetData(tmpCategoriesBuffer);
            //for (int index = 0; index < 8; index++)
            //{
            //    Debug.Log("Label: " + tmpCategoriesBuffer[index].ToString());
            //}
        }

        /// <summary>
        /// Change the point cloud color information to inspect state of labelling 
        /// </summary>
        public void ChangeLayer()
        {
            CurrentLayer++;
            if ((int)CurrentLayer >= 5)
            {
                CurrentLayer = MicrohabitatLabel.None;
                _colorsBuffer.SetData(_originalPointCloud.GetColors());
            }
            else
            {
                float3[] colorLayer = new float3[_originalPointCloud.GetNumberOfPoints()];
                uint counter = 0;
                _categoriesBuffer.GetData(_unityPointCloud.Categories);
                foreach (uint label in _unityPointCloud.Categories)
                {
                    if (label == (uint)CurrentLayer)
                        colorLayer[counter] = _unityPointCloud.GetColors()[counter];
                    else
                        colorLayer[counter] = new float3(Color.grey.r, Color.grey.g, Color.grey.b);

                    counter++;
                }

                // Update Color from point cloud with layer
                _colorsBuffer.SetData(colorLayer);
            }
        }

        public void SwitchLayer()
        {
            // Save workinglayer
            if (CurrentLayer == 0)
                _colorsBuffer.GetData(workingLayer);

            // Incremetn Layer
            CurrentLayer++;

            // Based on the current layer display appropriate mask 
            float3[] colorLayer = new float3[_originalPointCloud.GetNumberOfPoints()];

            uint counter = 0;

            if ((int)CurrentLayer >= 5)
            {
                CurrentLayer = MicrohabitatLabel.None;
                _categoriesBuffer.GetData(_unityPointCloud.Categories);
                foreach (uint label in _unityPointCloud.Categories)
                {
                    if (label > 0)
                        colorLayer[counter] = workingLayer[counter];
                    else
                        colorLayer[counter] = _unityPointCloud.GetColors()[counter];


                    counter++;
                }

            }
            else
            {
                _categoriesBuffer.GetData(_unityPointCloud.Categories);
                foreach (uint label in _unityPointCloud.Categories)
                {
                    if (label == (uint)CurrentLayer)
                        colorLayer[counter] = _unityPointCloud.GetColors()[counter];
                    else
                        colorLayer[counter] = new float3(Color.grey.r, Color.grey.g, Color.grey.b);

                    counter++;
                }
            }
            // Update Color from point cloud with layer
            _colorsBuffer.SetData(colorLayer);

        }

        /// <summary>
        /// Increase the size of the point (tetrahedron) inside the point cloud
        /// </summary>
        public void ScaleUpPointCloud(float delta)
        {
            if (_scale < 1.5f)
                _scale += delta;
        }

        /// <summary>
        /// Decrease the size of the point (tetrahedron) inside the point cloud
        /// </summary>
        public void ScaleDownPointCloud(float delta)
        {
            if (_scale > 0.5)
                _scale -= delta;
        }

        /// <summary>
        /// Rotate the point cloud clockwise
        /// </summary>
        public void RotateClockwisePointCloud(float degree)
        {
            if (_theta + degree > 360.0f)
                _theta -= 360.0f;

            _theta += degree;
        }

        /// <summary>
        /// Rotate the point cloud counterclockwise
        /// </summary>
        /// <param name="degree"></param>
        public void RotateCounterClockwisePointCloud(float degree)
        {
            if (_theta - degree < 0)
                _theta += 360.0f;

            _theta -= degree;
        }

    }
}