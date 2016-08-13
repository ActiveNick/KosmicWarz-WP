#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion
#define HUD
#define MUSIC
//#define FRAMERATE

#region Using Statements

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using Microsoft.Devices;
using Microsoft.Devices.Sensors;
using GameStateManagement;
using DPSF;
using DPSF.ParticleSystems;
#endregion


namespace SpaceInvadersWP7
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    class GameplayScreen : GameScreen
    {
        #region Fields

        ContentManager content;
        //SpriteFont gameFont;

        //Vector2 playerPosition = new Vector2(100, 100);
        //Vector2 enemyPosition = new Vector2(100, 100);

        //Random random = new Random();

        float pauseAlpha;

        InputAction pauseAction;

        KeyboardState lastKeyState = Keyboard.GetState();

        bool paused = false;

        #region GamePlay & 3D Objects Declarations

        Texture2D stars;
        Sky sky;

        //Camera/View information
        Camera camera = new Camera();

        //Visual Components and 3D Models

        // This is the main player ship, which includes its own model
        Ship ship = new Ship();

        // Declarations for the bullet models, transforms and bullet list
        Model shipBulletModel, enemyBulletModel;
        Bullet[] shipBulletList = new Bullet[GameConstants.NumFriendlyBullets];
        Bullet[] enemyBulletList = new Bullet[GameConstants.NumEnemyBullets];

        // Enemy object declarations
        Model enemyModel1, enemyModel2, enemyModel3;
        Enemy[, ,] enemyList = new Enemy[GameConstants.NumEnemyRows, GameConstants.NumEnemyCols, GameConstants.NumEnemyLayers];
        int numEnemies, maxEnemies;

        // Scaling matrix we are using to make the neemy bullets easier to see
        Matrix EnemyBulletTransformMatrix = Matrix.CreateScale(1.5f) * Matrix.CreateRotationX(MathHelper.PiOver2);

        // Game Difficulty Settings/Tuning
        int currentLevel = 1;
        int enemyFireRate = GameConstants.EnemyFireRate;
        int maxFriendlyBullets = GameConstants.NumFriendlyBullets;
        int maxEnemyBullets = GameConstants.NumEnemyBullets;

        #endregion

        #region 2D Graphics Declarations

        // Declare objects for the space background texture and its SpriteBatch
        SpriteBatch spriteBatch;
        // PrimitiveBatch is used it to draw lines like the HUD cross hair
        //PrimitiveBatch primitiveBatch;
        // HUD/UI Text
#if (HUD)
            SpriteFont fontUI;
#endif
        SpriteFont fontGameOver;

        #endregion

        #region Audio & Music Declarations

        // Audio objects
        //SoundEffect soundEffect;
        SoundEffect sfxPlayerFire, sfxEnemyFire, sfxPlayerEngine, sfxExplosion1, sfxExplosion2, sfxExplosion3, sfxPlayerHit, sfxPlayerExplode;
        // Needed for random explosion sounds when we blow up an enemy
        Random random = new Random();

#if (MUSIC)
        Song ADifferentJourney;
        bool songStart = false;
#endif

        #endregion

        #region Input, Sensors & Gestures Declarations

        // Gesture Variables
        bool isFiring = false;

        Accelerometer accelerometer;

        //Requires Microsoft.Devices in Microsoft.Phone.dll
        VibrateController vc = VibrateController.Default;

        #endregion

        #region Particle System Declarations

        // 2D PARTICLE SYSTEM
        // we want a sprite to represent our smokingEmitter
        Texture2D emitterSprite;

        // Here's the really fun part of the sample, the particle systems! These are
        // drawable game components, so we can just add them to the components
        // collection. Read more about each particle system in their respective source
        // files.
        ParticleSystem explosion;
        ParticleSystem smoke;
        ParticleSystem smokePlume;

        // For our Emitter test, we need both a ParticleEmitter and ParticleSystem
        ParticleEmitter emitter;
        ParticleSystem emitterSystem;

        // DPSF Particle System
        SpaceTravel spaceTravelPS = null;

        #endregion

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            pauseAction = new InputAction(
                new Buttons[] { Buttons.Start, Buttons.Back },
                new Keys[] { Keys.Escape },
                true);
        }


        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void Activate(bool instancePreserved)
        {
            if (!instancePreserved)
            {
                if (content == null)
                    content = new ContentManager(this.ScreenManager.Game.Services, "Content");

                // TODO: Add your initialization logic here
                // Prepare the camera            
                camera.aspectRatio = (float)this.ScreenManager.GraphicsDevice.Viewport.Width / this.ScreenManager.GraphicsDevice.Viewport.Height;

#if (FRAMERATE)
                // We add a framerate counter if required during debug mode
                this.ScreenManager.Game.Components.Add(new FrameRateCounter(ScreenManager.Game));
#endif
                // 2D PARTICLE SYSTEM
                // create the particle systems and add them to the components list.
                explosion = new ParticleSystem(this.ScreenManager.Game, "ExplosionSettings") { DrawOrder = ParticleSystem.AdditiveDrawOrder };
                this.ScreenManager.Game.Components.Add(explosion);

                smoke = new ParticleSystem(this.ScreenManager.Game, "ExplosionSmokeSettings") { DrawOrder = ParticleSystem.AlphaBlendDrawOrder };
                this.ScreenManager.Game.Components.Add(smoke);

                smokePlume = new ParticleSystem(this.ScreenManager.Game, "SmokePlumeSettings") { DrawOrder = ParticleSystem.AlphaBlendDrawOrder };
                this.ScreenManager.Game.Components.Add(smokePlume);

                emitterSystem = new ParticleSystem(this.ScreenManager.Game, "EmitterSettings") { DrawOrder = ParticleSystem.AlphaBlendDrawOrder };
                this.ScreenManager.Game.Components.Add(emitterSystem);
                emitter = new ParticleEmitter(emitterSystem, 60, new Vector2(400, 240));

                // Camera
                camera.projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                    MathHelper.ToRadians(45.0f), camera.aspectRatio, 1.0f, 3000000.0f);

                // Enable the gestures we care about. You must set EnabledGestures before
                // you can use any of the other gesture APIs.
                // We use both Tap and DoubleTap to workaround a bug in the XNA GS 4.0 Beta
                // where some Taps are missed if only Tap is specified.
                this.EnabledGestures =
                    GestureType.Hold |
                    GestureType.Tap |
                    GestureType.DoubleTap |
                    GestureType.FreeDrag |
                    GestureType.Flick |
                    GestureType.Pinch |
                    GestureType.PinchComplete;

                // Instantiate the accelerometer sensor
                accelerometer = new Accelerometer();
                accelerometer.ReadingChanged += new EventHandler<AccelerometerReadingEventArgs>(accelerometer_ReadingChanged);
                try
                {
                    accelerometer.Start();
                }
                catch (AccelerometerFailedException ex)
                {
                    // "error starting accelerometer";
                }

                ResetLevel(currentLevel);

                LoadContent();

                //gameFont = content.Load<SpriteFont>("gamefont");

                // A real game would probably have more content than this sample, so
                // it would take longer to load. We simulate that by delaying for a
                // while, giving you a chance to admire the beautiful loading screen.
                Thread.Sleep(1000);

                // once the load has finished, we use ResetElapsedTime to tell the game's
                // timing mechanism that we have just finished a very long frame, and that
                // it should not try to catch up.
                this.ScreenManager.Game.ResetElapsedTime();
            }

            if (Microsoft.Phone.Shell.PhoneApplicationService.Current.State.ContainsKey("PlayerPosition"))
            {
                //playerPosition = (Vector2)Microsoft.Phone.Shell.PhoneApplicationService.Current.State["PlayerPosition"];
                //enemyPosition = (Vector2)Microsoft.Phone.Shell.PhoneApplicationService.Current.State["EnemyPosition"];
            }
        }

        protected void LoadContent()
        {
            // Ship model
            ship.Model = LoadModelWithLighting("Models/PlayerShip"); //p1_wedge");
            // Enemy models
            enemyModel1 = LoadModelWithLighting("Models/EnemyShip1"); //p1_saucer");
            enemyModel2 = LoadModelWithLighting("Models/EnemyShip1"); //p2_saucer");
            enemyModel3 = LoadModelWithLighting("Models/EnemyShip1"); //p2_wedge");
            // Load the bullet model and set default effect transforms
            shipBulletModel = LoadModelWithLighting("Models/pea_proj");
            enemyBulletModel = LoadModelWithLighting("Models/mgun_proj");

            //Sound Initialization
            sfxPlayerFire = ScreenManager.Game.Content.Load<SoundEffect>("Audio/Waves/BoltFire");
            sfxPlayerEngine = ScreenManager.Game.Content.Load<SoundEffect>("Audio/Waves/engine_2");
            sfxPlayerHit = ScreenManager.Game.Content.Load<SoundEffect>("Audio/Waves/CarCrashMinor");
            sfxPlayerExplode = ScreenManager.Game.Content.Load<SoundEffect>("Audio/Waves/Shiphit");
            sfxEnemyFire = ScreenManager.Game.Content.Load<SoundEffect>("Audio/Waves/tx0_fire1");
            sfxExplosion1 = ScreenManager.Game.Content.Load<SoundEffect>("Audio/Waves/explosion1");
            sfxExplosion2 = ScreenManager.Game.Content.Load<SoundEffect>("Audio/Waves/explosion2");
            sfxExplosion3 = ScreenManager.Game.Content.Load<SoundEffect>("Audio/Waves/explosion3");

            // Initialize the SpriteBatch
            stars = ScreenManager.Game.Content.Load<Texture2D>("Textures/B1_stars");
            spriteBatch = new SpriteBatch(ScreenManager.GraphicsDevice);
            //primitiveBatch = new PrimitiveBatch(graphics.GraphicsDevice);

            sky = ScreenManager.Game.Content.Load<Sky>("skygalaxies1"); // was skymoon1 before the SpaceTravel effect was added

            // 2D Particle emitter for enemy explosions
            emitterSprite = ScreenManager.Game.Content.Load<Texture2D>("BlockEmitter");

            //Load DPSF Particle System Assets
            spaceTravelPS = new SpaceTravel(this.ScreenManager.Game);
            spaceTravelPS.AutoInitialize(this.ScreenManager.GraphicsDevice, this.ScreenManager.Game.Content, spriteBatch);

#if (MUSIC)
            ADifferentJourney = ScreenManager.Game.Content.Load<Song>("Audio/Music/ADifferentJourney");
            MediaPlayer.IsRepeating = true;
#endif

#if (HUD)
            fontUI = ScreenManager.Game.Content.Load<SpriteFont>("Fonts/HUD");
#endif
            fontGameOver = ScreenManager.Game.Content.Load<SpriteFont>("Fonts/GameOver");
        }

        /// <summary>
        /// LoadModelWithLighting is used to load a model and enable default
        /// lighting on that model at the same time
        /// </summary>
        protected Model LoadModelWithLighting(string modelName)
        {
            Model model;
            model = ScreenManager.Game.Content.Load<Model>(modelName);
            foreach (ModelMesh mesh in model.Meshes)
            {
                // This is where the model default lighting is set
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                }
            }
            return model;
        }

        // Set the initial position for the rows of enemies on screen
        private void ResetLevel(int currentLevel)
        {
            float xStart;
            float yStart;
            float zStart;
            Enemy enemy;
            int shipType;
            for (int d = 0; d < GameConstants.NumEnemyLayers; d++)
            {
                shipType = (d + 1);
                if (shipType == 4) { shipType = 1; }
                for (int i = 0; i < GameConstants.NumEnemyRows; i++)
                {
                    for (int j = 0; j < GameConstants.NumEnemyCols; j++)
                    {
                        enemy = new Enemy();
                        enemyList[i, j, d] = enemy;
                        xStart = (float)-GameConstants.PlayfieldSizeX + 5500 + (j * GameConstants.EnemyColOffset);
                        yStart = (float)GameConstants.PlayfieldSizeY - 7000 + (i * GameConstants.EnemyRowOffset);
                        zStart = (float)GameConstants.EnemyLayerOffset * -d;
                        enemyList[i, j, d].position = new Vector3(xStart, yStart, zStart);
                        enemyList[i, j, d].shipType = (byte)shipType;
                        enemyList[i, j, d].speedX = (GameConstants.EnemyMaxSpeedX * (1.0f + ((float)currentLevel / 10.0f)));
                        enemyList[i, j, d].speedY = (GameConstants.EnemyMaxSpeedY * (1.0f + ((float)currentLevel / 10.0f)) / (float)GameConstants.NumEnemyLayers);
                        enemyList[i, j, d].isActive = true;
                        enemyList[i, j, d].isDestroyed = false;
                        enemyList[i, j, d].ShatterTime = 0.0f;
                    }

                    // Cycle each row through each enemy ship type
                    shipType += 1;
                    if (shipType == 4) { shipType = 1; }
                }
            }

            shipBulletList = new Bullet[maxFriendlyBullets];
            enemyBulletList = new Bullet[maxEnemyBullets + currentLevel - 1];
            enemyFireRate = GameConstants.EnemyFireRate + ((currentLevel - 1) * 10);

            numEnemies = (GameConstants.NumEnemyRows * GameConstants.NumEnemyCols * GameConstants.NumEnemyLayers);
            maxEnemies = numEnemies;

            ship.Reset();

            GC.Collect();
        }

        public override void Deactivate()
        {
            //Microsoft.Phone.Shell.PhoneApplicationService.Current.State["PlayerPosition"] = playerPosition;
            //Microsoft.Phone.Shell.PhoneApplicationService.Current.State["EnemyPosition"] = enemyPosition;

            base.Deactivate();
        }


        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void Unload()
        {
            spaceTravelPS.Destroy();
            content.Unload();

            //Microsoft.Phone.Shell.PhoneApplicationService.Current.State.Remove("PlayerPosition");
            //Microsoft.Phone.Shell.PhoneApplicationService.Current.State.Remove("EnemyPosition");
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);

            // Gradually fade in or out depending on whether we are covered by the pause screen.
            if (coveredByOtherScreen)
                pauseAlpha = Math.Min(pauseAlpha + 1f / 32, 1);
            else
                pauseAlpha = Math.Max(pauseAlpha - 1f / 32, 0);

            if (IsActive)
            {
                // TODO: this game isn't very fun! You could probably improve
                // it by inserting something more interesting in this space :-)
#if (MUSIC)
                if (!songStart)
                {
                    MediaPlayer.Play(ADifferentJourney);
                    songStart = true;
                }
#endif

                if (!paused)
                {
                    // Calculate the amount of time elapsed since update was called last
                    float timeDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if (camera.Mode == Camera.CameraMode.FirstPerson)
                    {
                        camera.Position = ship.Position;
                        camera.Position.Y += 100;
                        camera.Position.Z += 500;
                    }

                    camera.Update(gameTime, timeDelta);

                    // Update enemy ship positions for remaining enemies
                    bool reverseX = false;
                    for (int d = 0; d < GameConstants.NumEnemyLayers; d++)
                    {
                        for (int i = 0; i < GameConstants.NumEnemyRows; i++)
                        {
                            for (int j = 0; j < GameConstants.NumEnemyCols; j++)
                            {
                                if (enemyList[i, j, d].isActive)
                                {
                                    enemyList[i, j, d].Update(timeDelta);
                                    // Check to see if one of the remaining
                                    if (!reverseX)
                                    {
                                        if ((enemyList[i, j, d].position.X >= (GameConstants.PlayfieldSizeX - GameConstants.EnemyColOffset)) ||
                                            (enemyList[i, j, d].position.X <= (-GameConstants.PlayfieldSizeX + GameConstants.EnemyColOffset)))
                                        {
                                            reverseX = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // If any active ship has hit either screen limit on the left and right,
                    // we reverse the X-axis speed of *all* the enemies
                    if (reverseX)
                    {
                        for (int d = 0; d < GameConstants.NumEnemyLayers; d++)
                        {
                            for (int i = 0; i < GameConstants.NumEnemyRows; i++)
                            {
                                for (int j = 0; j < GameConstants.NumEnemyCols; j++)
                                {
                                    enemyList[i, j, d].speedX *= -1.0f;
                                }
                            }
                        }
                    }

                    // Time for the enemy ships to fire at us! Let's pick a ship at random
                    int row = Randomize(0, GameConstants.NumEnemyRows - 1);
                    int col = Randomize(0, GameConstants.NumEnemyCols - 1);
                    int layer = Randomize(0, GameConstants.NumEnemyLayers - 1);
                    // Check to see if that specific ship decided to fire
                    if ((Randomize(1, 1000) <= (enemyFireRate * (int)(maxEnemies / Math.Max(numEnemies, 1)))) && !enemyList[row, col, layer].isDestroyed)
                    {
                        if ((DateTime.Now - enemyList[row, col, layer].lastFired).Milliseconds > GameConstants.EnemyFireDelay)
                        {
                            for (int k = 0; k < GameConstants.NumEnemyBullets; k++)
                            {
                                if (!enemyBulletList[k].isActive)
                                {
                                    enemyList[row, col, layer].lastFired = DateTime.Now;
                                    enemyBulletList[k].direction = ship.RotationMatrix.Backward;
                                    enemyBulletList[k].speed = GameConstants.BulletSpeedAdjustment;
                                    enemyBulletList[k].position = enemyList[row, col, layer].position - (900 * enemyBulletList[k].direction);
                                    enemyBulletList[k].isActive = true;
                                    enemyBulletList[k].isEnemy = true;
                                    PlayGameSound(sfxEnemyFire);
                                    break; //exit the loop     
                                }
                            }
                        }
                    }

                    // Call update on each active bullet currently on screen
                    for (int i = 0; i < maxFriendlyBullets; i++)
                    {
                        if (shipBulletList[i].isActive)
                        {
                            shipBulletList[i].Update(timeDelta);
                        }
                    }
                    for (int i = 0; i < GameConstants.NumEnemyBullets; i++)
                    {
                        if (enemyBulletList[i].isActive)
                        {
                            enemyBulletList[i].Update(timeDelta);
                        }
                    }

                    CollisionCheckEnemy(timeDelta);
                    CollisionCheckPlayer(timeDelta, gameTime);

                    // Update Particles
                    spaceTravelPS.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                }
            }
        }
        
        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(GameTime gameTime, InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            int playerIndex = (int)ControllingPlayer.Value;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

            // The game pauses either if the user presses the pause button, or if
            // they unplug the active gamepad. This requires us to keep track of
            // whether a gamepad was ever plugged in, because we don't want to pause
            // on PC if they are playing with a keyboard and have no gamepad at all!
            bool gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];

            PlayerIndex player;
            if (pauseAction.Evaluate(input, ControllingPlayer, out player) || gamePadDisconnected)
            {
                ScreenManager.AddScreen(new PhonePauseScreen(), ControllingPlayer);
            }
            else
            {
                isFiring = false;
                KeyboardState currentKeyState = Keyboard.GetState();

                if (input.TouchState.Count > 0)
                {
                    HandleTouchGestures(input);

                }
                // Get some input.
                UpdateInput(gameTime, currentKeyState);
            }
        }

        #region Collision Checks
        protected void CollisionCheckEnemy(float timeDelta)
        {
            // Player bullet-Enemy collision check
            Model targetModel;
            for (int d = 0; d < GameConstants.NumEnemyLayers; d++)
            {
                for (int i = 0; i < GameConstants.NumEnemyRows; i++)
                {
                    for (int j = 0; j < GameConstants.NumEnemyCols; j++)
                    {
                        // Only check for ships that have not been removed off the field
                        if (enemyList[i, j, d].isActive)
                        {
                            // Then make sure that ships now currently blowing up in pieces already
                            if (!enemyList[i, j, d].isDestroyed)
                            {
                                // Pick which model to use for a specific enemy row
                                switch (enemyList[i, j, d].shipType)
                                {
                                    case 1:
                                        targetModel = enemyModel1;
                                        break;
                                    case 2:
                                        targetModel = enemyModel2;
                                        break;
                                    default:
                                        targetModel = enemyModel3;
                                        break;
                                }

                                BoundingSphere enemySphere =
                                  new BoundingSphere(enemyList[i, j, d].position,
                                           targetModel.Meshes[0].BoundingSphere.Radius *
                                                 GameConstants.EnemyBoundingSphereScale);
                                for (int k = 0; k < shipBulletList.Length; k++)
                                {
                                    if (shipBulletList[k].isActive)
                                    {
                                        BoundingSphere bulletSphere = new BoundingSphere(
                                          shipBulletList[k].position,
                                          shipBulletModel.Meshes[0].BoundingSphere.Radius);
                                        if (enemySphere.Intersects(bulletSphere))
                                        {
                                            //Display explosion
                                            Vector2 where2 = Vector2.Zero;
                                            Matrix thisWorld = Matrix.CreateTranslation(enemyList[i, j, d].position);
                                            // create the explosion at the destroyed enemy ship location
                                            Vector3 where3 = ScreenProject(enemyList[i, j, d].position, ScreenManager.GraphicsDevice.Viewport, camera, thisWorld);
                                            where2.X = where3.X;
                                            where2.Y = where3.Y;

                                            // the overall explosion effect is actually comprised of two particle
                                            // systems: the fiery bit, and the smoke behind it. add particles to
                                            // both of those systems.
                                            explosion.AddParticles(where2, Vector2.Zero);
                                            //smoke.AddParticles(where, Vector2.Zero);

                                            // Randomly choose one of 3 explosion sounds for a cooler effect
                                            PlayRandomExplosionSound();
                                            shipBulletList[k].isActive = false;
                                            enemyList[i, j, d].isDestroyed = true;
                                            enemyList[i, j, d].isActive = false;
                                            numEnemies--;
                                            if (numEnemies == 0)
                                                ResetLevel(currentLevel++);
                                            break; //no need to check other bullets
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void CollisionCheckPlayer(float timeDelta, GameTime gameTime)
        {
            if (ship.isActive)
            {
                if (!ship.isDestroyed)
                {
                    // Player bullet-Enemy collision check
                    BoundingSphere shipSphere =
                      new BoundingSphere(ship.Position,
                               ship.Model.Meshes[0].BoundingSphere.Radius *
                                     GameConstants.ShipBoundingSphereScale);
                    for (int k = 0; k < enemyBulletList.Length; k++)
                    {
                        if (enemyBulletList[k].isActive)
                        {
                            BoundingSphere bulletSphere = new BoundingSphere(
                              enemyBulletList[k].position,
                              enemyBulletModel.Meshes[0].BoundingSphere.Radius); //.Transform(EnemyBulletTransformMatrix);
                            if (shipSphere.Intersects(bulletSphere))
                            {
                                //Display explosion
                                Vector2 where2 = Vector2.Zero;
                                Matrix thisWorld = Matrix.CreateTranslation(enemyBulletList[k].position);
                                // create the explosion at the destroyed enemy ship location
                                Vector3 where3 = ScreenProject(enemyBulletList[k].position, ScreenManager.GraphicsDevice.Viewport, camera, thisWorld);
                                where2.X = where3.X;
                                where2.Y = where3.Y;

                                // the overall explosion effect is actually comprised of two particle
                                // systems: the fiery bit, and the smoke behind it. add particles to
                                // both of those systems.
                                explosion.AddParticles(where2, Vector2.Zero);
                                //smoke.AddParticles(where, Vector2.Zero);

                                // We have been hit!!!
                                PlayGameSound(sfxPlayerHit);
                                ship.currentHullIntegrity -= 250;
                                enemyBulletList[k].isActive = false;
                                //vibrate the phone
                                vc.Start(TimeSpan.FromMilliseconds(500));
                                //Shake the camera
                                camera.Shake(250f, 2f);
                                //Now check if the ship got too many hits and blows up!
                                if (ship.currentHullIntegrity <= 0)
                                {
                                    //Display explosion
                                    where2 = Vector2.Zero;
                                    thisWorld = Matrix.CreateTranslation(ship.Position);
                                    // create the explosion at the destroyed enemy ship location
                                    where3 = ScreenProject(ship.Position, ScreenManager.GraphicsDevice.Viewport, camera, thisWorld);
                                    where2.X = where3.X;
                                    where2.Y = where3.Y;

                                    // the overall explosion effect is actually comprised of two particle
                                    // systems: the fiery bit, and the smoke behind it. add particles to
                                    // both of those systems.
                                    explosion.AddParticles(where2, Vector2.Zero);
                                    smoke.AddParticles(where2, Vector2.Zero);

                                    PlayGameSound(sfxPlayerExplode);
                                    ship.currentHullIntegrity = 0;
                                    ship.isDestroyed = true;
                                    ship.isActive = false;
                                    //Prevent the remaining enemy ships from moving down the screen
                                    for (int d = 0; d < GameConstants.NumEnemyLayers; d++)
                                    {
                                        for (int i = 0; i < GameConstants.NumEnemyRows; i++)
                                        {
                                            for (int j = 0; j < GameConstants.NumEnemyCols; j++)
                                            {
                                                if (enemyList[i, j, d].isActive)
                                                {
                                                    enemyList[i, j, d].speedY = 0.0f;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Manage Player Input
        protected void UpdateInput(GameTime gameTime, KeyboardState currentKeyState)
        {
            // Get the game pad state.
            if (!ship.isDestroyed && ship.isActive)
                ship.Update(currentKeyState);

            // Check to see if we are shooting?
            if (ship.isActive && !ship.isDestroyed && (isFiring || ((currentKeyState.IsKeyDown(Keys.Space) && (!lastKeyState.IsKeyDown(Keys.Space))))))
            {
                // Add another bullet.  Find an inactive bullet slot and use it
                // If all bullets slots are used, ignore the user input
                for (int i = 0; i < maxFriendlyBullets; i++)
                {
                    if (!shipBulletList[i].isActive)
                    {
                        shipBulletList[i].direction = ship.RotationMatrix.Forward;
                        shipBulletList[i].speed = GameConstants.BulletSpeedAdjustment;
                        shipBulletList[i].position = ship.Position + (900 * shipBulletList[i].direction);
                        shipBulletList[i].isActive = true;
                        //PlayGameSound("BoltFire");
                        PlayGameSound(sfxPlayerFire);
                        break; //exit the loop     
                    }
                }
            }
        }

        protected void HandleTouchGestures(InputState input)
        {
            // next we handle all of the gestures. since we may have multiple gestures available,
            // we use a loop to read in all of the gestures. this is important to make sure the 
            // TouchPanel's queue doesn't get backed up with old data
            foreach (GestureSample gesture in input.Gestures)
            {
                // read the next gesture from the queue
                //GestureSample gesture = TouchPanel.ReadGesture();

                // we can use the type of gesture to determine our behavior
                switch (gesture.GestureType)
                {
                    // on taps, we fire bullets from our ship
                    case GestureType.Tap:
                    case GestureType.DoubleTap:
                        isFiring = true;
                        break;

                    // on holds, pause and unpause the game
                    case GestureType.Hold:
                        if (ship.isActive)
                        {
                            paused = !paused;
                        }
                        else
                        {
                            currentLevel = 1;
                            ResetLevel(currentLevel);
                        }
                        break;

                    // on drags... nothing for now, could tie this to a free camera move
                    case GestureType.FreeDrag:
                        float angleY = 0;
                        angleY = gesture.Delta.Y / 500;
                        Quaternion rotateY = Quaternion.CreateFromAxisAngle(Vector3.Right, angleY);
                        camera.Position = Vector3.Transform(camera.Position, Matrix.CreateFromQuaternion(rotateY));
                        break;

                    // on flicks, we want to update the ship speed (X) or camera pitch (Y)
                    // The flick velocity is measured in pixels per second.
                    case GestureType.Flick:
                        if (!ship.isActive)
                        {
                            if (gesture.Delta.X < -1000)
                            {
                                currentLevel = 1;
                                ResetLevel(currentLevel);
                            }
                        }
                        //    if (gesture.Delta.X > 500)
                        //    {
                        //        ship.Velocity += ship.RotationMatrix.Right * GameConstants.VelocityScale * 40;
                        //        //PlayGameSound("engine_2");
                        //        PlayGameSound(sfxPlayerEngine);
                        //    }
                        //    else if (gesture.Delta.X < -500)
                        //    {
                        //        ship.Velocity += ship.RotationMatrix.Right * GameConstants.VelocityScale * -40;
                        //        //PlayGameSound("engine_2");
                        //        PlayGameSound(sfxPlayerEngine);
                        //    }
                        //    float angleY = 0;
                        //    if (gesture.Delta.Y > 500)
                        //    {
                        //        angleY = 0.1f;
                        //    }
                        //    else if (gesture.Delta.Y < -500)
                        //    {
                        //        angleY = -0.1f;
                        //    }
                        //    Quaternion rotateY = Quaternion.CreateFromAxisAngle(Vector3.Right, angleY);
                        //    camera.Position = Vector3.Transform(camera.Position, Matrix.CreateFromQuaternion(rotateY));
                        break;

                    // On pinches, switch between Orbit View and 1st person view
                    case GestureType.PinchComplete:
                        if (gesture.Timestamp > TimeSpan.FromMilliseconds(500))
                        {
                            if (camera.Mode == Camera.CameraMode.Orbit)
                            {
                                camera.SetFirstPerson();
                            }
                            else
                            {
                                camera.ResetDefaultOrbit();
                            }
                        }
                        break;
                }
            }
        }

        void accelerometer_ReadingChanged(object sender, AccelerometerReadingEventArgs e)
        {
            if (e.Y > 0)
            {
                ship.Velocity = ship.RotationMatrix.Left * (float)e.Y * GameConstants.VelocityScale * 75;
                //PlayGameSound("engine_2");
            }
            else if (e.Y < 0)
            {
                ship.Velocity = ship.RotationMatrix.Left * (float)e.Y * GameConstants.VelocityScale * 75;
                //PlayGameSound("engine_2");
            }
        }
        #endregion

        // Play a sound effect file during the game action
        private void PlayGameSound(SoundEffect sfx)
        {
            SoundEffectInstance sfxInstance = sfx.CreateInstance();
            sfxInstance.Play();
        }

        // Play a sound effect file during the game action
        private void PlayRandomExplosionSound()
        {
            SoundEffectInstance sfxInstance;

            switch (Randomize(1, 3))
            {
                case 1:
                    sfxInstance = sfxExplosion1.CreateInstance();
                    sfxInstance.Play();
                    break;
                case 2:
                    sfxInstance = sfxExplosion2.CreateInstance();
                    sfxInstance.Play();
                    break;
                case 3:
                    sfxInstance = sfxExplosion3.CreateInstance();
                    sfxInstance.Play();
                    break;
            }
            
        }


        // Quick function to easily generate random numbers
        private int Randomize(int min, int max)
        {
            return random.Next(min, max + 1);
        }

        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // This game has a blue background. Why? Because!
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target,
                                               Color.Black, 0, 0);

            // Our player and enemy are both actually just text strings.
            //SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            //spriteBatch.Begin();

            //spriteBatch.DrawString(gameFont, "// TODO", playerPosition, Color.Green);

            //spriteBatch.DrawString(gameFont, "Insert Gameplay Here",
            //                       enemyPosition, Color.DarkRed);

            //spriteBatch.End();

            // SPACE BACKGROUND
            
            // Draw a 2D space background with stars
            //Simple background texture that only works well in 3D games with a fixed camera
            //spriteBatch.Begin();
            //spriteBatch.Draw(stars, new Rectangle(0, 0, GameConstants.resX, GameConstants.resY), Color.White);
            //spriteBatch.End();

            // Draw the Cylindrical skybox
            //sky.Draw(camera);

            //Draw DPSF Particles: SpaceTravel
            spaceTravelPS.SetWorldViewProjectionMatrices(Matrix.Identity, camera.View, camera.projectionMatrix);
            spaceTravelPS.Draw();

            Matrix shipTransformMatrix = ship.RotationMatrix * Matrix.CreateTranslation(ship.Position);

            // Draw all the remaining enemies on screen
            for (int d = 0; d < GameConstants.NumEnemyLayers; d++)
            {
                for (int i = 0; i < GameConstants.NumEnemyRows; i++)
                {
                    for (int j = 0; j < GameConstants.NumEnemyCols; j++)
                    {
                        if (enemyList[i, j, d].isActive)
                        {
                            Matrix enemyTransform = enemyList[i, j, d].RotationMatrix
                            * Matrix.CreateTranslation(enemyList[i, j, d].position);

                            // Pick which model to use for a specific enemy row
                            switch (enemyList[i, j, d].shipType)
                            {
                                case 1:
                                    DrawModel(enemyModel1, enemyTransform);
                                    break;
                                case 2:
                                    DrawModel(enemyModel2, enemyTransform);
                                    break;
                                case 3:
                                    DrawModel(enemyModel3, enemyTransform);
                                    break;
                            }
                        }
                    }
                }
            }

            // Draw all the active bullets on screen
            // Fridendly Bullets
            for (int i = 0; i < shipBulletList.Length; i++)
            {
                if (shipBulletList[i].isActive)
                {
                    Matrix bulletTransform =
                      Matrix.CreateTranslation(shipBulletList[i].position);
                    DrawModel(shipBulletModel, bulletTransform);
                }
            }
            // Enemy bullets
            for (int i = 0; i < enemyBulletList.Length; i++)
            {
                if (enemyBulletList[i].isActive)
                {
                    Matrix bulletTransform = EnemyBulletTransformMatrix *
                      Matrix.CreateTranslation(enemyBulletList[i].position);
                    DrawModel(enemyBulletModel, bulletTransform);
                }
            }

            // Draw the crosshair if we are in 1st person mode
            //if (camera.Mode == Camera.CameraMode.FirstPerson)
            //    DrawCrossHair();

            if (ship.isActive)
            {
                DrawModel(ship.Model, shipTransformMatrix);
            }

            //Draw all the 2D stuff now
            spriteBatch.Begin();

#if (HUD)
            DrawOverlayText();
#endif

            if (!ship.isActive)
                DrawGameOver();

            spriteBatch.End();


            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0 || pauseAlpha > 0)
            {
                float alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, pauseAlpha / 2);

                ScreenManager.FadeBackBufferToBlack(alpha);
            }
        }

        void DrawModel(Model model, Matrix modelTransform)
        {
            //Matrix[] transforms = new Matrix[model.Bones.Count];
            //model.CopyAbsoluteBoneTransformsTo(transforms);
            model.Draw(modelTransform, camera.View, camera.projectionMatrix);

            //Draw the model, a model can have multiple meshes, so loop
            //foreach (ModelMesh mesh in model.Meshes)
            //{
            //    //This is where the mesh orientation is set
            //    foreach (BasicEffect effect in mesh.Effects)
            //    {
            //        effect.EnableDefaultLighting();
            //        effect.PreferPerPixelLighting = true;
            //        effect.World = transforms[mesh.ParentBone.Index] * modelTransform;
            //        effect.View = camera.View;
            //        effect.Projection = camera.projectionMatrix;
            //    }
            //    //Draw the mesh, will use the effects set above.
            //    mesh.Draw();
            //}
        }

#if (HUD)
        // Displays an overlay showing the current ship health
        private void DrawOverlayText()
        {
            string text = "HULL: " + ship.currentHullIntegrity.ToString();
            Vector2 stringCenter = fontUI.MeasureString(text);
            // Draw the string twice to create a drop shadow, first colored black
            // and offset one pixel to the bottom right, then again in white at the
            // intended position. This makes text easier to read over the background.
            spriteBatch.DrawString(fontUI, text, new Vector2(21, GameConstants.resY - 41), Color.White);
            spriteBatch.DrawString(fontUI, text, new Vector2(20, GameConstants.resY - 40), Color.Green);

            text = "Level: " + currentLevel.ToString();
            stringCenter = fontUI.MeasureString(text);
            spriteBatch.DrawString(fontUI, text, new Vector2(GameConstants.resX - stringCenter.X - 21, GameConstants.resY - 41), Color.White);
            spriteBatch.DrawString(fontUI, text, new Vector2(GameConstants.resX - stringCenter.X - 20, GameConstants.resY - 40), Color.Green);
        }
#endif

        // Displays the Game Over message
        private void DrawGameOver()
        {
            string text = "GAME OVER";
            // We want to draw the text centered on the screen, so we'll  
            // calculate the center of the string, and use that as the origin  
            // argument to spriteBatch.DrawString. DrawString automatically  
            // centers text around the vector specified by the origin argument.  
            Vector2 stringCenter = fontGameOver.MeasureString(text) * 0.5f;

            spriteBatch.DrawString(fontGameOver, text, new Vector2((int)((GameConstants.resX / 2) - stringCenter.X),
                (int)((GameConstants.resY / 2) - stringCenter.Y)), Color.Red);
        }

        //private void DrawCrossHair()
        //{
        //    int screenWidth = graphics.GraphicsDevice.Viewport.Width;
        //    int screenHeight = graphics.GraphicsDevice.Viewport.Height;

        //    // draw the sun in the center
        //    Vector2 position = new Vector2(screenWidth / 2, (screenHeight / 2) + 25);

        //    // the sun is made from 4 lines in a circle.
        //    primitiveBatch.Begin(PrimitiveType.LineList);

        //    // draw the vertical and horizontal lines
        //    primitiveBatch.AddVertex(position + new Vector2(0, 30), Color.Red);
        //    primitiveBatch.AddVertex(position + new Vector2(0, -30), Color.Red);

        //    primitiveBatch.AddVertex(position + new Vector2(30, 0), Color.Red);
        //    primitiveBatch.AddVertex(position + new Vector2(-30, 0), Color.Red);

        //    primitiveBatch.End();
        //}

        private Vector3 ScreenProject(Vector3 point, Viewport wholeViewport, Camera camera, Matrix world)
        {
            Vector4 mp = Vector4.Transform(new Vector4(point, 1.0f), Matrix.Invert(world));
            Vector3 pt = wholeViewport.Project(new Vector3(mp.X, mp.Y, mp.Z), camera.projectionMatrix, camera.View, world);
            return pt;
        }

        #endregion
    }
}
