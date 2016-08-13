using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SpaceInvadersWP7
{
    class Ship
    {
        public Model Model;

        // Set the initial position of the ship in world space to the bottom of the screen
        public Vector3 Position;

        //Velocity of the model, applied each frame to the model's position
        public Vector3 Velocity = Vector3.Zero;
        public bool isActive = true;

        public Matrix RotationMatrix = Matrix.CreateRotationX(MathHelper.PiOver2);

        public bool isDestroyed = false;

        public int currentHullIntegrity;

        public void Reset()
        {
            Position = new Vector3(0.0f, GameConstants.ShipVerticalStartupOffset, 0.0f);
            currentHullIntegrity = 1000;
            Velocity = Vector3.Zero;
            isDestroyed = false;
            isActive = true;
        }

        public void Update(KeyboardState currentKeyState)
        {
            // Finally, add this vector to our velocity.
            // Velocity += RotationMatrix.Right * GameConstants.VelocityScale * controllerState.ThumbSticks.Left.X;
            if (currentKeyState.IsKeyDown(Keys.Left))
                Velocity += RotationMatrix.Right * GameConstants.VelocityScale * -1;
            if (currentKeyState.IsKeyDown(Keys.Right))
                Velocity += RotationMatrix.Right * GameConstants.VelocityScale * 1;

            //Only allow dive/rise motion (Z-axis) if there are actually more than 1 layer of enemies
            //if (GameConstants.NumEnemyLayers > 1)
            //    Velocity += RotationMatrix.Down * GameConstants.VelocityScale * controllerState.ThumbSticks.Left.Y;

            // Add velocity to the current position.
            Position += Velocity;

            if (Position.X > (GameConstants.PlayfieldSizeX - GameConstants.EnemyColOffset))
            {
                Position.X = (GameConstants.PlayfieldSizeX - GameConstants.EnemyColOffset);
                Velocity.X = 0;
            }
            else if (Position.X < (-GameConstants.PlayfieldSizeX + GameConstants.EnemyColOffset))
            {
                Position.X = (-GameConstants.PlayfieldSizeX + GameConstants.EnemyColOffset);
                Velocity.X = 0;
            }
            else if (Position.Z < (-(GameConstants.NumEnemyLayers - 1) * GameConstants.EnemyLayerOffset))
            {
                Position.Z = (-(GameConstants.NumEnemyLayers - 1) * GameConstants.EnemyLayerOffset);
                Velocity.Z = 0;
            }
            else if (Position.Z > 0)
            {
                Position.Z = 0;
                Velocity.Z = 0;
            }
            else
            {
                // Bleed off velocity over time.
                Velocity *= 0.95f;
            }
        }
    }
}
