using System.IO;

namespace Pixel3D.LoopRecorder
{
	public delegate void Serialize<TGameState>(BinaryWriter bw, ref TGameState gameState, object userData);

	public delegate void Deserialize<TGameState>(BinaryReader br, ref TGameState gameState, object userData);
}