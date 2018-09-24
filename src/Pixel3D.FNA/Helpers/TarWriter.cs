using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Pixel3D.Helpers
{
    public class TarWriter : IDisposable
    {
        Stream stream;
        bool leaveOpen;

        public TarWriter(Stream stream, bool leaveOpen)
        {
            if(stream == null)
                throw new ArgumentNullException("stream");
            this.stream = stream;
            this.leaveOpen = leaveOpen;
        }

        public void Dispose()
        {
            CheckDisposed();
            Close();
        }

        private void CheckDisposed()
        {
            if(stream == null)
                throw new ObjectDisposedException(GetType().FullName);
        }

        public void Close()
        {
            // Write trailer:
            stream.Write(zeroBuffer, 0, 512);
            bytesWritten += 512;
            stream.Write(zeroBuffer, 0, 512);
            bytesWritten += 512;

            if(!leaveOpen)
            {
                stream.Close();
            }
            stream = null;
        }




        public void AddFile(string filename, MemoryStream data)
        {
            CheckDisposed();

            WriteHeader(filename, data.Length);
            data.Position = 0;
            data.CopyTo(stream);
            bytesWritten += data.Length;
            PadBlock();
        }

        /// <param name="text">Text to write into the file (can be null)</param>
        public void AddFile(string filename, string text)
        {
            CheckDisposed();

            byte[] data = Encoding.UTF8.GetBytes(text ?? string.Empty);
            AddFile(filename, data);
        }

        public void AddFile(string filename, byte[] data)
        {
            AddFile(filename, data, 0, data.Length);
        }

        public void AddFile(string filename, byte[] data, int start, int count)
        {
            CheckDisposed();
            if(start + count > data.Length)
                throw new ArgumentOutOfRangeException("count");

            WriteHeader(filename, count);
            stream.Write(data, start, count);
            bytesWritten += count;
            PadBlock();
        }


        /// <summary>IMPORTANT: Should always be zeroed after use!</summary>
        private byte[] zeroBuffer = new byte[512];

        private void WriteHeader(string filename, long size)
        {
            // http://www.fileformat.info/format/tar/corion.htm

            byte[] header = zeroBuffer;

            int filenameUtf8Length = Encoding.UTF8.GetByteCount(filename);
            if(filenameUtf8Length > 99) // <- trailing nul
                throw new ArgumentException("Filename must be 99 or fewer UTF8 bytes.");
            Encoding.UTF8.GetBytes(filename, 0, filename.Length, header, 0);

            // NOTE: All these fields are octal because uggghhhhhhhhh

            // Write "mode"
            string mode =  "100777 ";
            Encoding.UTF8.GetBytes(mode, 0, mode.Length, header, 100);

            // Write "owner" and "group" IDs
            string owner = "     0 ";
            Encoding.UTF8.GetBytes(owner, 0, owner.Length, header, 108);
            Encoding.UTF8.GetBytes(owner, 0, owner.Length, header, 116);

            // Write the file size (no trailing nul)
            string sizeString = String.Format("{0, 11} ", Convert.ToString(size, 8));
            if(sizeString.Length != 12)
                throw new ArgumentException("File size is too large to encode");
            Encoding.UTF8.GetBytes(sizeString, 0, sizeString.Length, header, 124);

            // Write the time stamp (no trailing nul)
            var unixTime = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            string unixTimeString = String.Format("{0, 11} ", Convert.ToString(unixTime, 8));
            if(unixTimeString.Length != 12)
                throw new ArgumentException("File time is too large to encode");
            Encoding.UTF8.GetBytes(unixTimeString, 0, unixTimeString.Length, header, 136);

            // Link type = '0'
            header[156] = 0x30;

            // Calculate checksum:
            // NOTE: The following madness:
            //           > The checksum is calculated by taking the sum of the unsigned byte values of the header record
            //           > with the eight checksum bytes taken to be ascii spaces (decimal value 32)
            uint checksum = 8*32;
            for(int i = 0; i < header.Length; i++)
                checksum += header[i];
            checksum &= ((1u << 17)-1); // <- checksum precision is 17 bits
            string checksumString = String.Format("{0, 6} ", Convert.ToString((int)checksum, 8));
            Debug.Assert(checksumString.Length == 7); // <- precision limit should prevent this from overflowing
            Encoding.UTF8.GetBytes(checksumString, 0, checksumString.Length, header, 148);

            stream.Write(header, 0, header.Length);
            bytesWritten += header.Length;

            // Clear the zero buffer so we can reuse it for padding
            Array.Clear(zeroBuffer, 0, zeroBuffer.Length);
        }


        long bytesWritten;

        private void PadBlock()
        {
            long nextBoundary = (bytesWritten + 511) & ~511;
            int bytesToPad = (int)(nextBoundary - bytesWritten);
            Debug.Assert(bytesToPad < 512);

            stream.Write(zeroBuffer, 0, bytesToPad);
            bytesWritten += bytesToPad;
        }

    }
}
