using System.IO;
using Pixel3D.Serialization;

namespace Pixel3D.Engine.LoopSystem
{
    public static class ByteArrayExtensions
    {
        public static void WriteLoopWithComment(this BinaryWriter loopWriter, byte[] saveState, Value128 definitionHash, string comment)
        {
            loopWriter.Write((byte)'l');
            loopWriter.Write((byte)'o');
            loopWriter.Write((byte)'o');
            loopWriter.Write((byte)'p');
            loopWriter.Write((byte)' ');
            loopWriter.Write(System.Text.Encoding.ASCII.GetBytes(comment));
            loopWriter.Write((byte)' ');
            loopWriter.Write((byte)0); // <- nul terminated string
            loopWriter.Write(definitionHash.v1);
            loopWriter.Write(definitionHash.v2);
            loopWriter.Write(definitionHash.v3);
            loopWriter.Write(definitionHash.v4);
            loopWriter.Write(saveState.Length);
            loopWriter.Write(saveState);
        }
    }
}