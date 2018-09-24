using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Network.Rollback;

namespace Pixel3D.Network.Demo
{
    class Player
    {
        public Player(Color color, Point startPosition, string name)
        {
            this.Color = color;
            this.Position = startPosition;
            this.Name = name;
        }


        public Color Color { get; private set; }
        public Point Position { get; private set; }
        public string Name { get; private set; }

        #region Serialization

        public Player(BinaryReader br)
        {
            this.Color = br.ReadColor();
            this.Position = br.ReadPoint();
            this.Name = br.ReadNullableString();
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(Color);
            bw.Write(Position);
            bw.WriteNullableString(Name);
        }

        #endregion



        public void Update(PlayerInput input, bool firstTimeSimulated)
        {
            Point p = Position;

            int speed = 120 / RollbackDriver.FramesPerSecond;

            // Move
            if(input.IsDown(PlayerButton.Up))
                p.Y -= speed;
            if(input.IsDown(PlayerButton.Down))
                p.Y += speed;
            if(input.IsDown(PlayerButton.Left))
                p.X -= speed;
            if(input.IsDown(PlayerButton.Right))
                p.X += speed;

            // Constrain to world boundary
            if(p.X < 0)
                p.X = 0;
            if(p.X > SquarePlayground.worldWidth)
                p.X = SquarePlayground.worldWidth;
            if(p.Y < 0)
                p.Y = 0;
            if(p.Y > SquarePlayground.worldHeight)
                p.Y = SquarePlayground.worldHeight;

            Position = p;
        }


        public void Draw(SpriteBatch sb, Vector2 smoothing)
        {
            const int size = 30;
            Point p = new Point(Position.X + (int)Math.Round(smoothing.X), Position.Y + (int)Math.Round(smoothing.Y)); // <- pixel snapped smoothing
            sb.Draw(NetworkDemoGame.WhitePixel, new Rectangle(p.X - size/2, p.Y - size/2, size, size), Color);
            sb.DrawString(NetworkDemoGame.DefaultFont, Name, new Vector2(p.X - size/2, p.Y + size/2), Color);

            // Without smoothing, for demonstration
            sb.Draw(NetworkDemoGame.WhitePixel, new Rectangle(Position.X - size/2, Position.Y - size/2, size, size), Color * 0.1f);
        }


    }
}
