namespace Pixel3D.Engine.AssetManagement
{
    public interface IAssetProvider
    {
        T Load<T>(string assetPath) where T : class;
    }
}
