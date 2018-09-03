using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Pixel3D.LoopRecorder
{
    public sealed class Loop : IDisposable
    {
        const int maxLoopInputCount = 60 * 60 * 60; // 1 hour == ~3MB (should be enough for anyone)

		public Value128 definitionHash;
        public string comment;
        public string filename;

        public byte[] saveState;
        public bool IsValid { get { return saveState != null; } }

        public MultiInputState[] inputFrames;
        public int frameCount;


        public BinaryWriter loopWriter;
        public bool IsRecording { get { return loopWriter != null; } }

		public void RecordInput(MultiInputState input)
        {
            Debug.Assert(IsRecording);

            // NOTE: this is _assumed_ to be compatible with how we load loops (by direct memory copy)
            loopWriter.Write((uint)input.Player1);
            loopWriter.Write((uint)input.Player2);
            loopWriter.Write((uint)input.Player3);
            loopWriter.Write((uint)input.Player4);

            if(inputFrames.Length == frameCount)
                Array.Resize(ref inputFrames, inputFrames.Length * 2);
            inputFrames[frameCount++] = input;

            if(frameCount >= maxLoopInputCount)
                StopRecording();
        }

        public void StopRecording()
        {
            if(loopWriter != null)
                loopWriter.Close();
            loopWriter = null;
        }

        public void Dispose()
        {
            StopRecording();
        }


		public static Loop StartRecording(string filename, byte[] saveState, Value128 definitionHash, string comment)
        {
            Loop result = new Loop();

            result.filename = filename;
            result.definitionHash = definitionHash;
            result.comment = comment;
            result.saveState = saveState;
            result.frameCount = 0;
            result.inputFrames = new MultiInputState[2*60*60];

            result.loopWriter = new BinaryWriter(File.Create(filename));
            result.loopWriter.WriteLoopWithComment(saveState, definitionHash, comment);

            return result;
        }

		public static Loop TryLoadFromFile(string filename, ref Value128 definitionHash)
        {
            try
            {
                using(var fileStream = File.OpenRead(filename))
                {
                    return TryLoadFromStream(fileStream, filename, ref definitionHash);
                }
            }
            catch(Exception e)
            {
                Trace.WriteLine($"Loop \"{Path.GetFileNameWithoutExtension(filename)}\": Failed to open file ({e.Message})");
                return null;
            }
        }
		
        public static Loop TryLoadFromStream(Stream stream, string filename, ref Value128 definitionHash)
        {
            Loop result = new Loop();
            result.filename = filename;

            try
            {
                BinaryReader br = new BinaryReader(stream);

                // Read file identifier:
                if(br.ReadByte() != (byte)'l' || br.ReadByte() != (byte)'o' || br.ReadByte() != (byte)'o' || br.ReadByte() != (byte)'p' || br.ReadByte() != (byte)' ')
                    throw new Exception("File missing identifier!");

                // Read comment (typically a git hash):
                List<byte> commentBytes = new List<byte>();
                byte readByte;
                while((readByte = br.ReadByte()) != 0) // Wait for null terminator
                    commentBytes.Add(readByte);
                if(commentBytes.Count > 0 && commentBytes[commentBytes.Count-1] == (byte)' ') // Remove trailing space from comment
                    commentBytes.RemoveAt(commentBytes.Count-1);
                result.comment = System.Text.Encoding.ASCII.GetString(commentBytes.ToArray());

                result.definitionHash.v1 = br.ReadUInt32();
                result.definitionHash.v2 = br.ReadUInt32();
                result.definitionHash.v3 = br.ReadUInt32();
                result.definitionHash.v4 = br.ReadUInt32();

                if(result.definitionHash == definitionHash) // <- Only bother to load the loop if the definition hash matches
                {
                    int saveStateLength = br.ReadInt32();
                    result.saveState = br.ReadBytes(saveStateLength);

                    long remainingBytes = (int)(br.BaseStream.Length - br.BaseStream.Position);
                    unsafe
                    {
                        if((ulong)remainingBytes > (ulong)(maxLoopInputCount * sizeof(MultiInputState)))
                            throw new Exception("File too huge!"); // (or impossible negative size)
                        if(remainingBytes % sizeof(MultiInputState) != 0) // <- file didn't finish writing out, do we actually care?
                            throw new Exception("Bad file!");

                        result.frameCount = ((int)remainingBytes) / sizeof(MultiInputState);
                        result.inputFrames = new MultiInputState[result.frameCount + 2048];

                        byte[] workingBuffer = new byte[remainingBytes];
                        br.Read(workingBuffer, 0, (int)remainingBytes);

                        fixed(MultiInputState* inputFrames = &(result.inputFrames[0]))
                        {
                            Marshal.Copy(workingBuffer, 0, (IntPtr)inputFrames, (int)remainingBytes);
                        }
                    }
                }

	            Trace.WriteLine($"Loop \"{Path.GetFileNameWithoutExtension(filename)}\": {(result.saveState == null ? "[INVALID]" : "[OK]")} comment = \"{result.comment}\" definitions = \"{result.definitionHash}\"");

            }
            catch(Exception e)
            {
                Debug.WriteLine($"Loop \"{Path.GetFileNameWithoutExtension(filename)}\": Corrupt file! ({e.Message})");
                return null;
            }

            return result;
        }

    }
}
