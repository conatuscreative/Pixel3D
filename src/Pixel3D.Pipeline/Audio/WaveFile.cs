using System;
using System.Diagnostics;
using System.IO;

namespace Pixel3D.Pipeline
{
    // REFERENCE: 
    //  https://web.archive.org/web/20141213140451/https://ccrma.stanford.edu/courses/422/projects/WaveFormat/
    //  https://en.wikipedia.org/wiki/WAV
    //  http://www-mmsp.ece.mcgill.ca/documents/audioformats/wave/Docs/riffmci.pdf

    public struct WaveFile
    {
        public int sampleRate;
        public int channelCount;
        public int bitsPerSample;
        public int sampleCount;

        // PERF: If I was being really pedantic about performance, I might make this a stream processing thing, rather than doing all this buffering
        /// <summary>Sample data for each channel, concatenated together (so we can pin it all at once to memcpy to libvorbis)</summary>
        public float[] sampleData;

        /// <summary>Read a "simple" wave file</summary>
        public static WaveFile Read(BinaryReader br)
        {
            const uint riffMagic = 0x46464952u; // "RIFF"
            const uint waveMagic = 0x45564157u; // "WAVE"
            const uint fmt_Magic = 0x20746d66u; // "fmt "
            const uint dataMagic = 0x61746164u; // "data"
			
            // RIFF Header:
            uint fileId = br.ReadUInt32();
            if(fileId != riffMagic)
                throw new Exception("Not a RIFF file");

            int fileSize = br.ReadInt32(); // <- does not include fileId and fileFormat fields

            uint fileFormat = br.ReadUInt32();
            if(fileFormat != waveMagic)
                throw new Exception("Not a WAVE file");

            WaveFile result = new WaveFile();

            bool hasFormatChunk = false;
            bool hasDataChunk = false;
            while(!(hasFormatChunk && hasDataChunk))
            {
                uint chunkId = br.ReadUInt32();
                int chunkSize = br.ReadInt32();
                int alignedChunkSize = chunkSize + (chunkSize&1); // <- Chunks are word-aligned (round up to nearest word)

                switch(chunkId)
                {
                    case fmt_Magic:
                        {
                            if(hasFormatChunk)
                                throw new Exception("File has two format chunks");

                            short audioFormat = br.ReadInt16();
                            if(audioFormat != 1) // <- PCM format
                                throw new Exception("Cannot handle wave format " + audioFormat + " (can only read PCM)");

                            result.channelCount = br.ReadInt16();
                            if(result.channelCount != 1 && result.channelCount != 2)
                                throw new Exception("Refusing file with too many channels, because XNA won't like it"); // we can handle it, XNA can't

                            result.sampleRate = br.ReadInt32();
                            if(result.sampleRate < 8000 || result.sampleRate > 48000)
                                throw new Exception("Refusing file with unusual sample rate, because XNA won't like it"); // we can handle it, XNA can't

                            int byteRate = br.ReadInt32();
                            short blockAlign = br.ReadInt16();

                            result.bitsPerSample = br.ReadInt16();
                            if(result.bitsPerSample != 16 && result.bitsPerSample != 8)
                                throw new Exception("Cannot currently handle audio that is not 8-bit or 16-bit");

                            // If we really care:
                            Debug.Assert(byteRate == result.sampleRate * result.channelCount * result.bitsPerSample/8);
                            Debug.Assert(blockAlign == result.channelCount * result.bitsPerSample/8);
							
                            // For some weird reason, some format chunks have extra data after the PCM header
                            if(alignedChunkSize != 16)
                                br.BaseStream.Seek(alignedChunkSize - 16, SeekOrigin.Current);

                            hasFormatChunk = true;
                        }
                        break;


                    case dataMagic:
                        {
                            if(hasDataChunk)
                                throw new Exception("File has two data chunks");
                            if(!hasFormatChunk)
                                throw new Exception("File has data chunk before format chunk");

                            long debugStart = br.BaseStream.Position;

                            int sampleSize = result.bitsPerSample / 8;
                            Debug.Assert(sampleSize == 1 || sampleSize == 2); // <- checked when we read the format chunk

                            int blockSize = result.channelCount * sampleSize;
                            result.sampleCount = chunkSize / blockSize;

                            result.sampleData = new float[result.sampleCount * result.channelCount];

                            for(int s = 0; s < result.sampleCount; s++)
                            {
                                for(int c = 0; c < result.channelCount; c++)
                                {
                                    float sample;
                                    if(sampleSize == 1)
                                        sample = (br.ReadByte()-128) / 128f;
                                    else
                                        sample = br.ReadInt16() / 32768f;

                                    result.sampleData[s + c*result.sampleCount] = sample;
                                }
                            }

                            long debugEnd = br.BaseStream.Position;
                            if((debugEnd - debugStart) != chunkSize)
                                throw new Exception("Audio file corruption: Bad data chunk length");


                            // Seek past any padding...
                            if(alignedChunkSize != chunkSize)
                                br.BaseStream.Seek(alignedChunkSize - chunkSize, SeekOrigin.Current);

                            hasDataChunk = true;
                        }
                        break;


                    default:
                        br.BaseStream.Seek(alignedChunkSize, SeekOrigin.Current);
                        break;
                }
            }

            return result;
        }
		
		public void MakeMono()
		{
			if(channelCount != 2)
				return;

			channelCount = 1;
		}
    }
}
