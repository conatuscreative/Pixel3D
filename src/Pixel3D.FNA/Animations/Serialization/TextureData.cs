namespace Pixel3D.Animations.Serialization
{
    public struct TextureData
    {
        public int width, height;
        public byte[] data;

        public TextureData(int width, int height, byte[] data)
        {
            this.width = width;
            this.height = height;
            this.data = data;
        }
    }
}
