using Pixel3D.Animations;
using Pixel3D.Engine;
using Pixel3D.Engine.Levels;

namespace Pixel3D.Levels
{
    public interface ILevelSubBehaviour
    {
        void BeforeBeginLevel(UpdateContext updateContext);
        void BeginLevelStoryTriggers(UpdateContext updateContext);
        void BeginLevel(UpdateContext updateContext, Level previousLevel, string targetSpawn);
        void BeforeUpdate(UpdateContext updateContext);
        void AfterUpdate(UpdateContext updateContext);
        void BeforeBackgroundDraw(DrawContext drawContext);
        void AfterDraw(DrawContext drawContext);
    }
}