using System.Collections.Generic;
using System.Diagnostics;

namespace Pixel3D.Animations
{
    /// <summary>
    /// An animation "play head" (warning: mutable struct)
    /// </summary>
    public struct AnimationPlayer
    {
        public Animation animation;
        public int frame;
        public int tick;

        public AnimationPlayer(Animation animation)
        {
            Debug.Assert(animation != null && animation.FrameCount > 0); // <- catch easy-to-make "blank animation" error

            this.animation = animation;
            frame = 0;
            tick = 0;
        }


        public static bool operator ==(AnimationPlayer a, AnimationPlayer b)
        {
            return ReferenceEquals(a.animation, b.animation) & (a.frame == b.frame) & (a.tick == b.tick);
        }

        public static bool operator !=(AnimationPlayer a, AnimationPlayer b)
        {
            return !(a == b);
        }



        public AnimationFrame CurrentFrame
        {
            get { return animation.Frames[frame]; }
        }

        public bool Done { get { return OnLastFrame && tick >= animation.Frames[frame].delay; } }

        /// <summary>Safe version of "Done" for animations that must end</summary>
        public bool DoneOnce { get { return animation.isLooped ? OnLastTickOfCurrentLoop : Done; } }

        public bool OnLastFrame { get { return !animation.isLooped && frame == animation.FrameCount - 1; } }

        public bool OnLastTickOfCurrentLoop { get { return frame == animation.FrameCount - 1 && tick == animation.Frames[frame].delay - 1; } }

        /// <summary>Set a given animation, if it is not set already (if it is already set, keep playing it)</summary>
        public void SetWithoutRestart(Animation animation)
        {
            if(this.animation != animation) // If we're not already playing the animation
                this = new AnimationPlayer(animation);
        }


        // Hacky way to stop the editor bugging out...
        public void EditorSafeTick()
        {
            if(animation == null || animation.Frames.Count == 0)
                return;

            if(frame >= animation.Frames.Count)
                frame = 0;

            Tick();
        }

        public void Tick()
        {
            tick += 1;

            int delayForFrame = animation.Frames[frame].delay;
            if(delayForFrame > 0) // frames with no delay
            {
                if(tick >= delayForFrame)
                    AdvanceFrame();
            }
        }

        /// <summary>Manually advance to the next frame</summary>
        public void AdvanceFrame()
        {
            tick = 0;

            frame++;
            if (frame >= animation.FrameCount)
            {
                if (animation.isLooped)
                {
                    frame = 0;
                }
                else
                {
                    frame = animation.FrameCount - 1;
                    tick = animation.Frames[frame].delay; // Become "Done"
                }
            }
        }

        /// <summary>Manually rewind to the last frame (for the editor)</summary>
        public void PreviousFrame()
        {
            tick = 0;
            frame--;
            if (frame >= 0) return;
            frame = animation.FrameCount - 1;
        }


        public void SetCurrentFrame(int frameIndex)
        {
            tick = 0;
            frame = frameIndex;
        }

        public int GetCurrentFrame()
        {
            return frame;
        }

        public Position PositionDeltaThisTick()
        {
            if(tick == 0) // Only apply position delta on the first frame
                return animation.Frames[frame].positionDelta;
            else
                return Position.Zero;
        }

        /// <summary>Return a list of triggers this tick, or null if there are no triggers. (Triggers only happen on tick 0 of a given frame.)</summary>
        public List<string> TriggersThisTick()
        {
            if(tick == 0)
                return animation.Frames[frame].triggers;
            else
                return null;
        }

        public string CueThisTick()
        {
            if (tick == 0)
                return animation.Frames[frame].cue;
            else
                return null;
        }
    }
}
