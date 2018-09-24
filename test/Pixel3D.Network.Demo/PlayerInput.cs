using Common.GlobalInput;
using Microsoft.Xna.Framework.Input;

namespace Pixel3D.Network.Demo
{
    enum PlayerButton
    {
        Left,
        Right,
        Up,
        Down,

        Count,
    }


    // NOTE: This is a slimmed down version of this struct, which does not support "went up" and "went down"
    struct PlayerInput
    {
        public PlayerInput(InputState current)
        {
            this.current = current;
        }

        public InputState current;

        public bool IsDown(PlayerButton button)
        {
            return (current & (InputState)(1u << (int)button)) != 0;
        }

        public bool IsUp(PlayerButton button)
        {
            return !IsDown(button);
        }
    }



    static class InputMapping
    {
        public static readonly Keys[][] keyboardMap = 
        {
            new[] { Keys.A, Keys.D, Keys.W, Keys.S },
            new[] { Keys.J, Keys.L, Keys.I, Keys.K },
            new[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down },
            new[] { Keys.NumPad4, Keys.NumPad6, Keys.NumPad8, Keys.NumPad5 },
        };

        public static readonly Buttons[] gamePadMap = { Buttons.DPadLeft, Buttons.DPadRight, Buttons.DPadUp, Buttons.DPadDown };
        public static readonly Buttons[] gamePadMapAlt = { Buttons.LeftThumbstickLeft, Buttons.LeftThumbstickRight, Buttons.LeftThumbstickUp, Buttons.LeftThumbstickDown };


        public static MultiInputState GetPlayerInputSample()
        {
            MultiInputState output = new MultiInputState();

            for(int i = 0; i < 4; i++)
            {
                if(Input.IsActive)
                {
                    output[i] |= Input.KeyboardState.MapInputs(keyboardMap[i]);
                    output[i] |= Input.GamePadState(i).MapInputs(gamePadMap);
                    output[i] |= Input.GamePadState(i).MapInputs(gamePadMapAlt);
                }
            }

            return output;
        }

    }
}
