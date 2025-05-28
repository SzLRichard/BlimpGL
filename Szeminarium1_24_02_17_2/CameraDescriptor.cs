using Silk.NET.Input;
using Silk.NET.Maths;
using System;

namespace Szeminarium1_24_02_17_2
{
    internal class CameraDescriptor
    {

        private const float FollowDistance = 350f;
        private const float FollowHeight = 150f;
        private const float RotationSpeed = 1f;
        private const float MovementSpeed = 500f;

        private Vector3D<float> _position;
        private Vector3D<float> _targetPosition = Vector3D<float>.Zero;
        private float _yaw = 0f;
        private float _pitch = -0.1f;

        public Vector3D<float> Position => _position;
        public Vector3D<float> Target => _targetPosition;
        public Vector3D<float> UpVector => Vector3D<float>.UnitY;
        public float Yaw => _yaw;

        public Vector3D<float> Forward => new Vector3D<float>(
            (float)Math.Sin(_yaw) * (float)Math.Cos(_pitch),
            (float)Math.Sin(_pitch),
            (float)Math.Cos(_yaw) * (float)Math.Cos(_pitch)
        );
        
        public Vector3D<float> Right => Vector3D.Normalize(Vector3D.Cross(Forward, UpVector));

        public void Reset()
        {
            _position = new Vector3D<float>(0, 0, 0);
            _yaw = 0f;
            _pitch = 0f;
        }

        public void Update(double deltaTime, Vector3D<float> blimpPosition)
        {
            var horizontalOffset = -new Vector3D<float>(
                (float)Math.Sin(_yaw),
                0,
                (float)Math.Cos(_yaw)) * FollowDistance;

            var verticalOffset = Vector3D<float>.UnitY * FollowHeight;

            var desiredPosition = blimpPosition + horizontalOffset + verticalOffset;
            _position = Vector3D.Lerp(_position, desiredPosition, (float)(deltaTime * 5));
            _targetPosition = blimpPosition;
        }

        public void ProcessInput(double deltaTime, IKeyboard keyboard, ref Vector3D<float> blimpPosition)
        {
            var moveSpeed = (float)(MovementSpeed * deltaTime);
            var rotSpeed = (float)(RotationSpeed * deltaTime);

            var horizontalForward = new Vector3D<float>(
                (float)Math.Sin(_yaw),
                0,
                (float)Math.Cos(_yaw));

            var horizontalRight = Vector3D.Normalize(Vector3D.Cross(horizontalForward, UpVector));

            if (keyboard.IsKeyPressed(Key.W))
            {
                blimpPosition += horizontalForward * moveSpeed;
            }
            if (keyboard.IsKeyPressed(Key.S))
            {
                blimpPosition -= horizontalForward * moveSpeed;
            }
            if (keyboard.IsKeyPressed(Key.A))
            {
                blimpPosition -= horizontalRight * moveSpeed;
            }
            if (keyboard.IsKeyPressed(Key.D))
            {
                blimpPosition += horizontalRight * moveSpeed;
            }

            if (keyboard.IsKeyPressed(Key.E))
            {
                _yaw -= rotSpeed;
            }
            if (keyboard.IsKeyPressed(Key.Q))
            {
                _yaw += rotSpeed;
            }

            if (keyboard.IsKeyPressed(Key.R))
            {
                _pitch = Math.Clamp(_pitch + rotSpeed, -MathF.PI / 6f, MathF.PI / 6f);
            }
            if (keyboard.IsKeyPressed(Key.F))
            {
                _pitch = Math.Clamp(_pitch - rotSpeed, -MathF.PI / 6f, MathF.PI / 6f);
            }
        }
    }
}