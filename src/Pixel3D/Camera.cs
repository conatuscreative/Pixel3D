using System;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Pixel3D.Extensions;

namespace Pixel3D
{
    public class Camera
    {
        #region Screen and Render Windows

        // Tracks the position of the window on the "Screen" (ie: for mouse position, full-screen overlays, etc)
        //  as well as its position on the current "Render" target (ie: for viewports, projection, etc)

        public Point ScreenSize { get; private set; }
        public Rectangle ScreenClipParent { get; private set; }
        /// <summary>Area for displaying content, in screen space</summary>
        public Rectangle ScreenContentArea { get; private set; }
        /// <summary>How much the content is zoomed within the screen area</summary>
        public int ScreenContentZoom { get; private set; }


        public Point RenderSize { get; private set; }
        public Rectangle RenderClipParent { get; private set; }
        /// <summary>Area for displaying content, in render space</summary>
        public Rectangle RenderContentArea { get; private set; }
        /// <summary>How much the content is zoomed within the render area</summary>
        public int RenderContentZoom { get; private set; }


        /// <summary>Size of the content area this camera is drawing into, in in-camera units</summary>
        public Point ContentSize { get; private set; }

        #endregion


        #region Windowing Setup

        public void SetRootWindow(int screenWidth, int screenHeight, int zoom)
        {
            ContentSize = new Point(
                    (screenWidth  + zoom - 1) / zoom,  // <- round-up (ceiling)
                    (screenHeight + zoom - 1) / zoom); // <- round-up (ceiling)

            RenderSize = ScreenSize = new Point(screenWidth, screenHeight);
            RenderClipParent = ScreenClipParent = new Rectangle(0, 0, screenWidth, screenHeight);
            RenderContentArea = ScreenContentArea = new Rectangle(0, 0, screenWidth, screenHeight);
            RenderContentZoom = ScreenContentZoom = zoom;

            RecalculateWindow();
        }



        /// <param name="contentZoom">The amount that the content is zoomed by in this area (if drawn to a render target, this is how much to scale the render target by)</param>
        /// <param name="makeRender">If this is true, then the content area will become a new render target</param>
        public void SetFromParent(Camera parent, Rectangle contentArea, int contentZoom, bool childIsRenderTarget)
        {
            if(ReferenceEquals(parent, this))
                throw new InvalidOperationException(); // could possibly implement this - can't be bothered right now
            if(contentZoom < 1)
                throw new ArgumentOutOfRangeException("contentZoom");


            // Set the content size, rounding up (overflow to bottom-left if not on a pixel boundary of the contained content)
            this.ContentSize = new Point(
                    (contentArea.Width  + contentZoom - 1) / contentZoom,
                    (contentArea.Height + contentZoom - 1) / contentZoom);


            this.ScreenSize = parent.ScreenSize;
            this.ScreenClipParent = parent.ScreenClippedArea;
            this.ScreenContentArea = parent.ContentToScreen(contentArea);
            this.ScreenContentZoom = parent.ScreenContentZoom * contentZoom;

            if(!childIsRenderTarget)
            {
                this.RenderSize = parent.RenderSize;
                this.RenderClipParent = parent.RenderClippedArea;
                this.RenderContentArea = parent.ContentToRender(contentArea);
                this.RenderContentZoom = parent.RenderContentZoom * contentZoom;
            }
            else // Alternate setup where the child is a render target surface
            {
                this.RenderSize = this.ContentSize;
                this.RenderClipParent = new Rectangle(0, 0, this.ContentSize.X, this.ContentSize.Y);
                this.RenderContentArea = new Rectangle(0, 0, this.ContentSize.X, this.ContentSize.Y);
                this.RenderContentZoom = 1;
            }

            this.RecalculateWindow();
        }

        /// <summary>Updates a child content window that has the same layout as its parent (useful for adding render target)</summary>
        public void SetFromParent(Camera parent, bool childIsRenderTarget)
        {
            SetFromParent(parent, new Rectangle(0, 0, parent.ContentSize.X, parent.ContentSize.Y), 1, childIsRenderTarget);
        }


        public void CopyTargetingFrom(Camera other)
        {
            // These automatically call recalculate:
            this.WorldTarget = other.WorldTarget;
            this.ContentTargetProportional = other.ContentTargetProportional;
        }


        /// <summary>Note: also copies this camera's targeting information</summary>
        public void CopyAndRemoveRenderTarget(Camera parent)
        {
            this.ContentSize = parent.ContentSize;
            this.RenderSize = this.ScreenSize = parent.ScreenSize;
            this.RenderClipParent = this.ScreenClipParent = parent.ScreenClipParent;
            this.RenderContentArea = this.ScreenContentArea = parent.ScreenContentArea;
            this.RenderContentZoom = this.ScreenContentZoom = parent.ScreenContentZoom;

            this.RecalculateWindow();

            CopyTargetingFrom(parent);
        }

        #endregion


        #region Windowing Outputs

        public Rectangle ScreenClippedArea { get; private set; }

        public Rectangle RenderClippedArea { get; private set; }


        public Microsoft.Xna.Framework.Graphics.Viewport Viewport { get { return new Microsoft.Xna.Framework.Graphics.Viewport(RenderClippedArea); } }

        public bool IsViewportValid
        {
            // NOTE: Skipping bounds testing, because we assume you set the screen area correctly and everything clipped properly
            get { return RenderClippedArea.Width > 0 && RenderClippedArea.Height > 0; }
        }
        

        public Matrix SpriteBatchProjectMatrix { get; private set; }


        private void RecalculateWindow()
        {
            ScreenClippedArea = Rectangle.Intersect(ScreenClipParent, ScreenContentArea);

            RenderClippedArea = Rectangle.Intersect(RenderClipParent, RenderContentArea); 
            SpriteBatchProjectMatrix = CreateSpriteBatchProjectMatrix(RenderClippedArea.Width, RenderClippedArea.Height);

            // Changing the window can change shared transforms as well as camera targeting
            RecalculateCamera();
        }

        public static Matrix CreateSpriteBatchProjectMatrix(int width, int height)
        {
            // TODO: Can we do this without the matrix multiply? Just call CreateOrthographicOffCenter with the offset built-in?
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
#if SDL2
            return projection;
#else
            Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
            return halfPixelOffset * projection;
#endif
        }

        #endregion


        #region Windowing Transforms and Queries

        /// <summary>Transforms a rectangle in content space to a rectangle in Render space (positions on the render target)</summary>
        public Rectangle ContentToRender(Rectangle rect)
        {
            Rectangle output;
            output.X = RenderContentArea.X + (rect.X * RenderContentZoom);
            output.Y = RenderContentArea.Y + (rect.Y * RenderContentZoom);
            output.Width  = rect.Width  * RenderContentZoom;
            output.Height = rect.Height * RenderContentZoom;

            return output;
        }

        /// <summary>Transforms a rectangle in content space to a rectangle in Viewport space</summary>
        public Rectangle ContentToView(Rectangle rect)
        {
            Rectangle output;
            output.X = (rect.X * RenderContentZoom);
            output.Y = (rect.Y * RenderContentZoom);
            output.Width  = rect.Width  * RenderContentZoom;
            output.Height = rect.Height * RenderContentZoom;

            return output;
        }

        public Rectangle ContentToScreen(Rectangle rect)
        {
            Rectangle output;
            output.X = ScreenContentArea.X + (rect.X * ScreenContentZoom);
            output.Y = ScreenContentArea.Y + (rect.Y * ScreenContentZoom);
            output.Width  = rect.Width  * ScreenContentZoom;
            output.Height = rect.Height * ScreenContentZoom;

            return output;
        }

        public bool ScreenPositionIsNotClipped(Point screenPosition)
        {
            return ScreenClippedArea.Contains(screenPosition);
        }


        /// <summary>Convert a position in Screen space to Content space (suitable for mouse input)</summary>
        public Point ScreenToContent(Point position)
        {
            return new Point((position.X - ScreenContentArea.X) / ScreenContentZoom, (position.Y - ScreenContentArea.Y) / ScreenContentZoom);
        }

        #endregion

        
        #region Letterboxing

        /// <summary> Bannon's letterbox is 31 pixels on the top</summary>
        public const int TopBarHeight = 31;

        /// <summary> Bannon's letterbox is 25 pixels on the bottom</summary>
        public const int BottomBarHeight = 25;

        /// <summary>Display bounds of the top bar</summary>
        public Rectangle TopBarDisplayBounds
        {
            get
            {
                var db = DisplayBounds;
                return new Rectangle(db.X, db.Y, db.Width, TopBarHeight);
            }
        }

        /// <summary>Display bounds of the bottom bar</summary>
        public Rectangle BottomBarDisplayBounds
        {
            get
            {
                var db = DisplayBounds;
                return new Rectangle(db.X, db.Y + db.Height - BottomBarHeight, db.Width, BottomBarHeight);
            }
        }

        #endregion


        #region Camera Targeting

        private Position _worldTarget;
        public Position WorldTarget
        {
            get { return _worldTarget; }
            set
            {
                if(_worldTarget != value)
                {
                    _worldTarget = value;
                    RecalculateCamera();
                }
            }
        }

        private Vector2 _contentTargetProportional = new Vector2(1f/2f, 2f/3f);
        /// <summary>Proportional position in the Content area where the Target will appear</summary>
        public Vector2 ContentTargetProportional
        {
            get { return _contentTargetProportional; }
            set
            {
                _contentTargetProportional = value;
                RecalculateCamera();
            }
        }

        #endregion


        #region Camera Outputs

        /// <summary>
        /// The pixel boundary where the WorldTarget will be drawn, in Content space.
        /// (NOTE: Will draw the WorldTarget above and to the right of this pixel boundary, as World space is flipped)
        /// </summary>
        public Point ContentTargetInPixels { get; private set; }


        /// <summary>The boundary of the Content area in Display space</summary>
        public Rectangle DisplayBounds { get; private set; }

        /// <summary>The boundary of the Content area in World space</summary>
        public Rectangle WorldZeroBounds { get; private set; }



        /// <summary>View matrix for rendering</summary>
        public Matrix ViewMatrix { get; private set; }

        /// <summary>View*Project matrix for rendering</summary>
        public Matrix ViewProjectMatrix { get; private set; }


        /// <summary>Matrix to convert Display space to Audio space</summary>
        public Matrix AudioMatrix { get; private set; }


        /// <summary>TODO: Want to get rid of this. Switch to WorldZeroBounds.Center - should be safe in all cases, but needs verification.</summary>
        [Obsolete]
        public Position WorldZeroCenterRoundedUp { get { return DisplayBounds.Center.FlipY().AsPosition(); } }
        
        private void RecalculateCamera()
        {
            ContentTargetInPixels = new Point(
                    (int)System.Math.Floor(ContentSize.X * ContentTargetProportional.X),
                    (int)System.Math.Floor(ContentSize.Y * ContentTargetProportional.Y));

            // Change the targeting calculation to put the world target at the top-left of the content area
            // (Historically, when we were in floating-point, we did this to keep the world pixel-aligned at the top left)
            // (NOTE: These are pixel boundaries, so the content target is below the boundary, while the world target is above!)
            Point targetAtWorldZero = WorldTarget.ToWorldZero;
            Point worldTargetTopLeft = new Point(
                    targetAtWorldZero.X - ContentTargetInPixels.X,
                    targetAtWorldZero.Y + ContentTargetInPixels.Y); // Negated ContentTargetInPixels.Y to convert relative Content to relative World

            // Convert to Display space:
            Point displayTargetTopLeft = new Point(worldTargetTopLeft.X, -worldTargetTopLeft.Y); // Negation of Y is conversion from world space to display space
            DisplayBounds = new Rectangle(displayTargetTopLeft.X, displayTargetTopLeft.Y, ContentSize.X, ContentSize.Y);

            // Calculate world boundary:
            WorldZeroBounds = DisplayBounds.FlipYNonIndexable();


            //
            // Create Matrices:

            Matrix cameraMatrix = Matrix.CreateTranslation(-displayTargetTopLeft.X, -displayTargetTopLeft.Y, 0); // Move the target to the origin (top-left)

            ViewMatrix = cameraMatrix * Matrix.CreateScale(RenderContentZoom, RenderContentZoom, 1)
                    * Matrix.CreateTranslation(RenderContentArea.X - RenderClippedArea.X, RenderContentArea.Y - RenderClippedArea.Y, 0);

            ViewProjectMatrix = ViewMatrix * SpriteBatchProjectMatrix;


            // Audio (being maths-lazy here by just tweaking cameraMatrix -AR)
            AudioMatrix = cameraMatrix * Matrix.CreateScale(2f / ContentSize.X, 2f / ContentSize.Y, 1f) * Matrix.CreateTranslation(-1, -1, 0);
        }

        #endregion


        #region Camera Transforms

        /// <summary>Convert a position in Content space to a position at World Zero</summary>
        public Point ContentToWorldZero(Point contentPosition)
        {
            // NOTE: Negating Y to convert from Content/Display space to World space
            //       Subtracing one to account for difference in pixel boundaries (Content has boundary at top-left, World is at bottom-left)
            return new Point(
                     (contentPosition.X + DisplayBounds.X),
                    -(contentPosition.Y + DisplayBounds.Y) - 1); 
        }

        /// <summary>Convert a world position to an audio pan/fade position (0 is centre, -1 and 1 are client edges)</summary>
        public Vector2 WorldToAudio(Position worldPosition)
        {
            return Vector2.Transform(worldPosition.ToDisplay, AudioMatrix);
        }

        public Rectangle WorldZeroToContent(Rectangle rectangle)
        {
            // Convert to display space:
            rectangle.Y = -(rectangle.Y + rectangle.Height); // NOTE: non-indexable flip

            // Convert to content space:
            rectangle.X -= DisplayBounds.X;
            rectangle.Y -= DisplayBounds.Y;

            return rectangle;
        }

        public Rectangle DisplayToContent(Rectangle rectangle)
        {
            // Convert to content space:
            rectangle.X -= DisplayBounds.X;
            rectangle.Y -= DisplayBounds.Y;

            return rectangle;
        }

        #endregion

        
        #region Combined Transforms

        public Point ScreenToWorldZero(Point screenPosition)
        {
            return ContentToWorldZero(ScreenToContent(screenPosition));
        }

        #endregion


        #region Layout

        /// <summary> Four slots across native width of 360 pixels = 90 pixels - 8 gutter for game clock = 82</summary>
        public const int PlayerSlotWidth = 82;

        /// <summary> Figure out the drawing area for the game, between the HUD bars</summary>
        public Rectangle CreateGameContentRectangle(Point hudContentSize)
        {
            return new Rectangle(0, TopBarHeight, hudContentSize.X, hudContentSize.Y - (TopBarHeight + BottomBarHeight));
        }


        #endregion
    }
}
