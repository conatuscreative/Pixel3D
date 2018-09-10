namespace Pixel3D
{
	public static class OrderedDictionaryExtensions
	{
		public const string FallbackKey = "*";

		public static int IndexOf<TKey, TValue>(this OrderedDictionary<TKey, TValue> dictionary, TKey value)
		{
			for (var i = 0; i < dictionary.Count; i++)
			{
				var rule = dictionary[dictionary.GetKey(i)];
				if (value.Equals(rule))
				{
					return i;
				}
			}
			return -1;
		}

		public static TValue GetValue<TKey, TValue>(this OrderedDictionary<TKey, TValue> dictionary, int i)
		{
			return dictionary[dictionary.GetKey(i)];
		}

		public static void SetValue<TKey, TValue>(this OrderedDictionary<TKey, TValue> dictionary, int i, TValue value)
		{
			var key = dictionary.GetKey(i);
			dictionary[key] = value;
		}

		public static bool RemoveAt<TKey, TValue>(this OrderedDictionary<TKey, TValue> dictionary, int i)
		{
			return dictionary.Remove(dictionary.GetKey(i));
		}
		
		public static bool TryGetExactValue<TKey, TValue>(this OrderedDictionary<TKey, TValue> dictionary, string context, out TValue value, out int index)
		{
			for (var i = 0; i < dictionary.Count; i++)
			{
				var rule = dictionary.GetKey(i);
				if (!context.Equals(rule))
					continue;
				value = dictionary[rule];
				index = i;
				return true;
			}
			value = default(TValue);
			index = -1;
			return false;
		}
	}
}
