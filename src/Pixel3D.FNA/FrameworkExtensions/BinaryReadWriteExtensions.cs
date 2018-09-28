// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.IO;

namespace Pixel3D.FrameworkExtensions
{
    public static class BinaryReadWriteExtensions
    {
        public static bool WriteBoolean(this BinaryWriter bw, bool value)
        {
            bw.Write(value);
            return value; // Returning the written value allows for easy null checks
        }


        public static void WriteNullableString(this BinaryWriter bw, string value)
        {
            if(bw.WriteBoolean(value != null))
                bw.Write(value);
        }

        /// <summary>Write null in cases where the string is blank (sometimes the editor gives us blank strings for no good reason)</summary>
        public static void WriteNullableStringNonBlank(this BinaryWriter bw, string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                value = null;

            if(bw.WriteBoolean(value != null))
                bw.Write(value);
        }

        public static string ReadNullableString(this BinaryReader br)
        {
            if(br.ReadBoolean())
                return br.ReadString();
            else
                return null;
        }




        public static void WriteNullableSingle(this BinaryWriter bw, float? value)
        {
            if(bw.WriteBoolean(value.HasValue))
                bw.Write(value.Value);
        }

        public static float? ReadNullableSingle(this BinaryReader br)
        {
            if (br.ReadBoolean())
                return br.ReadSingle();
            else
                return null;
        }

        public static void WriteNullableInt(this BinaryWriter bw, int? value)
        {
            if(bw.WriteBoolean(value.HasValue))
                bw.Write(value.Value);
        }

        public static int? ReadNullableInt(this BinaryReader br)
        {
            if (br.ReadBoolean())
                return br.ReadInt32();
            else
                return null;
        }


    }
}
