using Microsoft.Xna.Framework.Graphics;

namespace Platformer2D
{
    public class Animation
    {
        public Texture2D Texture   { get; }
        public float     FrameTime { get; }
        public bool      IsLooping { get; }

        private readonly int frameCount;

        // FrameWidth calculado a partir do número real de frames
        public int FrameCount  => frameCount;
        public int FrameWidth  => Texture.Width / frameCount;
        public int FrameHeight => Texture.Height;

        /// <param name="frameCount">Número real de frames no spritesheet</param>
        public Animation(Texture2D texture, float frameTime, bool isLooping, int frameCount)
        {
            Texture         = texture;
            FrameTime       = frameTime;
            IsLooping       = isLooping;
            this.frameCount = frameCount;
        }
    }
}