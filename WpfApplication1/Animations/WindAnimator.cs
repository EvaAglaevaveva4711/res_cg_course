using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace WpfApplication1
{
    public class WindAnimator
    {
        private bool _isAnimating;
        private readonly DispatcherTimer _timer;
        private readonly List<GeometryModel3D> _sunflowerModels;
        private List<Bone> _bones;
        private double _amplitude;
        private double _frequency;
        private double _timeElapsed;
        private readonly Dictionary<GeometryModel3D, Point3DCollection> _originalPositions;
        private readonly Dictionary<int, List<(Bone Bone, double Weight)>> _vertexToBonesMap;

        public WindAnimator(List<GeometryModel3D> sunflowerModels, List<Bone> bones, Dictionary<int, List<(Bone Bone, double Weight)>> vertexToBonesMap)
        {
            _sunflowerModels = sunflowerModels ?? throw new ArgumentNullException(nameof(sunflowerModels));
            _bones = bones ?? throw new ArgumentNullException(nameof(bones));
            _vertexToBonesMap = vertexToBonesMap ?? throw new ArgumentNullException(nameof(vertexToBonesMap));

            _timeElapsed = 0;
            _originalPositions = new Dictionary<GeometryModel3D, Point3DCollection>();

            // Сохраняем оригинальные позиции вершин для каждой модели
            foreach (var model in _sunflowerModels)
            {
                if (model.Geometry is MeshGeometry3D meshGeometry)
                {
                    _originalPositions[model] = new Point3DCollection(meshGeometry.Positions);
                }
            }

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // около 60 FPS
            };
            _timer.Tick += OnTimerTick;
        }
        
        
        private void OnTimerTick(object sender, EventArgs e)
        {
            _timeElapsed += _timer.Interval.TotalSeconds;

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var geometryModel in _sunflowerModels)
                {
                    if (geometryModel.Geometry is MeshGeometry3D meshGeometry)
                    {
                        UpdateVerticesBasedOnBones(meshGeometry, _originalPositions[geometryModel]);
                    }
                }
            });
        }
        
        
        private void UpdateVerticesBasedOnBones(MeshGeometry3D meshGeometry, Point3DCollection originalPositions)
        {
            double windStrength = _amplitude * Math.Sin(_timeElapsed * _frequency * 2 * Math.PI);

            var boneTransforms = new Dictionary<Bone, Matrix3D>();

            ApplyBoneTransform(_bones.FirstOrDefault(b => b.Name == "root"), null, windStrength, boneTransforms);
            ApplyBoneTransform(_bones.FirstOrDefault(b => b.Name == "Sunflower01"), _bones.FirstOrDefault(b => b.Name == "root"), windStrength, boneTransforms);
            ApplyBoneTransform(_bones.FirstOrDefault(b => b.Name == "Sunflower02"), _bones.FirstOrDefault(b => b.Name == "Sunflower01"), windStrength, boneTransforms);
            ApplyBoneTransform(_bones.FirstOrDefault(b => b.Name == "Sunflower03"), _bones.FirstOrDefault(b => b.Name == "Sunflower02"), windStrength, boneTransforms);
            ApplyBoneTransform(_bones.FirstOrDefault(b => b.Name == "Sunflower04"), _bones.FirstOrDefault(b => b.Name == "Sunflower03"), windStrength, boneTransforms);
            ApplyBoneTransform(_bones.FirstOrDefault(b => b.Name == "Sunflower05"), _bones.FirstOrDefault(b => b.Name == "Sunflower04"), windStrength, boneTransforms);
            ApplyBoneTransform(_bones.FirstOrDefault(b => b.Name == "Sunflower5"), _bones.FirstOrDefault(b => b.Name == "Sunflower05"), windStrength, boneTransforms);

            for (int i = 0; i < meshGeometry.Positions.Count; i++)
            {
                var vertex = originalPositions[i];
                var transformedVertex = new Vector3D(0, 0, 0);

                if (_vertexToBonesMap.TryGetValue(i, out var boneWeights))
                {
                    foreach (var (bone, weight) in boneWeights)
                    {
                        if (boneTransforms.ContainsKey(bone))
                        {
                            var transformed = boneTransforms[bone].Transform(new Vector3D(vertex.X, vertex.Y, vertex.Z));
                            transformedVertex += transformed * weight;
                        }
                    }
                }

                meshGeometry.Positions[i] = new Point3D(transformedVertex.X, transformedVertex.Y, transformedVertex.Z);
            }
        }

        // Метод для применения вращения к вершинам кости
        private void ApplyBoneTransform(Bone bone, Bone parentBone, double windStrength, Dictionary<Bone, Matrix3D> boneTransforms)
        {
            if (bone == null) return;

            double angle = windStrength * GetBoneMultiplier(bone.Name);
            var localRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), angle)).Value;

            if (parentBone != null && boneTransforms.ContainsKey(parentBone))
            {
                boneTransforms[bone] = localRotation * boneTransforms[parentBone];
            }
            else
            {
                boneTransforms[bone] = localRotation;
            }
        }

        // Метод для получения множителя угла в зависимости от кости
        private double GetBoneMultiplier(string boneName)
        {
            switch (boneName)
            {
                case "root": return 1.0;
                case "Sunflower01": return 2.0;
                case "Sunflower02": return 3.0;
                case "Sunflower03": return 4.0;
                case "Sunflower04": return 5.0;
                case "Sunflower05": return 6.0;
                case "Sunflower5": return 7.0;
                default: return 1.0;
            }
        }
        
        public void Start(double amplitude, double frequency)
        {
            _amplitude = amplitude;
            _frequency = frequency;
            
            if (!_isAnimating)
            {
                _isAnimating = true;
                _timeElapsed = 0;
                _timer.Start();
            }
        }

        public void Stop()
        {
            if (_isAnimating)
            {
                _isAnimating = false;
                _timer.Stop();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var geometryModel in _sunflowerModels)
                    {
                        if (geometryModel.Geometry is MeshGeometry3D meshGeometry)
                        {
                            meshGeometry.Positions = _originalPositions[geometryModel];
                        }
                    }
                });
            }
        }
    }
}