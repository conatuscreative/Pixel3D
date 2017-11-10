using System;
using Pixel3D.Collections;

namespace Pixel3D.Levels
{
    public static class PropertiesExtensions
    {
        public static string GetString(this OrderedDictionary<string, string> properties, string propertyName)
        {
            string value;
            properties.TryGetValue(propertyName, out value);
            return value; // may be null
        }

        public static bool GetBoolean(this OrderedDictionary<string, string> properties, string propertyName)
        {
            string valueString;
            if (!properties.TryGetValue(propertyName, out valueString))
                return false;

            bool value;
            if (!bool.TryParse(valueString, out value))
                return false;

            return value;
        }

        public static sbyte? GetSByte(this OrderedDictionary<string, string> properties, string propertyName)
        {
            string valueString;
            if (!properties.TryGetValue(propertyName, out valueString))
                return null;

            sbyte value;
            if (!sbyte.TryParse(valueString, out value))
                return null;

            return value;
        }

        public static byte? GetByte(this OrderedDictionary<string, string> properties, string propertyName)
        {
            string valueString;
            if (!properties.TryGetValue(propertyName, out valueString))
                return null;

            byte value;
            if (!byte.TryParse(valueString, out value))
                return null;

            return value;
        }

        public static int? GetInteger(this OrderedDictionary<string, string> properties, string propertyName)
        {
            string valueString;
            if(!properties.TryGetValue(propertyName, out valueString))
                return null;

            int value;
            if(!Int32.TryParse(valueString, out value))
                return null;

            return value;
        }
    }
}
