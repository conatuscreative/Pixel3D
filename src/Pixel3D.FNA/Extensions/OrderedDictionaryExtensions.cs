// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Diagnostics;

namespace Pixel3D.Extensions
{
	public static class OrderedDictionaryExtensions
	{
		public static bool HasBaseFallback<T>(this OrderedDictionary<string, T> dictionary)
		{
			return dictionary.ContainsKey("*");
		}

		public static T GetBaseFallback<T>(this OrderedDictionary<string, T> dictionary)
		{
			return dictionary[Pixel3D.OrderedDictionaryExtensions.FallbackKey];
		}

		public static bool TryRemoveBaseFallBack<T>(this OrderedDictionary<string, T> dictionary)
		{
			return dictionary.Remove(Pixel3D.OrderedDictionaryExtensions.FallbackKey);
		}

		public static void AddBaseFallback<T>(this OrderedDictionary<string, T> dictionary, T value)
		{
			dictionary.Add(Pixel3D.OrderedDictionaryExtensions.FallbackKey, value);
		}

		public static T Get<T>(this OrderedDictionary<string, T> dictionary, string context)
		{
		    T value;
			if (context != null && dictionary.TryGetValue(context, out value))
				return value;

			if (dictionary.HasBaseFallback())
			{
				value = dictionary.GetBaseFallback();
				return value;
			}

			return default(T);
		}

		public static bool TryGetBestValue<T>(this OrderedDictionary<string, T> dictionary, string context, out T value)
		{
			if (context == null)
			{
				value = dictionary.GetBaseFallback();
				return dictionary.HasBaseFallback();
			}

			// TODO support multi-tag rules?
			//for (int i = countOfMultiTagRules; i < count; i++)
			//{
			//	if (rules[i].Count == 0 || (rules[i].Count == 1 && rules[i][0] == context))
			//	{
			//		value = values[i];
			//		return true;
			//	}
			//}

			value = dictionary.Get(context);
			if (value != null)
				return true;
			
			Debug.Assert(!dictionary.HasBaseFallback()); // Should always match something if we have a base fallback.
			value = default(T);
			return false;
		}
	}
}
