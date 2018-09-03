namespace Pixel3D.Strings
{
    public interface ILocalizationProvider
    {
        string GetSingleString(TagSet tagSet);
        string GetSingleStringUppercase(TagSet tagSet);
        StringList GetStrings(TagSet tagSet);

        string GetRandomString(TagSet tagSet);
        string GetRandomStringUppercase(TagSet tagSet);

        string GetSingleString(string tagSet);
        string GetSingleStringUppercase(string tagSet);
        StringList GetStrings(string tagSet);

        string GetRandomString(string tagSet);
        string GetRandomStringUppercase(string tagSet);
    }
}