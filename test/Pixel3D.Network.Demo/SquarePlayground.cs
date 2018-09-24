using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Network.Rollback;

namespace Pixel3D.Network.Demo
{
    class SquarePlayground : IGameState
    {
        public const int worldWidth = 600;
        public const int worldHeight = 600;

        const int playerCornerOffset = 100;


        const int playerCount = 4; // should match number of available input assignments

        readonly Player[] players = new Player[playerCount];
        int frame = 0;


        Point GetPlayerStart(int playerIndex, byte[] playerData)
        {
            if(playerData != null)
            {
                MemoryStream ms = new MemoryStream(playerData);
                BinaryReader br = new BinaryReader(ms);
                return new Point(br.ReadInt32(), br.ReadInt32());
            }

            switch(playerIndex)
            {
                case 0: return new Point(playerCornerOffset, playerCornerOffset);
                case 1: return new Point(worldWidth-playerCornerOffset, playerCornerOffset);
                case 2: return new Point(playerCornerOffset, worldHeight-playerCornerOffset);
                case 3: return new Point(worldWidth-playerCornerOffset, worldHeight-playerCornerOffset);
                default: return new Point(worldWidth/2, worldHeight/2);
            }
        }

        Color GetPlayerColor(int playerIndex)
        {
            switch(playerIndex)
            {
                case 0: return Color.Red;
                case 1: return Color.Blue;
                case 2: return Color.Green;
                case 3: return Color.Gold;
                default: return Color.Black;
            }
        }

        public void PlayerJoin(int playerIndex, string playerName, byte[] playerData, bool firstTimeSimulated)
        {
            Debug.Assert(players[playerIndex] == null);
            players[playerIndex] = new Player(GetPlayerColor(playerIndex), GetPlayerStart(playerIndex, playerData), playerName);
        }

        public void PlayerLeave(int playerIndex, bool firstTimeSimulated)
        {
            Debug.Assert(players[playerIndex] != null);
            players[playerIndex] = null;
        }

        public void Update(MultiInputState input, bool firstTimeSimulated)
        {
            if(firstTimeSimulated)
                UpdateSmoothing();

            frame++;

            for(int i = 0; i < players.Length; i++)
            {
                if(players[i] != null)
                    players[i].Update(new PlayerInput(input[i]), firstTimeSimulated);
            }
        }


        public void Draw(SpriteBatch sb)
        {
            sb.Begin();
            for(int i = 0; i < players.Length; i++)
            {
                if(players[i] != null)
                    players[i].Draw(sb, smoothing[i]);
            }
            sb.End();
        }




        #region Serialization

        public byte[] Serialize()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(frame);

            for(int i = 0; i < players.Length; i++)
            {
                if(bw.WriteBoolean(players[i] != null))
                {
                    players[i].Serialize(bw);
                }
            }

            return ms.ToArray();
        }

        public void Deserialize(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            BinaryReader br = new BinaryReader(ms);

            frame = br.ReadInt32();

            for(int i = 0; i < players.Length; i++)
            {
                if(br.ReadBoolean())
                {
                    players[i] = new Player(br);
                }
                else
                {
                    players[i] = null;
                }
            }
        }

        #endregion



        #region Smoothing

        // Not the greatest way to capture smoothing data
        // (doing this because deserialize replaces world data, so smoothing must be stored externally)
        Vector2[] smoothing = new Vector2[playerCount];
        Point?[] savedPositions = new Point?[playerCount];

        /// <summary>Capture smoothing state</summary>
        void IGameState.BeforePrediction()
        {
            for(int i = 0; i < players.Length; i++)
            {
                if(players[i] == null)
                {
                    smoothing[i] = Vector2.Zero;
                    savedPositions[i] = null;
                }
                else
                {
                    savedPositions[i] = players[i].Position;
                }
            }
        }

        void IGameState.AfterPrediction()
        {
            for(int i = 0; i < players.Length; i++)
            {
                if(players[i] == null || !savedPositions[i].HasValue)
                {
                    smoothing[i] = Vector2.Zero;
                }
                else
                {
                    smoothing[i] += new Vector2(
                            savedPositions[i].Value.X - players[i].Position.X,
                            savedPositions[i].Value.Y - players[i].Position.Y);
                }
            }
        }

        void UpdateSmoothing()
        {
            for(int i = 0; i < smoothing.Length; i++)
            {
                smoothing[i] = smoothing[i] * 0.95f;
            }
        }

        #endregion




        public void WriteDiscoveryData(BinaryWriter bw)
        {
            // Nothing interesting...
        }

        public void BeforeRollbackAwareFrame(int frame, bool startupPrediction)
        {
            // Nothing to do...
        }

        public void AfterRollbackAwareFrame()
        {
            // Nothing to do...
        }

        public void RollbackDriverDetach()
        {
            // Nothing to do...
        }

    }
}
