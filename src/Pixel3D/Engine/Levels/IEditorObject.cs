using Pixel3D.Animations;

namespace Pixel3D.Levels
{
    /// <summary>Provides various accessors for handling "objects" in the editor (ie: Shims and Things)</summary>
    public interface IEditorObject : IHasDrawableFrame
    {
        AnimationSet AnimationSet { get; }
        Position Position { get; set; }
        bool FacingLeft { get; }
    }
}