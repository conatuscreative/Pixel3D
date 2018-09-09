using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using Pixel3D.Audio;

namespace Pixel3D.Engine
{
    // Having to use vorbisfile instead of libvorbis directly makes me sad. -AR
    // (See conversations between myself and Ethan about why struct layout cross-platform is annoying,
    //  and at least FNA has been tested.)

    public static class VorbisfileDecoder
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false), SuppressUnmanagedCodeSecurity]
        private static extern unsafe void* memcpy(void* dest, void* src, UIntPtr byteCount);

        private unsafe struct FakeFile
        {
            public byte* start;
            public byte* position;
            public byte* end;
        }

        private static unsafe IntPtr BufferReadFunc(IntPtr ptr, IntPtr size, IntPtr elements, IntPtr datasource)
        {
            FakeFile* file = (FakeFile*)datasource;

            long s = size.ToInt64();
            long e = elements.ToInt64();

            long bytesToRead = e * s;
            long remainingBytes = file->end - file->position;
            if(bytesToRead > remainingBytes)
                bytesToRead = remainingBytes;

            e = bytesToRead / s;
            bytesToRead = e * s;

            Debug.Assert(bytesToRead >= 0);

            memcpy(ptr.ToPointer(), file->position, (UIntPtr)bytesToRead);
            file->position += bytesToRead;

            return (IntPtr)(e);
        }
		
        private static readonly Vorbisfile.ov_callbacks StaticCallbacks = new Vorbisfile.ov_callbacks
        {
            read_func = BufferReadFunc,
            seek_func = null,
            close_func = null,
            tell_func = null,
        };

        public static unsafe SoundEffect Decode(byte* start, byte* end)
        {
            FakeFile file = new FakeFile { start = start, position = start, end = end };

            int sampleCount = *(int*)file.position; // <- We encoded this, before the packets start, because Vorbis doesn't know [not sure if this is still true for Ogg, haven't checked -AR]
            file.position += 4;
            int loopStart = *(int*)file.position;
            file.position += 4;
            int loopLength = *(int*)file.position;
            file.position += 4;

            // TODO: Consider modifying vorbisfile binding so we can stackalloc `vf`
            IntPtr vf;
            Vorbisfile.ov_open_callbacks((IntPtr)(&file), out vf, IntPtr.Zero, IntPtr.Zero, StaticCallbacks);

            Vorbisfile.vorbis_info info = Vorbisfile.ov_info(vf, 0);

            byte[] audioData = new byte[sampleCount * info.channels * 2]; // *2 for 16-bit audio (as required by XNA)

            fixed(byte* writeStart = audioData)
            {
                byte* writePosition = writeStart;
                byte* writeEnd = writePosition + audioData.Length;

                while(true)
                {
                    int currentSection;
                    int result = (int)Vorbisfile.ov_read(vf, (IntPtr)writePosition, (int)(writeEnd - writePosition), 0, 2, 1, out currentSection);

                    if(result == 0) // End of file
                        break;
                    else if(result > 0)
                        writePosition += result;
                    if(writePosition >= writeEnd)
                        break;
                }

                Debug.Assert(writePosition == writeEnd); // <- If this fires, something went terribly wrong. (TODO: Throw exception?)
            }

            Vorbisfile.ov_clear(ref vf);

            return new SoundEffect(audioData, 0, audioData.Length, (int)info.rate, (AudioChannels)info.channels, loopStart, loopLength);
        }

	    // This is split out so that it can run late in the loading process (because it saturates the CPU)
	    public static unsafe void DecodeVorbisData(ReadAudioPackage.Result input)
	    {
		    if (!AudioDevice.Available)
			    return; // <- No sound!

		    // IMPORTANT: This is lock-free, because each entry only writes to its own slot (everything else is read-only)
		    fixed (byte* data = input.audioPackageBytes)
		    {
			    byte* vorbisStart = data + input.vorbisOffset;

			    int count = input.sounds.Length;
			    //for (int i = 0; i < count; i++)
			    Parallel.ForEach(Enumerable.Range(0, count), i =>
			    {
				    input.sounds[i].owner = VorbisfileDecoder.Decode(vorbisStart + input.offsets[i], vorbisStart + input.offsets[i + 1]);
			    });
		    }
	    }
	}
}
