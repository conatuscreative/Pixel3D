// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Strings
{
	public interface ILocalizationProvider
	{
		string GetSingleString(TagSet tagSet);
		string GetSingleStringUppercase(TagSet tagSet);
		StringList GetStrings(TagSet tagSet);

		string GetRandomString(TagSet tagSet);
		string GetRandomStringUppercase(TagSet tagSet);

		string GetSingleString(string symbol);
		string GetSingleStringUppercase(string symbol);
		StringList GetStrings(string symbol);

		string GetRandomString(string symbol);
		string GetRandomStringUppercase(string symbol);
	}
}