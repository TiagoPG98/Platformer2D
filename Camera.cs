using Microsoft.Xna.Framework;

namespace Platformer2D
{
    /// <summary>
    /// Câmara que segue o jogador horizontalmente.
    /// Garante que o mundo é maior que a janela — requisito TP2.
    /// </summary>
    public class Camera
    {
        public Vector2 Position { get; private set; }

        private readonly int screenWidth;
        private readonly int worldWidth;

        public Camera(int screenWidth, int worldWidth)
        {
            this.screenWidth = screenWidth;
            this.worldWidth  = worldWidth;
        }

        public void Follow(Vector2 target)
        {
            float x = target.X - screenWidth / 2f;
            x = MathHelper.Clamp(x, 0, worldWidth - screenWidth);
            Position = new Vector2(x, 0);
        }

        public Rectangle WorldToScreen(Rectangle r) =>
            new Rectangle((int)(r.X - Position.X), r.Y, r.Width, r.Height);

        public Vector2 WorldToScreen(Vector2 v) => v - Position;
    }
}
