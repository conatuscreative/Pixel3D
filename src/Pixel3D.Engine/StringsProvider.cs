using System;
using System.IO;
using System.IO.Compression;
using Pixel3D.Engine.Strings;

namespace Pixel3D.Engine
{
	public class StringsProvider
	{
		#region Get By TagSet

		public int GetStringCount(TagSet tagSet, byte language)
		{
			return stringBanks[(int)language].GetStringCount(tagSet);
		}

		public string GetSingleString(TagSet tagSet, byte language)
		{
			return stringBanks[(int)language].GetSingleString(tagSet);
		}

		public string GetIndexedString(TagSet tagSet, byte language, int index)
		{
			return stringBanks[(int)language].GetStringChoice(tagSet, index);
		}

		public string GetSingleStringUppercase(TagSet tagSet, byte language)
		{
			return stringBanks[(int)language].GetSingleStringUppercase(tagSet);
		}

		public StringList GetStrings(TagSet tagSet, byte language)
		{
			return stringBanks[(int)language].GetStrings(tagSet);
		}

		public StringList GetStringsUppercase(TagSet tagSet, byte language)
		{
			return stringBanks[(int)language].GetStringsUppercase(tagSet);
		}

		public string GetRandomString(TagSet tagSet, byte language, int choiceIndex)
		{
			return stringBanks[(int)language].GetStringChoice(tagSet, choiceIndex);
		}

		public string GetRandomStringUppercase(TagSet tagSet, byte language, int choiceIndex)
		{
			return stringBanks[(int)language].GetStringChoiceUppercase(tagSet, choiceIndex);
		}

		#endregion
		
		#region Get By String

		public int GetStringCount(string tagSet, byte language)
		{
			return stringBanks[(int)language].GetStringCount(tagSet);
		}

		public string GetSingleString(string tagSet, byte language)
		{
			return stringBanks[(int)language].GetSingleString(tagSet);
		}

		public string GetIndexedString(string tagSet, byte language, int index)
		{
			return stringBanks[(int)language].GetStringChoice(tagSet, index);
		}

		public string GetSingleStringUppercase(string tagSet, byte language)
		{
			return stringBanks[(int)language].GetSingleStringUppercase(tagSet);
		}

		public StringList GetStrings(string tagSet, byte language)
		{
			return stringBanks[(int)language].GetStrings(tagSet);
		}

		public StringList GetStringsUppercase(string tagSet, byte language)
		{
			return stringBanks[(int)language].GetStringsUppercase(tagSet);
		}

		public string GetRandomString(string tagSet, byte language, int choiceIndex)
		{
			return stringBanks[(int)language].GetStringChoice(tagSet, choiceIndex);
		}

		public string GetRandomStringUppercase(string tagSet, byte language, int choiceIndex)
		{
			return stringBanks[(int)language].GetStringChoiceUppercase(tagSet, choiceIndex);
		}

		#endregion

		protected StringBank[] stringBanks;

		public void LoadStrings(byte[] header, string filename, int languageCount)
		{
			stringBanks = new StringBank[languageCount];

			string stringsPackagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);

			using (FileStream fs = File.OpenRead(stringsPackagePath))
			{
				for (int i = 0; i < header.Length; i++)
					if (fs.ReadByte() != header[i])
						throw new Exception("Strings package is corrupt");

				using (var br = new BinaryReader(new GZipStream(fs, CompressionMode.Decompress, true)))
				{
					for (int i = 0; i < languageCount; i++)
					{
						stringBanks[i] = new StringBank(br);
					}
				}
			}
		}
	}
}