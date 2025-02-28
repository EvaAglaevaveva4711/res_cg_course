using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace WpfApplication1
{
    public class CameraController
    {
        private const double CameraMoveAmount = 0.3;
        private const double ZoomAmount = 1.8;
        private const double RotationSpeed = 0.6;

        private PerspectiveCamera _camera;
        private Point lastMousePosition;
        private bool isRotating = false;

        public CameraController(PerspectiveCamera camera)
        {
            _camera = camera;
        }

        public void KeyDown(object sender, KeyEventArgs e)
        {
            Vector3D moveDirection = new Vector3D();

            switch (e.Key)
            {
                case Key.Up:
                    moveDirection = new Vector3D(0, CameraMoveAmount, 0);
                    break;
                case Key.Down:
                    moveDirection = new Vector3D(0, -CameraMoveAmount, 0);
                    break;
                case Key.Left:
                    moveDirection = new Vector3D(-CameraMoveAmount, 0, 0);
                    break;
                case Key.Right:
                    moveDirection = new Vector3D(CameraMoveAmount, 0, 0);
                    break;
            }

            _camera.Position = new Point3D(
                _camera.Position.X + moveDirection.X,
                _camera.Position.Y + moveDirection.Y,
                _camera.Position.Z + moveDirection.Z);
        }

        public void MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoom = e.Delta > 0 ? ZoomAmount : -ZoomAmount;
            Vector3D moveVector = _camera.LookDirection;
            moveVector.Normalize();
            moveVector *= zoom;

            _camera.Position = new Point3D(
                _camera.Position.X + moveVector.X,
                _camera.Position.Y + moveVector.Y,
                _camera.Position.Z + moveVector.Z);
        }

        public void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isRotating = true;
                lastMousePosition = e.GetPosition((IInputElement)sender);
            }
        }

        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                isRotating = false;
            }
        }

        public void MouseMove(object sender, MouseEventArgs e)
        {
            if (isRotating)
            {
                Point currentPosition = e.GetPosition((IInputElement)sender);
                double deltaX = -currentPosition.X + lastMousePosition.X;
                double deltaY = +currentPosition.Y - lastMousePosition.Y;

                RotateCameraDirection(deltaX, deltaY);
                lastMousePosition = currentPosition;
            }
        }

        private void RotateCameraDirection(double deltaX, double deltaY)
        {
            double angleX = deltaX * RotationSpeed;
            var rotationY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), angleX);
            var rotationTransformY = new RotateTransform3D(rotationY);
            _camera.LookDirection = rotationTransformY.Transform(_camera.LookDirection);

            double angleY = deltaY * RotationSpeed;
            Vector3D rightAxis = Vector3D.CrossProduct(_camera.LookDirection, new Vector3D(0, 1, 0));
            rightAxis.Normalize();

            var rotationX = new AxisAngleRotation3D(rightAxis, -angleY);
            var rotationTransformX = new RotateTransform3D(rotationX);
            _camera.LookDirection = rotationTransformX.Transform(_camera.LookDirection);
        }
    }
}
