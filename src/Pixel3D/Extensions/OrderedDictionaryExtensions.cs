using System.Diagnostics;

namespace Pixel3D
{
	public static class OrderedDictionaryExtensions
	{
		private const string FallbackKey = "*";

		public static bool HasBaseFallback<T>(this OrderedDictionary<string, T> dictionary)
		{
			return dictionary.ContainsKey("*");
		}

		public static T GetBaseFallback<T>(this OrderedDictionary<string, T> dictionary)
		{
			return dictionary[FallbackKey];
		}

		public static bool TryRemoveBaseFallBack<T>(this OrderedDictionary<string, T> dictionary)
		{
			return dictionary.Remove(FallbackKey);
		}

		public static void AddBaseFallback<T>(this OrderedDictionary<string, T> dictionary, T value)
		{
			dictionary.Add(FallbackKey, value);
		}

		public static T Get<T>(this OrderedDictionary<string, T> dictionary, string context)
		{
			if (dictionary.TryGetValue(context, out var value))
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
