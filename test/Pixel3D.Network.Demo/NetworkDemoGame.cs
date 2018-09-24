using System;
using System.IO;
using Common.GlobalInput;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Pixel3D.Network.Common;
using Pixel3D.Network.Rollback;
using Pixel3D.P2P;

namespace Pixel3D.Network.Demo
{
    public class NetworkDemoGame : Game
    {
        GraphicsDeviceManager graphics;

        public string commandLineHost;


        public NetworkDemoGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = SquarePlayground.worldWidth;
            graphics.PreferredBackBufferHeight = SquarePlayground.worldHeight;

            Content.RootDirectory = "Content";

            IsMouseVisible = true;

            TargetElapsedTime = RollbackDriver.FrameTime;
        }



        SimpleNetworkMenu simpleNetworkMenu;
        SimpleConsole console = new SimpleConsole();


        // The game state:
        SquarePlayground squarePlayground;


        protected override void Initialize()
        {
            base.Initialize();

            simpleNetworkMenu = new SimpleNetworkMenu(commandLineHost, console, CreateNetwork, CreateRollbackDriverAndGame);

            // Create initial game
            squarePlayground = new SquarePlayground();
            for(int i = 0; i < 4; i++)
                squarePlayground.PlayerJoin(i, "Local " + i, null, true);
        }


        protected override void OnExiting(object sender, EventArgs args)
        {
            if(network != null)
                network.Shutdown().Wait(1500);
            base.OnExiting(sender, args);
        }



        #region Content

        SpriteBatch sb;
        DisplayText dt;

        public static Texture2D WhitePixel { get; private set; }
        public static SpriteFont DefaultFont { get; private set; }


        protected override void LoadContent()
        {
            base.LoadContent();

            DefaultFont = Content.Load<SpriteFont>("LargeText");

            sb = new SpriteBatch(GraphicsDevice);
            dt = new DisplayText(GraphicsDevice, DefaultFont, Color.White * 0.8f);

            WhitePixel = new Texture2D(GraphicsDevice, 1, 1);
            WhitePixel.SetData(new[] { Color.White });
        }


        protected override void UnloadContent()
        {
            sb.Dispose();
            dt.Dispose();

            WhitePixel.Dispose();

            base.UnloadContent();
        }

        #endregion



        #region Network and Rollback Driver

        P2PNetwork network;
        RollbackDriver rollbackDriver;

        P2PNetwork CreateNetwork(BadNetworkSimulation badNetworkSimulation)
        {
            var appConfig = new NetworkAppConfig("Rollback Test", new[] { 11101, 11102, 11103, 11104, 11105, 11106 }, 0, null);

            network = new P2PNetwork(appConfig, new SimpleConsoleWriter(console), badNetworkSimulation);
            return network;
        }

        RollbackDriver CreateRollbackDriverAndGame()
        {
            rollbackDriver = new RollbackDriver(squarePlayground = new SquarePlayground(), null, network, (int)PlayerButton.Count);
            return rollbackDriver;
        }

        #endregion



        #region Update

        Point? startPosition;

        void UpdateStartPosition()
        {
            if(IsActive && GraphicsDevice.Viewport.Bounds.Contains(Input.MousePosition))
            {
                if(Input.LeftMouseWentDown)
                {
                    startPosition = Input.MousePosition;

                    MemoryStream ms = new MemoryStream();
                    BinaryWriter bw = new BinaryWriter(ms);
                    bw.Write(Input.MousePosition.X);
                    bw.Write(Input.MousePosition.Y);
                    simpleNetworkMenu.localPlayerData = ms.ToArray();
                }
                else if(Input.RightMouseWentDown)
                {
                    startPosition = null;
                    simpleNetworkMenu.localPlayerData = null;
                }                
            }
        }



        protected override void Update(GameTime gameTime)
        {
            Input.Update(IsActive);


            if(Input.KeyWentDown(Keys.Escape))
                Exit(); // Make it easy to stop the game while testing


            simpleNetworkMenu.Update();
            simpleNetworkMenu.UpdateRollbackDriverHotkeys();

            UpdateStartPosition();

            // Display stuff:
            if(Input.KeyWentDown(Keys.F12))
                showRollbackDebugInfo = !showRollbackDebugInfo;


            if(network != null)
            {
                network.Update();
            }

            if(rollbackDriver != null) // Networked mode
            {
                rollbackDriver.Update(gameTime.ElapsedGameTime, InputMapping.GetPlayerInputSample());
            }
            else
            {
                squarePlayground.Update(InputMapping.GetPlayerInputSample(), true);
            }


            console.Update(gameTime.ElapsedGameTime.TotalSeconds);
        }

        #endregion



        #region Draw

        bool showRollbackDebugInfo = false;

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            squarePlayground.Draw(sb);

            if(startPosition.HasValue)
            {
                sb.Begin();
                sb.Draw(WhitePixel, new Vector2(startPosition.Value.X - 2, startPosition.Value.Y - 2), null, Color.Gray, 0, Vector2.Zero, 5, 0, 0);
                sb.Draw(WhitePixel, new Vector2(startPosition.Value.X - 1, startPosition.Value.Y - 1), null, Color.White, 0, Vector2.Zero, 3, 0, 0);
                sb.End();
            }

            bool noFade = Input.Shift && Input.Control;
            console.Draw(sb, DefaultFont, WhitePixel, noFade);

            simpleNetworkMenu.Draw(dt);

            if(showRollbackDebugInfo)
                RollbackDebugDisplay.Draw(dt, rollbackDriver);

            base.Draw(gameTime);
        }

        #endregion


    }
}
