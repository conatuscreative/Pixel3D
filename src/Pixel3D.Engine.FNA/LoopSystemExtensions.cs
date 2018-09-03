using Pixel3D.Network.Rollback.Input;
using Pixel3D.Serialization;
using InputState = Pixel3D.LoopRecorder.InputState;

namespace Pixel3D.Engine.FNA
{
	public static class LoopSystemExtensions
	{
		public static Value128 AsSerializeValue(this Pixel3D.LoopRecorder.Value128 value)
		{
			return new Value128 {v1 = value.v1, v2 = value.v2, v3 = value.v3, v4 = value.v4};
		}

		public static Pixel3D.LoopRecorder.Value128 AsLoopValue(this Value128 value)
		{
			return new Pixel3D.LoopRecorder.Value128 { v1 = value.v1, v2 = value.v2, v3 = value.v3, v4 = value.v4 };
		}

		public static MultiInputState AsNetworkValue(this Pixel3D.LoopRecorder.MultiInputState value)
		{
			return new MultiInputState
			{
				Player1 = (Network.Rollback.Input.InputState)value.Player1,
				Player2 = (Network.Rollback.Input.InputState)value.Player2,
				Player3 = (Network.Rollback.Input.InputState)value.Player3,
				Player4 = (Network.Rollback.Input.InputState)value.Player4
			};
		}

		public static LoopRecorder.MultiInputState AsLoopValue(this MultiInputState value)
		{
			return new LoopRecorder.MultiInputState
			{
				Player1 = (InputState) value.Player1,
				Player2 = (InputState) value.Player2,
				Player3 = (InputState) value.Player3,
				Player4 = (InputState) value.Player4
			};
		}
	}
}
