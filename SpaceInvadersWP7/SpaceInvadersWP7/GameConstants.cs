using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace SpaceInvadersWP7
{
    static class GameConstants
    {
        //Viewport Defaults
        public const int resX = 800;
        public const int resY = 480;

        //Camera constants
        public const float CameraDistance = 25000.0f;
        //Full speed at which ship can rotate; measured in radians per second.
        public const float CameraRotationRate = (MathHelper.PiOver4 / 300);

        public const float ShipVerticalStartupOffset = -7000.0f;
        public const float VelocityScale = 15.0f; //amplifies controller speed input
        //Constants for the size of the playfield
        public const float PlayfieldSizeX = 21000f;
        public const float PlayfieldSizeY = 15000f;
        //Bullet constants
        public const int NumFriendlyBullets = 4;
        public const float BulletSpeedAdjustment = 100.0f;
        public const int NumEnemyBullets = 15;
        public const int EnemyFireRate = 50;
        //Enemy ships constants
        public const int NumEnemyRows = 3;
        public const int NumEnemyCols = 8; // Set to 10
        public const int NumEnemyLayers = 1;
        public const int EnemyRowOffset = 2500;
        public const int EnemyColOffset = 3000;
        public const int EnemyLayerOffset = 2500;
        public const float EnemyMinSpeedX = 100.0f;
        public const float EnemyMaxSpeedX = 5000.0f;
        public const float EnemyMinSpeedY = -100.0f;
        public const float EnemyMaxSpeedY = -200.0f;
        public const float EnemySpeedAdjustment = 5.0f;
        public const int EnemyFireDelay = 250;
        //Bullet collision constants
        public const float EnemyBoundingSphereScale = 0.9f * 250f;  //90% size
        public const float ShipBoundingSphereScale = 0.9f * 120f;  //90% size
        //Vibration Constants
        public const float VibrationDurationShipHit = 0.5f;
    }
}
