using System.Runtime.InteropServices;

namespace Pixel3D.LoopRecorder
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Value128
    {
        public uint v1, v2, v3, v4;
        
        public static bool operator ==(Value128 a, Value128 b)
        {
            return a.v1 == b.v1 && a.v2 == b.v2 && a.v3 == b.v3 && a.v4 == b.v4;
        }

        public static bool operator !=(Value128 a, Value128 b)
        {
            return !(a == b);
        }

        public byte[] ToBytes()
        {
            byte[] result = new byte[4*4];

            result[ 0] = (byte)(v1 >> (0*8));
            result[ 1] = (byte)(v1 >> (1*8));
            result[ 2] = (byte)(v1 >> (2*8));
            result[ 3] = (byte)(v1 >> (3*8));
                    
            result[ 4] = (byte)(v2 >> (0*8));
            result[ 5] = (byte)(v2 >> (1*8));
            result[ 6] = (byte)(v2 >> (2*8));
            result[ 7] = (byte)(v2 >> (3*8));
                    
            result[ 8] = (byte)(v3 >> (0*8));
            result[ 9] = (byte)(v3 >> (1*8));
            result[10] = (byte)(v3 >> (2*8));
            result[11] = (byte)(v3 >> (3*8));
                    
            result[12] = (byte)(v4 >> (0*8));
            result[13] = (byte)(v4 >> (1*8));
            result[14] = (byte)(v4 >> (2*8));
            result[15] = (byte)(v4 >> (3*8));

            return result;
        }


        #region Object guff

        public override bool Equals(object obj)
        {
            if(obj is Value128)
                return ((Value128)obj) == this;
            return false;
        }

        public override int GetHashCode()
        {
            return (int)(v1 ^ v2 ^ v3 ^ v4);
        }

        public override string ToString()
        {
            return string.Format("{0:X8}{1:X8}{2:X8}{3:X8}", v1, v2, v3, v4);
        }

        #endregion

    }
}