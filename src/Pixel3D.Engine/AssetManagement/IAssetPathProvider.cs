namespace Pixel3D.Engine.AssetManagement
{
    public interface IAssetPathProvider
    {
        /// <summary>Get the path for an asset, or null if the asset is not managed by this provider</summary>
        string GetAssetPath<T>(T asset) where T : class;
    }
}
