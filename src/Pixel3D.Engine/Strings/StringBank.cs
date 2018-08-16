using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Pixel3D.Engine.Strings
{
	public class StringBank
	{
		/// <summary>Represents a range of strings (so we don't need an individual list for every string!)</summary>
		public struct StringRange
		{
			public int start, count;
		}

		TagLookup<StringRange> lookup;
		List<string> lowercase;
		List<string> uppercase;


		#region Constructor and Serialization

		/// <summary>Construct from text file lines</summary>
		public StringBank(IEnumerable<string> inputLines, bool generateUpperCase = false)
		{
			// This is so we can collate the strings per tag set:
			Dictionary<TagSet, List<string>> temporaryLookup = new Dictionary<TagSet, List<string>>();
			int stringCount = 0;

			// Load all of the localized lines and collate them:
			foreach (var line in inputLines)
			{
				if (string.IsNullOrWhiteSpace(line) || line.TrimStart(' ').StartsWith("#"))
					continue; // ignore comment lines and empty lines

				var split = line.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);

				Debug.Assert(split.Length == 2, "invalid localization format");
				if (split.Length != 2)
					continue;

				var value = split[1].Trim();
				if (value == "[NONE]")
					continue;

				// to represent multiple symbols in a tag we use Symbol1,Symbol2
				var symbols = split[0].Trim().Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
				var tagSet = new TagSet(symbols);


				// Shove it in the string list for now...
				List<string> stringList;
				if (!temporaryLookup.TryGetValue(tagSet, out stringList))
					temporaryLookup.Add(tagSet, stringList = new List<string>());

				stringList.Add(ButtonGlyphReplacement(value));
				stringCount++;
			}

			// Fill out the real lookup with the collated lines:
			lookup = new TagLookup<StringRange>(temporaryLookup.Count);
			lowercase = new List<string>(stringCount);
			foreach (var kvp in temporaryLookup)
			{
				StringRange sr = new StringRange {start = lowercase.Count, count = kvp.Value.Count};
				lowercase.AddRange(kvp.Value);
				Debug.Assert(lowercase.Count == sr.start + sr.count);
				lookup.Add(kvp.Key, sr);
			}

			// Now that all strings are loaded, swap out any reference tags, 
			// i.e. "I am a <BeardedDragon>" -> "I am a Bearded Dragon"
			Regex tagsPattern = new Regex(@"<(\w*)>", RegexOptions.Compiled);
			for (int i = 0; i < lowercase.Count; i++)
			{
				lowercase[i] = tagsPattern.Replace(lowercase[i], m =>
				{
					List<string> replacements;
					if (temporaryLookup.TryGetValue(new TagSet(m.Groups[1].Captures[0].Value), out replacements)
					) // <- NOTE: doing dictionary lookup, not tag lookup
					{
						return replacements[0];
					}
					else
					{
						Console.WriteLine("Failed to find string replacement for \"{0}\"", m.Value);
						return m.Value;
					}
				});
			}

			// If you ask nicely, we'll generate upper-case strings as well...
			if (generateUpperCase)
			{
				uppercase = new List<string>(lowercase.Count);
				for (int i = 0; i < lowercase.Count; i++)
					uppercase.Add(lowercase[i].ToUpperInvariant());
			}
		}


		// NOTE: We do button glyph replacement directly on translations, so users can't enter a name like "[P]" and do crazy things.
		//       (also saves us a formatting step at runtime, which is obviously a win).
		//       This also opens up some interesting possibilities for doing a runtime replacement from "generic button" -> "specific button"
		//       (And we definitely don't want users to be doing that at runtime, possibly crossing the network.)

		public static string ButtonGlyphReplacement(string input)
		{
			int index = input.IndexOf('[');
			if (index == -1)
				return input;
			return ButtonGlyphReplacementHelper(input, index);
		}

		private static string ButtonGlyphReplacementHelper(string input, int fromIndex)
		{
			StringBuilder builder = new StringBuilder(input.Length);
			builder.Append(input, 0, fromIndex);

			for (int i = fromIndex; i < input.Length; i++)
			{
				if (input[i] == '[' && (i + 2 <= input.Length) && input[i + 2] == ']')
				{
					switch (input[i + 1])
					{
						// NOTE: Matches table in "rcru.xbdf"
						case 'U':
							builder.Append('\uE001');
							break; // Up
						case 'D':
							builder.Append('\uE002');
							break; // Down
						case 'L':
							builder.Append('\uE003');
							break; // Left
						case 'R':
							builder.Append('\uE004');
							break; // Right
						case 'X':
							builder.Append('\uE005');
							break; // Pause
						case 'J':
							builder.Append('\uE006');
							break; // Jump
						case 'P':
							builder.Append('\uE007');
							break; // Punch
						case 'K':
							builder.Append('\uE008');
							break; // Kick
						case 'G':
							builder.Append('\uE009');
							break; // Grab
						case 'B':
							builder.Append('\uE00A');
							break; // Block
						case 'S':
							builder.Append('\uE00B');
							break; // Special
						case 'T':
							builder.Append('\uE00C');
							break; // Taunt
						case 'I':
							builder.Append('\uE00D');
							break; // Select
						case 'E':
							builder.Append('\uE00E');
							break; // Use
						// NOTE: No replacement for "new-move-icon"

						default:
							goto copyCharacter;
					}

					i += 2;
					continue;
				}

				copyCharacter:
				builder.Append(input[i]);
			}

			return builder.ToString();
		}


		/// <summary>Reverse the glyph replacement for situations where we need to output normal text</summary>
		public static string ButtonGlyphsToPlainText(string input)
		{
			for (int i = 0; i < input.Length; i++)
			{
				if (input[i] >= '\uE000' && input[i] <= '\uF8FF') // <- Unicode Private Use Area
					return ButtonGlyphsToPlainTextHelper(input, i);
			}
			return input;
		}

		private static string ButtonGlyphsToPlainTextHelper(string input, int fromIndex)
		{
			StringBuilder builder = new StringBuilder(input.Length);
			builder.Append(input, 0, fromIndex);

			for (int i = fromIndex; i < input.Length; i++)
			{
				if (input[i] >= '\uE000' && input[i] <= '\uF8FF') // <- Unicode Private Use Area
				{
					switch (input[i])
					{
						// NOTE: Matches table in "rcru.xbdf"
						case '\uE001':
							builder.Append("[Up]");
							break;
						case '\uE002':
							builder.Append("[Down]");
							break;
						case '\uE003':
							builder.Append("[Left]");
							break;
						case '\uE004':
							builder.Append("[Right]");
							break;
						case '\uE005':
							builder.Append("[Pause]");
							break;
						case '\uE006':
							builder.Append("[Jump]");
							break;
						case '\uE007':
							builder.Append("[Punch]");
							break;
						case '\uE008':
							builder.Append("[Kick]");
							break;
						case '\uE009':
							builder.Append("[Grab]");
							break;
						case '\uE00A':
							builder.Append("[Block]");
							break;
						case '\uE00B':
							builder.Append("[Special]");
							break;
						case '\uE00C':
							builder.Append("[Taunt]");
							break;
						case '\uE00D':
							builder.Append("[Select]");
							break;
						case '\uE00E':
							builder.Append("[Use]");
							break;
						// NOTE: No replacement for "new-move-icon"

						default:
							builder.Append("[UNKNOWN PUA ");
							builder.Append((int) input[i]);
							builder.Append("]");
							break;
					}
				}
				else
					builder.Append(input[i]);
			}

			return builder.ToString();
		}



		/// <summary>Write to binary stream</summary>
		public void Serialize(BinaryWriter bw)
		{
			lookup.Serialize(bw, sr =>
			{
				bw.Write(sr.start);
				bw.Write(sr.count);
			});

			bw.Write(lowercase.Count);
			for (int i = 0; i < lowercase.Count; i++)
				bw.Write(lowercase[i]);
			// NOTE: we generate upper-case at load time (for performance)
		}

		/// <summary>Construct from binary stream</summary>
		public StringBank(BinaryReader br)
		{
			lookup = new TagLookup<StringRange>(br, () => new StringRange {start = br.ReadInt32(), count = br.ReadInt32()});

			int count = br.ReadInt32();
			lowercase = new List<string>(count);
			uppercase = new List<string>(count);
			for (int i = 0; i < count; i++)
			{
				string s = br.ReadString();
				lowercase.Add(s);
				uppercase.Add(s.ToUpperInvariant());
			}
		}

		#endregion



		#region Getters (by TagSet)

		public int GetStringCount(TagSet tagSet)
		{
			StringRange sr;
			bool result = lookup.TryGetBestValue(tagSet, out sr);
			Debug.Assert(result || sr.count == 0);
			return sr.count;
		}


		public string GetSingleString(TagSet tagSet)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return lowercase[sr.start];
			}
			return null;
		}

		public string GetSingleStringUppercase(TagSet tagSet)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return uppercase[sr.start];
			}
			return null;
		}


		public StringList GetStrings(TagSet tagSet)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return new StringList(lowercase, sr);
			}
			return new StringList();
		}

		public StringList GetStringsUppercase(TagSet tagSet)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return new StringList(uppercase, sr);
			}
			return new StringList();
		}


		public string GetStringChoice(TagSet tagSet, int choiceIndex)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return lowercase[sr.start + (choiceIndex % sr.count)];
			}
			return null;
		}

		public string GetStringChoiceUppercase(TagSet tagSet, int choiceIndex)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return uppercase[sr.start + (choiceIndex % sr.count)];
			}
			return null;
		}

		#endregion


		#region Getters (by string) -- NOTE: Copy-pasted!

		public int GetStringCount(string tagSet)
		{
			StringRange sr;
			bool result = lookup.TryGetBestValue(tagSet, out sr);
			Debug.Assert(result || sr.count == 0);
			return sr.count;
		}


		public string GetSingleString(string tagSet)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return lowercase[sr.start];
			}
			return null;
		}

		public string GetSingleStringUppercase(string tagSet)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return uppercase[sr.start];
			}
			return null;
		}


		public StringList GetStrings(string tagSet)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return new StringList(lowercase, sr);
			}
			return new StringList();
		}

		public StringList GetStringsUppercase(string tagSet)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return new StringList(uppercase, sr);
			}
			return new StringList();
		}


		public string GetStringChoice(string tagSet, int choiceIndex)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return lowercase[sr.start + (choiceIndex % sr.count)];
			}
			return null;
		}

		public string GetStringChoiceUppercase(string tagSet, int choiceIndex)
		{
			StringRange sr;
			if (lookup.TryGetBestValue(tagSet, out sr))
			{
				Debug.Assert(sr.count > 0);
				return uppercase[sr.start + (choiceIndex % sr.count)];
			}
			return null;
		}

		#endregion

	}
}
