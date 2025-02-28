using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;


namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        private CameraController _cameraController;
        private SunflowerField _sunflowerField;
        private Sun _sun;

        private string _sunflowerModelPath;
        private string _sunflowerTexturePath;
        private string _fieldPath;
        

        public MainWindow()
        {
            InitializeComponent();
            
            WidthTextBox.Text = "100"; 
            HeightTextBox.Text = "100"; 
            CountSunflowersBox.Text = "10"; 
            
            AmplitudeWindTextBox.Text = "0,5";
            FrequencyWindTextBox.Text = "2";
            TimeOfDayComboBox.SelectedIndex = 1;
            TimeGoingBox.Text = "4";
            
            _cameraController = new CameraController(MainCamera);
            this.KeyDown += _cameraController.KeyDown;
            this.MouseDown += _cameraController.MouseDown;
            this.MouseUp += _cameraController.MouseUp;
            this.MouseMove += _cameraController.MouseMove;
            this.MouseWheel += _cameraController.MouseWheel;
            this.Loaded += (s, e) => this.Focus();
            
            string currentDirectory = Environment.CurrentDirectory;
            _sunflowerModelPath = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\Models\Sunflower_2\Sunflower_new.fbx"));
            _sunflowerTexturePath = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\Models\Sunflower_2\Sunflower_1001_Diffuse.png"));
            _fieldPath = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\Models\Field\Field.txt"));
        }
        
        //стенсельные тени
        

        private void InitializeSun()
        {
            _sun = new Sun();
            MainViewport.Children.Add(_sun.Visual);
            
            ModelVisual3D lightModel = new ModelVisual3D { Content = _sun.Light };
            MainViewport.Children.Add(lightModel);
            
            if (_sunflowerField != null)
            {
                _sun.Attach(_sunflowerField);
                
                foreach (var sunflower in _sunflowerField.GetModels())
                {
                    var modelVisual = new ModelVisual3D { Content = sunflower };
                    MainViewport.Children.Add(modelVisual);
                }
            }
            else
            {
                Console.WriteLine("sunflowerField = null");
            }
        }

        private void SetSceneSizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainViewport.Children.Count() > 0)
                {
                    MainViewport.Children.Clear();
                }
                
                InitializeSun();
                if (string.IsNullOrWhiteSpace(WidthTextBox.Text) || 
                    string.IsNullOrWhiteSpace(HeightTextBox.Text) || 
                    string.IsNullOrWhiteSpace(CountSunflowersBox.Text))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля.");
                    return;
                }
                
                double width = double.Parse(WidthTextBox.Text);
                double height = double.Parse(HeightTextBox.Text);
                int countSunflowers = int.Parse(CountSunflowersBox.Text);

                Console.WriteLine($"Количество элементов в сцене: {MainViewport.Children.Count}");
                _sunflowerField = new SunflowerField(width, height, _fieldPath, countSunflowers, _sunflowerModelPath, _sunflowerTexturePath);
                Console.WriteLine($"Количество элементов в сцене: {MainViewport.Children.Count}");
                
                MainViewport.Children.Clear();
                InitializeSun();
                
                foreach (var sunflower in _sunflowerField.GetModels())
                {
                    var modelVisual = new ModelVisual3D { Content = sunflower };
                    MainViewport.Children.Add(modelVisual);
                }
                
                _sun.Attach(_sunflowerField);
            }
            catch (FormatException)
            {
                MessageBox.Show("Пожалуйста, введите корректные числовые значения для ширины и высоты.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
        
        private void StartWindButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sunflowerField != null)
            {
                if (string.IsNullOrWhiteSpace(FrequencyWindTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля.");
                    return;
                }

                double amplitude = double.Parse(AmplitudeWindTextBox.Text);
                if (amplitude < 0 || amplitude > 1)
                {
                    MessageBox.Show("Пожалуйста, введите значение амплитуды от 0 до 1.");
                    return;
                }
                double frequency = double.Parse(FrequencyWindTextBox.Text);

                _sunflowerField.StartWind(amplitude, frequency);

                Console.WriteLine("The 'Start Wind' button is pressed");
            }
        }

        private void StopWindButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sunflowerField != null)
                _sunflowerField.StopWind();
        }
        
        
        private void ApplyTimeOfDayButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedTime = (TimeOfDayComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (_sun != null)
            {
                _sun.SetTimeOfDay(selectedTime);
            }
        }

        private void SetTimeGoingButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TimeGoingBox.Text))
            {
                MessageBox.Show("Пожалуйста, заполните поле течения времени.");
                return;
            }

            if (double.TryParse(TimeGoingBox.Text, out double speed))
            {
                if (_sun != null)
                {
                    _sun.SetSunSpeed(speed);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректное числовое значение.");
            }
            
        }
        
        // public void TestSunflowerFieldLoading(object sender, RoutedEventArgs e)
        // {
        //     double width = 100.0;
        //     double height = 100.0;
        //
        //     for (int countSunflowers = 1; countSunflowers <= 401; countSunflowers += 20)
        //     {
        //         ClearScene();
        //
        //         Stopwatch stopwatch = Stopwatch.StartNew();
        //
        //         _sunflowerField = new SunflowerField(width, height, _fieldPath, countSunflowers, _sunflowerModelPath, _sunflowerTexturePath);
        //         
        //         stopwatch.Stop();
        //
        //         Console.WriteLine($"countSunflowers: {countSunflowers}, Time: {stopwatch.Elapsed.TotalMilliseconds} ms");
        //     }
        // }
        //
        // public void TestWindPerformance(object sender, RoutedEventArgs e)
        // {
        //     double width = 100.0;
        //     double height = 100.0;
        //     
        //     for (int countSunflowers = 1; countSunflowers <= 401; countSunflowers += 20)
        //     {
        //         ClearScene();
        //
        //         _sunflowerField = new SunflowerField(width, height, _fieldPath, countSunflowers, _sunflowerModelPath, _sunflowerTexturePath);
        //
        //         _sunflowerField.StartWind(amplitude: 0.1, frequency: 0.5);
        //         double cpuUsage = MeasureCpuUsage(duration: 10000); 
        //
        //         _sunflowerField.StopWind();
        //
        //         Console.WriteLine($"countSunflowers: {countSunflowers}, CPU usage : {cpuUsage:F2}%");
        //     }
        // }
        //
        // private double MeasureCpuUsage(int duration)
        // {
        //     PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        //     cpuCounter.NextValue();
        //
        //     Thread.Sleep(1000); 
        //
        //     float totalCpuUsage = 0;
        //     int samples = 0;
        //
        //     Stopwatch stopwatch = Stopwatch.StartNew();
        //     while (stopwatch.ElapsedMilliseconds < duration)
        //     {
        //         totalCpuUsage += cpuCounter.NextValue();
        //         samples++;
        //         Thread.Sleep(100);
        //     }
        //
        //     return totalCpuUsage / samples;
        // }
        //
        // public void TestSunflowerFieldLoadingWithVariableSceneSize(object sender, RoutedEventArgs e)
        // {
        //     int countSunflowers = 50; 
        //
        //     for (double width = 10; width <= 2010; width += 200)
        //     {
        //         for (double height = 10; height <= 2010; height += 200)
        //         {
        //             ClearScene();
        //
        //             Stopwatch stopwatch = Stopwatch.StartNew();
        //
        //             _sunflowerField = new SunflowerField(width, height, _fieldPath, countSunflowers, _sunflowerModelPath, _sunflowerTexturePath);
        //
        //             stopwatch.Stop();
        //
        //             Console.WriteLine($"Width: {width}, Length: {height}, Time downloading: {stopwatch.Elapsed.TotalMilliseconds} мс");
        //         }
        //     }
        // }

        // private void ClearScene()
        // {
        //     if (_sunflowerField != null)
        //     {
        //         _sunflowerField._modelGroup.Children.Clear();
        //         _sunflowerField._sunflowerModels.Clear();
        //         _sunflowerField._shadowModels.Clear();
        //         // Console.WriteLine("Сцена очищена.");
        //     }
        //     else
        //     {
        //         Console.WriteLine("Сцена не требует очистки, так как _sunflowerField равен null.");
        //     }
        // }
    }
}