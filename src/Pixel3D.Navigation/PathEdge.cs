namespace Pixel3D.Navigation
{
    public struct PathEdge
    {
        public int navEdgeIndex;
        //public NavRegion region; // <- Commented out: Final path shouldn't need to store this (but we might want to see it for FindPath debug)
    }
}
