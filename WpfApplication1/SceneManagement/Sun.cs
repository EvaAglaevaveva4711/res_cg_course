using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;

namespace WpfApplication1
{
    public class Sun
    {
        private List<IObserver> _observers = new List<IObserver>();
        private DirectionalLight _sunLight;
        public SphereVisual3D Visual { get; private set; }
        private DispatcherTimer _sunTimer;
        
        private double _sunAngle = 0.0; // Угол для источника света
        private double _sunSpeed = 5.25; // Скорость вращения
        
        private List<Model3D> _shadowModels = new List<Model3D>();

        public Sun()
        {
            _sunLight = new DirectionalLight
            {
                Color = Colors.Yellow,
                Direction = new Vector3D(0, -1, 0) 
            };
        
            Visual = new SphereVisual3D
            {
                Center = new Point3D(0, 0, 0),
                Radius = 0.01, 
                Fill = Brushes.Yellow
            };
            
            StartSun(1000, 1000);
        }

        public DirectionalLight Light => _sunLight;
        
        public void UpdateSunPosition(double width, double height, List<Model3D> shadowModels)
        {
            _sunAngle += _sunSpeed; // Увеличиваем угол
            if (_sunAngle >= 360) _sunAngle -= 360; 

            double centerX = width / 2; // Центр по оси X
            double centerZ = height / 2; // Центр по оси Z

            double x = Math.Cos(_sunAngle * Math.PI / 180);
            double z = Math.Sin(_sunAngle * Math.PI / 180); 

            
            double intensity = 1.0; 
            double difference = 90;

            if (_sunAngle >= 180 && _sunAngle < 180 + difference)
            {
                intensity = 1.0 - ((_sunAngle - 180) / difference);
            }
            else if (_sunAngle >= 360 - difference && _sunAngle < 360)
            {
                intensity = (_sunAngle - (360 - difference)) / difference; 
            }
            
            Vector3D direction = new Vector3D(0, -z * intensity, x * intensity);
            direction.Normalize(); // Нормализуем вектор
            _sunLight.Direction = direction;
            Visual.Center = new Point3D(centerX + x, z, centerZ); 
            
            UpdateShadows(_shadowModels);
            UpdateLightColor();

            NotifyObservers();
        }
        
        public void UpdateShadows(List<Model3D> shadowModels)
        {
            _shadowModels = shadowModels;

            Vector3D lightDirection = _sunLight.Direction;
            foreach (var shadowModel in _shadowModels)
            {
                if (shadowModel is GeometryModel3D geometryModel)
                {
                    double stretchFactor = Math.Abs(lightDirection.Z);
                    var transform = new ScaleTransform3D(1, 1, 1 + stretchFactor * 20);
                    geometryModel.Transform = transform;
                }
            }
        }
        
        private void UpdateLightColor()
        {
            if (_sunAngle >= 0 && _sunAngle < 90) // Утро
            {
                _sunLight.Color = Colors.LightYellow;
            }
            else if (_sunAngle >= 90 && _sunAngle < 180) // День
            {
                _sunLight.Color = Colors.White;
            }
            else if (_sunAngle >= 180 && _sunAngle < 270) // Вечер
            {
                _sunLight.Color = Colors.Orange;
            }
            else // Ночь
            {
                _sunLight.Color = Colors.DarkSlateBlue;
            }
        }

        
        public void StartSun(double width, double height)
        {
            if (_sunTimer == null)
            {
                _sunTimer = new DispatcherTimer();
                _sunTimer.Interval = TimeSpan.FromMilliseconds(100); // Интервал обновления
                _sunTimer.Tick += (s, e) => UpdateSunPosition(width, height, _shadowModels); // Предполагается, что UpdateSunPosition() обновляет положение солнца
                _sunTimer.Start();
            }
        }

        public void SetTimeOfDay(string timeOfDay)
        {
            switch (timeOfDay)
            {
                case "Утро":
                    _sunAngle = 45; // Угол для утра
                    break;
                case "День":
                    _sunAngle = 90; // Угол для дня
                    break;
                case "Вечер":
                    _sunAngle = 225; // Угол для вечера
                    break;
                case "Ночь":
                    _sunAngle = 270; // Угол для ночи
                    break;
                default:
                    _sunAngle = 90; // По умолчанию день
                    break;
            }

            UpdateSunPosition(1000, 1000, _shadowModels);
        }

        public void SetSunSpeed(double speed)
        {
            _sunSpeed = speed;
        }
        
        public double Angle
        {
            get => _sunAngle;
            set
            {
                _sunAngle = value;
                NotifyObservers();
            }
        }

        public void Attach(IObserver observer)
        {
            _observers.Add(observer);
        }

        public void Detach(IObserver observer)
        {
            _observers.Remove(observer);
        }
        
        private void NotifyObservers()
        {
            foreach (var observer in _observers)
            {
                observer.Update(this);
            }
        }
    }
}
