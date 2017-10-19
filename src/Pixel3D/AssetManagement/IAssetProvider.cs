namespace Pixel3D.AssetManagement
{
    public interface IAssetProvider
    {
        T Load<T>(string assetPath) where T : class;
    }
}
