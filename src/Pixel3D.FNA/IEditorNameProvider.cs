namespace Pixel3D
{
    /// <summary> Here specifically to support fetching the friendly names of untyped assets </summary>
    public interface IEditorNameProvider
    {
        string EditorName { get; }
    }
}