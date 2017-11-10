namespace Pixel3D.Navigation
{
    public struct PathClipResult
    {
        /// <summary>The preferred direction to travel along the path</summary>
        public DirectionNumber direction;
        /// <summary>Prediction of how far will be travelled in the X direction.</summary>
        public int travelX;
    }


    public struct DriveResult
    {
        public DirectionNumber preferredDirection;
        public DirectionFlags allowableDirections;
        public bool shouldJump;

        public static DriveResult Empty { get { return new DriveResult { preferredDirection = DirectionNumber.None }; } }
    }

}
