// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.IO;
using System.IO.Compression;

namespace Pixel3D.Strings
{
	public class StringsProvider
	{
		protected StringBank[] stringBanks;

		public void LoadStrings(byte[] header, string filename, int languageCount)
		{
			stringBanks = new StringBank[languageCount];

			var stringsPackagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);

			using (var fs = File.OpenRead(stringsPackagePath))
			{
				for (var i = 0; i < header.Length; i++)
					if (fs.ReadByte() != header[i])
						throw new Exception("Strings package is corrupt");

				using (var br = new BinaryReader(new GZipStream(fs, CompressionMode.Decompress, true)))
				{
					for (var i = 0; i < languageCount; i++) stringBanks[i] = new StringBank(br);
				}
			}
		}

		#region Get By TagSet

		public int GetStringCount(TagSet tagSet, byte language)
		{
			return stringBanks[language].GetStringCount(tagSet);
		}

		public string GetSingleString(TagSet tagSet, byte language)
		{
			return stringBanks[language].GetSingleString(tagSet);
		}

		public string GetIndexedString(TagSet tagSet, byte language, int index)
		{
			return stringBanks[language].GetStringChoice(tagSet, index);
		}

		public string GetSingleStringUppercase(TagSet tagSet, byte language)
		{
			return stringBanks[language].GetSingleStringUppercase(tagSet);
		}

		public StringList GetStrings(TagSet tagSet, byte language)
		{
			return stringBanks[language].GetStrings(tagSet);
		}

		public StringList GetStringsUppercase(TagSet tagSet, byte language)
		{
			return stringBanks[language].GetStringsUppercase(tagSet);
		}

		public string GetRandomString(TagSet tagSet, byte language, int choiceIndex)
		{
			return stringBanks[language].GetStringChoice(tagSet, choiceIndex);
		}

		public string GetRandomStringUppercase(TagSet tagSet, byte language, int choiceIndex)
		{
			return stringBanks[language].GetStringChoiceUppercase(tagSet, choiceIndex);
		}

		#endregion

		#region Get By String

		public int GetStringCount(string tagSet, byte language)
		{
			return stringBanks[language].GetStringCount(tagSet);
		}

		public string GetSingleString(string tagSet, byte language)
		{
			return stringBanks[language].GetSingleString(tagSet);
		}

		public string GetIndexedString(string tagSet, byte language, int index)
		{
			return stringBanks[language].GetStringChoice(tagSet, index);
		}

		public string GetSingleStringUppercase(string tagSet, byte language)
		{
			return stringBanks[language].GetSingleStringUppercase(tagSet);
		}

		public StringList GetStrings(string tagSet, byte language)
		{
			return stringBanks[language].GetStrings(tagSet);
		}

		public StringList GetStringsUppercase(string tagSet, byte language)
		{
			return stringBanks[language].GetStringsUppercase(tagSet);
		}

		public string GetRandomString(string tagSet, byte language, int choiceIndex)
		{
			return stringBanks[language].GetStringChoice(tagSet, choiceIndex);
		}

		public string GetRandomStringUppercase(string tagSet, byte language, int choiceIndex)
		{
			return stringBanks[language].GetStringChoiceUppercase(tagSet, choiceIndex);
		}

		#endregion
	}
}