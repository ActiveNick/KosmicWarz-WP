using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace SpaceInvadersWP7
{
    class Enemy
    {
        public Vector3 position;
        public float speedX = GameConstants.EnemyMaxSpeedX;
        public float speedY = GameConstants.EnemyMaxSpeedY;
        public byte shipType;
        public bool isActive;
        public DateTime lastFired;

        public float ShatterTime = 0.0f;
        public bool isDestroyed = false;

        public Matrix RotationMatrix = Matrix.CreateRotationX(MathHelper.PiOver2);
            //* Matrix.CreateRotationZ(MathHelper.Pi);

        public void Update(float delta)
        {
            if (!isDestroyed)
            {
                position.X += speedX * delta;
                position.Y += speedY * delta;
            }
        }
    }
}
