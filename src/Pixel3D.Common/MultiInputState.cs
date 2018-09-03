using System;
using System.Diagnostics;

namespace Pixel3D.Network.Rollback.Input
{
    [Serializable]
    public struct MultiInputState
    {
        public InputState Player1;
        public InputState Player2;
        public InputState Player3;
        public InputState Player4;

        public const int Count = 4;
        
        public InputState this[int playerNumber]
        {
            get
            {
                switch(playerNumber)
                {
                    case 0: return Player1;
                    case 1: return Player2;
                    case 2: return Player3;
                    case 3: return Player4;
                    default:
                        Debug.Assert(false); // Not a valid player number
                        return default(InputState);
                }
            }
            set
            {
                switch(playerNumber)
                {
                    case 0: Player1 = value; break;
                    case 1: Player2 = value; break;
                    case 2: Player3 = value; break;
                    case 3: Player4 = value; break;
                    default:
                        Debug.Assert(false); // Not a valid player number
                        break;
                }
            }
        }
	}
}
