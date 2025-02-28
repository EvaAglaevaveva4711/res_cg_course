using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Assimp;
using HelixToolkit.Wpf;
using Vector3D = System.Windows.Media.Media3D.Vector3D;


namespace WpfApplication1
{
    public class SunflowerField : IObserver
    {
        private double _width;
        private double _height;
        private int _countSunflowers;
        public Model3DGroup _modelGroup;
        private Random _random;
        private List<Point3D> _fieldPositions;
        private string _fieldDataPath;
        private string _triangleIndicesPath;
        private string _sunflowerModelPath;
        private string _sunflowerTexturePath; 
        public List<GeometryModel3D> _sunflowerModels;
        private WindAnimator _windAnimator;
        private List<BoneWeight> _boneWeights;
        public List<Bone> _bones { get; private set; }
        
        public List<Model3D> _shadowModels = new List<Model3D>();
        
        private Dictionary<int, List<(Bone Bone, double Weight)>> _vertexToBonesMap;
        
        public event Action ShadowsUpdated;
        
        public SunflowerField(double width, double height, string pointsFilePath, int countSunflowers, string sunflowerModelPath, string sunflowerTexturePath)
        {
            _width = width;
            _height = height;
            _countSunflowers = countSunflowers;
            _modelGroup = new Model3DGroup();
            _random = new Random();
            _sunflowerModelPath = sunflowerModelPath;
            _sunflowerTexturePath = sunflowerTexturePath;
            _sunflowerModels = new List<GeometryModel3D>();
            _boneWeights = new List<BoneWeight>();
            
            string currentDirectory = Environment.CurrentDirectory;
            _fieldDataPath = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\Models\Field\field_data.txt"));
            _triangleIndicesPath = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\Models\Field\field_triangle_indices.txt"));
            
            LoadSunflowerPositions(pointsFilePath);
            CreateField();
            AddSunflowers(); 
        }
        
        public void Update(Sun sun)
        {
            sun.UpdateShadows(_shadowModels);
            OnShadowsUpdated();
        }
        
        private void LoadSunflowerPositions(string filePath)
        {
            _fieldPositions = new List<Point3D>();
            foreach (var line in File.ReadLines(filePath))
            {
                var coordinates = line.Split(',');
                if (coordinates.Length == 3 &&
                    double.TryParse(coordinates[0], out double x) &&
                    double.TryParse(coordinates[1], out double y) &&
                    double.TryParse(coordinates[2], out double z))
                {
                    _fieldPositions.Add(new Point3D(x, y, z));
                }
            }
        }

        private void CreateField()
        {
            var plane = new GeometryModel3D
            {
                Geometry = CreatePlaneGeometry(),
                Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 100, 255, 100))),
                BackMaterial = new SpecularMaterial(new SolidColorBrush(Colors.White), 50)
            };

            _modelGroup.Children.Add(plane);
        }
        
        private List<Point3D> ReadFieldDataFromFile(string filePath)
        {
            var points = new List<Point3D>();

            try
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var coordinates = line.Split(',');
                    if (coordinates.Length == 3)
                    {
                        double x = double.Parse(coordinates[0]);
                        double y = double.Parse(coordinates[1]);
                        double z = double.Parse(coordinates[2]);
                        
                        double normalizedX = x / 10.0; // Предполагаем, что исходные координаты в файле находятся в диапазоне [0, 10]
                        double normalizedZ = z / 10.0;
                        
                        double scaledX = normalizedX * _width;
                        double scaledZ = normalizedZ * _height;

                        points.Add(new Point3D(scaledX, y, scaledZ));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
            }

            return points;
        }
        
        private Int32Collection ReadTriangleIndicesFromFile(string filePath)
        {
            var indices = new List<int>();

            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    foreach (var part in parts)
                    {
                        if (int.TryParse(part, out int index))
                        {
                            indices.Add(index);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
            }

            return new Int32Collection(indices);
        }

        private MeshGeometry3D CreatePlaneGeometry()
        {
            var mesh = new MeshGeometry3D();
            var points = ReadFieldDataFromFile(_fieldDataPath);
            mesh.Positions = new Point3DCollection(points);
            mesh.TriangleIndices = ReadTriangleIndicesFromFile(_triangleIndicesPath);

            return mesh;
        }

        public void AddSunflowers()
        {
            var plane = _modelGroup.Children.OfType<GeometryModel3D>().FirstOrDefault();
            _modelGroup.Children.Clear();
            if (plane != null)
            {
                _modelGroup.Children.Add(plane);
            }
            _shadowModels.Clear(); // Очищаем список теней
            _sunflowerModels.Clear();
            
            for (int i = 0; i < _countSunflowers; i++)
            {
                double x = _random.NextDouble() * _width;
                double z = _random.NextDouble() * _height;

                var sunflowerModelGroup = LoadSunflowerModel(x, z);
                _modelGroup.Children.Add(sunflowerModelGroup);
                
                var shadowModel = CreateSunflowerShadow(sunflowerModelGroup, x, z);
                _modelGroup.Children.Add(shadowModel);
                _shadowModels.Add(shadowModel);
                
                foreach (var child in sunflowerModelGroup.Children)
                {
                    if (child is GeometryModel3D geometryModel)
                    {
                        _sunflowerModels.Add(geometryModel);
                    }
                }
            }
            _windAnimator = new WindAnimator(_sunflowerModels, _bones, _vertexToBonesMap);
            
            OnShadowsUpdated();
        }
        
        private Model3D CreateSunflowerShadow(Model3DGroup sunflowerModelGroup, double x, double z)
        {
            var shadowGroup = new Model3DGroup();

            foreach (var child in sunflowerModelGroup.Children)
            {
                if (child is GeometryModel3D geometryModel)
                {
                    var flattenedGeometry = FlattenGeometry(geometryModel.Geometry as MeshGeometry3D);

                    var shadowMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)));

                    var shadowModel = new GeometryModel3D(flattenedGeometry, shadowMaterial);

                    shadowGroup.Children.Add(shadowModel);
                }
            }

            shadowGroup.Transform = new TranslateTransform3D(x, 0.01, z);

            return shadowGroup;
        }
        
        private MeshGeometry3D FlattenGeometry(MeshGeometry3D originalGeometry)
        {
            var flattenedGeometry = new MeshGeometry3D
            {
                Positions = new Point3DCollection(),
                TriangleIndices = originalGeometry.TriangleIndices,
                TextureCoordinates = originalGeometry.TextureCoordinates
            };

            foreach (var point in originalGeometry.Positions)
            {
                flattenedGeometry.Positions.Add(new Point3D(point.X, 0.01, point.Z)); 
            }

            return flattenedGeometry;
        }
        
        protected virtual void OnShadowsUpdated()
        {
            ShadowsUpdated?.Invoke();
        }


        private Model3DGroup LoadSunflowerModel(double x, double z)
        {
            var importer = new AssimpContext();
            var scene = importer.ImportFile(_sunflowerModelPath, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals);
            var matcapTexture = new ImageBrush(new BitmapImage(new Uri(_sunflowerTexturePath)));

            var sunflowerModelGroup = new Model3DGroup();

            Random random = new Random();
            double minScale = 0.7;
            double maxScale = 1.5;
            double randomScale = random.NextDouble() * (maxScale - minScale) + minScale;

            _boneWeights.Clear();

            foreach (var mesh in scene.Meshes)
            {
                var geometry = MeshGeometryCreator.CreateMeshGeometry(mesh);

                Brush materialBrush = matcapTexture;
                var diffuseMaterial = new DiffuseMaterial(materialBrush);
                var specularMaterial = new SpecularMaterial
                {
                    Brush = new SolidColorBrush(Colors.White),
                    SpecularPower = 100 
                };

                var materialGroup = new MaterialGroup();
                materialGroup.Children.Add(diffuseMaterial);
                materialGroup.Children.Add(specularMaterial);

                var geometryModel = new GeometryModel3D(geometry, materialGroup) { BackMaterial = materialGroup };
                sunflowerModelGroup.Children.Add(geometryModel);

                if (mesh.Bones.Count > 0)
                {
                    foreach (var bone in mesh.Bones)
                    {
                        for (int i = 0; i < bone.VertexWeightCount; i++)
                        {
                            var vertexIndex = bone.VertexWeights[i].VertexID;
                            var weight = bone.VertexWeights[i].Weight;

                            _boneWeights.Add(new BoneWeight(vertexIndex, bone.Name, weight));
                        }
                    }
                }
                ModifyStemVertices(geometry, randomScale);
            }

            BoneSunflowerLoader(_boneWeights);

            sunflowerModelGroup.Transform = new TranslateTransform3D(x, 0, z);

            return sunflowerModelGroup;
        }

        private void ModifyStemVertices(MeshGeometry3D geometry, double scale_Y)
        {
            var stemBones = new List<string> { "root", "Sunflower01", "Sunflower02", "Sunflower03", "Sunflower04" };

            var newPositions = new Point3DCollection();
            
            for (int i = 0; i < geometry.Positions.Count; i++)
            {
                var vertex = geometry.Positions[i];
                bool isStemVertex = false;

                foreach (var boneWeight in _boneWeights)
                {
                    if (stemBones.Contains(boneWeight.BoneName))
                    {
                        isStemVertex = true;
                        break;
                    }
                }
                if (isStemVertex)
                {
                    Random random = new Random();
                    double minScale = scale_Y - 0.3;
                    double maxScale = scale_Y + 0.3;
                    double scale_X = random.NextDouble() * (maxScale - minScale) + minScale;

                    vertex.X *= scale_X;
                    vertex.Y *= scale_Y; 
                }
                
                newPositions.Add(vertex);
            }
            
            geometry.Positions = newPositions;
        }
        
        public void BoneSunflowerLoader(List<BoneWeight> boneWeights)
        {
            _bones = new List<Bone>();

            foreach (var boneWeight in boneWeights)
            {
                var bone = _bones.FirstOrDefault(b => b.Name == boneWeight.BoneName);

                if (bone == null)
                {
                    bone = new Bone(boneWeight.BoneName);
                    _bones.Add(bone);
                }

                bone.AddVertex(boneWeight.VertexID, boneWeight.Weight);
            }
            InitializeVertexToBonesMap();
        }
        
        private void InitializeVertexToBonesMap()
        {
            _vertexToBonesMap = new Dictionary<int, List<(Bone Bone, double Weight)>>();

            foreach (var bone in _bones)
            {
                foreach (var vertexWeight in bone.ConnectedVertices)
                {
                    if (!_vertexToBonesMap.ContainsKey(vertexWeight.VertexID))
                    {
                        _vertexToBonesMap[vertexWeight.VertexID] = new List<(Bone Bone, double Weight)>();
                    }
                    _vertexToBonesMap[vertexWeight.VertexID].Add((bone, vertexWeight.Weight));
                }
            }
        }
        
        public IEnumerable<Model3D> GetModels()
        {
            foreach (var child in _modelGroup.Children)
            {
                if (child is Model3D model3D)
                {
                    yield return model3D; 
                }
            }
        }
        
        public void StartWind(double amplitude, double frequency)
        {
            if (_windAnimator != null) 
                _windAnimator.Start(amplitude, frequency); 
        }

        public void StopWind()
        { 
            if (_windAnimator != null)
                _windAnimator.Stop(); 
        }
        
    }
}
