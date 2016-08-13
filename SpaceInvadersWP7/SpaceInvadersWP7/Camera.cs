using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SpaceInvadersWP7
{
    public class Camera
    {
        public enum CameraMode { Orbit, FirstPerson, Chase }

        public CameraMode Mode = CameraMode.Orbit;

        public Matrix projectionMatrix;
        //public Matrix viewMatrix;

        //public Vector3 LookAt;
        public Vector3 Position;
        public Vector3 Target;
        public float aspectRatio;
        public Vector3 Velocity;
        public float velocityX;
        public float velocityY;
        float positionAngleX;
        float positionAngleY;

        public Matrix RotationMatrix = Matrix.CreateRotationX(MathHelper.PiOver2);

        // We only need one Random object no matter how many Cameras we have
        private static readonly Random random = new Random();

        // Are we shaking?
        public bool shaking;

        // The maximum magnitude of our shake offset
        private float shakeMagnitude;

        // The total duration of the current shake
        private float shakeDuration;

        // A timer that determines how far into our shake we are
        private float shakeTimer;

        // The shake offset vector
        public Vector3 shakeOffset;

        public Matrix View
        {
            get
            {
                // Start with our regular position and target
                Vector3 position = Position;
                Vector3 target = Target;

                // If we're shaking, add our offset to our position and target
                if (shaking)
                {
                    position += shakeOffset;
                    target += shakeOffset;
                }

                // Return the matrix using our modified position and target
                if (Mode == CameraMode.Orbit)
                {
                    return Matrix.CreateLookAt(position, target, Vector3.Up);
                }
                else
                {
                    return Matrix.CreateLookAt(position, target, Vector3.Backward);
                }
            }
        }

        public Camera()
        {
            ResetDefaultOrbit();
        }

        public void ResetDefaultOrbit()
        {
            Mode = CameraMode.Orbit;
            positionAngleX = 0.0f;
            positionAngleY = (MathHelper.PiOver4 / 2);
            Position = new Vector3(0.0f, 0.0f, GameConstants.CameraDistance);
            Quaternion rotateY = Quaternion.CreateFromAxisAngle(Vector3.Right, positionAngleY);
            Position = Vector3.Transform(Position, Matrix.CreateFromQuaternion(rotateY));
            Target = Vector3.Zero;
            Velocity = Vector3.Zero;
            velocityX = GameConstants.CameraRotationRate;
            velocityY = -GameConstants.CameraRotationRate;
            //viewMatrix = Matrix.CreateLookAt(Position, Target, Vector3.Up);
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45.0f), aspectRatio, 5000.0f, 100000.0f);
        }

        public void SetFirstPerson()
        {
            Mode = CameraMode.FirstPerson;
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(80.0f), aspectRatio, 1.0f, 100000.0f);
        }

        /// <summary>
        /// Shakes the camera with a specific magnitude and duration.
        /// </summary>
        /// <param name="magnitude">The largest magnitude to apply to the shake.</param>
        /// <param name="duration">The length of time (in seconds) for which the shake should occur.</param>
        public void Shake(float magnitude, float duration)
        {
            // We're now shaking
            shaking = true;

            // Store our magnitude and duration
            shakeMagnitude = magnitude;
            shakeDuration = duration;

            // Reset our timer
            shakeTimer = 0f;
        }

        public void Update(GameTime gameTime, float timeDelta)
        {
            if (Mode == CameraMode.Orbit)
            {
                // Add velocity to the current camera position.
                Position += Velocity;

                //float angleX = controllerState.ThumbSticks.Right.X * GameConstants.CameraRotationRate;
                //Quaternion rotateX = Quaternion.CreateFromAxisAngle(Vector3.Up, angleX);
                //Position = Vector3.Transform(Position, Matrix.CreateFromQuaternion(rotateX));

                //float angleY = 0; // = controllerState.ThumbSticks.Right.Y * GameConstants.CameraRotationRate;
                //if (currentKeyState.IsKeyDown(Keys.Up))
                //    angleY = GameConstants.CameraRotationRate;
                //if (currentKeyState.IsKeyDown(Keys.Down))
                //    angleY = -GameConstants.CameraRotationRate;

                //positionAngleX += velocityX;
                //if (positionAngleX > (MathHelper.PiOver4 / 2))
                //{
                //    positionAngleX = (MathHelper.PiOver4 / 2);
                //    velocityX = -GameConstants.CameraRotationRate;
                //}
                //else if (positionAngleX < -(MathHelper.PiOver4 / 2))
                //{
                //    positionAngleX = -(MathHelper.PiOver4 / 2);
                //    velocityX = GameConstants.CameraRotationRate;
                //}
                //Quaternion rotateX = Quaternion.CreateFromAxisAngle(Vector3.Up, velocityX);
                //Position = Vector3.Transform(Position, Matrix.CreateFromQuaternion(rotateX));

                positionAngleY += velocityY;
                if (positionAngleY > (MathHelper.PiOver4))
                {
                    positionAngleY = (MathHelper.PiOver4);
                    velocityY = -GameConstants.CameraRotationRate;
                }
                else if (positionAngleY < (MathHelper.PiOver4 / 2))
                {
                    positionAngleY = (MathHelper.PiOver4 / 2);
                    velocityY = GameConstants.CameraRotationRate;
                }
                Quaternion rotateY = Quaternion.CreateFromAxisAngle(Vector3.Right, velocityY);
                Position = Vector3.Transform(Position, Matrix.CreateFromQuaternion(rotateY));

                //viewMatrix = Matrix.CreateLookAt(Position, Vector3.Zero, Vector3.Up);
                Target = Vector3.Zero;

                // Bleed off the camera velocity over time.
                Velocity *= 0.99f;
            }
            else
            {
                //viewMatrix = Matrix.CreateLookAt(Position, new Vector3(Position.X, Position.Y + 1000, Position.Z), Vector3.Backward);
                Target = new Vector3(Position.X, Position.Y + 1000, Position.Z);
            }

            // If we're shaking...
            if (shaking)
            {
                // Move our timer ahead based on the elapsed time
                shakeTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                // If we're at the max duration, we're not going to be shaking anymore
                if (shakeTimer >= shakeDuration)
                {
                    shaking = false;
                    shakeTimer = shakeDuration;
                }

                // Compute our progress in a [0, 1] range
                float progress = shakeTimer / shakeDuration;

                // Compute our magnitude based on our maximum value and our progress. This causes
                // the shake to reduce in magnitude as time moves on, giving us a smooth transition
                // back to being stationary. We use progress * progress to have a non-linear fall 
                // off of our magnitude. We could switch that with just progress if we want a linear 
                // fall off.
                float magnitude = shakeMagnitude * (1f - (progress * progress));

                // Generate a new offset vector with three random values and our magnitude
                shakeOffset = new Vector3(NextFloat(), NextFloat(), NextFloat()) * magnitude;
            }
        }

        /// <summary>
        /// Helper to generate a random float in the range of [-1, 1].
        /// </summary>
        private float NextFloat()
        {
            return (float)random.NextDouble() * 2f - 1f;
        }
    }
}
