using Silk.NET.Input;
using Silk.NET.Maths;
using System;

namespace Szeminarium1_24_02_17_2
{
    internal class CameraDescriptor
    {

        private const float FollowDistance = 400f;
        private const float FollowHeight = 300f;
        private const float RotationSpeed = 20f;
        private const float MovementSpeed = 30f;

        private Vector3D<float> _position;
        private Vector3D<float> _targetPosition = Vector3D<float>.Zero;
        private float _yaw = 0f;
        private float _pitch = 0.1f;

        public Vector3D<float> Position => _position;
        public Vector3D<float> Target => _targetPosition;
        public Vector3D<float> UpVector => Vector3D<float>.UnitY;
        public float Yaw => _yaw;
        public Vector3D<float> Forward => new Vector3D<float>(
            (float)Math.Sin(_yaw),
            0,
            (float)Math.Cos(_yaw)
        );
        public Vector3D<float> Right => Vector3D.Normalize(Vector3D.Cross(Forward, UpVector));

        public void Update(double deltaTime, Vector3D<float> blimpPosition)
        {
            var cameraOffset = -Forward * FollowDistance + Vector3D<float>.UnitY * FollowHeight;
            var desiredPosition = blimpPosition + cameraOffset;

            _position = Vector3D.Lerp(_position, desiredPosition, (float)(deltaTime * 5));
            _targetPosition = blimpPosition;
        }

        public void ProcessInput(double deltaTime, IKeyboard keyboard, ref Vector3D<float> blimpPosition)
        {
            var moveSpeed = (float)(MovementSpeed * deltaTime);
            var rotSpeed = (float)(RotationSpeed * deltaTime);

            if (keyboard.IsKeyPressed(Key.W))
            {
                blimpPosition += Forward * moveSpeed;
            }
            if (keyboard.IsKeyPressed(Key.S))
            {
                blimpPosition -= Forward * moveSpeed;
            }
            if (keyboard.IsKeyPressed(Key.A))
            {
                blimpPosition -= Right * moveSpeed;
            }
            if (keyboard.IsKeyPressed(Key.D))
            {
                blimpPosition += Right * moveSpeed;
            }

            if (keyboard.IsKeyPressed(Key.Q))
            {
                _yaw -= rotSpeed;
            }
            if (keyboard.IsKeyPressed(Key.E))
            {
                _yaw += rotSpeed;
            }

            if (keyboard.IsKeyPressed(Key.R))
            {
                _pitch = Math.Clamp(_pitch + rotSpeed, -MathF.PI / 4, MathF.PI / 4);
            }
            if (keyboard.IsKeyPressed(Key.F))
            {
                _pitch = Math.Clamp(_pitch - rotSpeed, -MathF.PI / 4, MathF.PI / 4);
            }
        }
    }
}