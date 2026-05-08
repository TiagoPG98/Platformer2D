using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer2D
{
    /// <summary>
    /// Projétil disparado pelo jogador.
    /// Sistema de disparo adaptado do TManager/MissileSprite do SpaceInvaders (TP1).
    /// Move-se horizontalmente e desaparece ao sair do mundo ou ao colidir.
    /// </summary>
    public class Projectile
    {
        public Vector2 Position;
        public bool    Active = true;

        private readonly float     direction;
        private readonly Texture2D texture;
        private const    float     Speed = 10f;

        public const int Width  = 18;
        public const int Height = 8;

        public Rectangle Bounds =>
            new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        public Projectile(Texture2D texture, Vector2 position, int direction)
        {
            this.texture   = texture;
            this.Position  = position;
            this.direction = direction;
        }

        public void Update()
        {
            Position.X += Speed * direction;
            if (Position.X < -100 || Position.X > Game1.WorldWidth + 100)
                Active = false;
        }

        public void Draw(SpriteBatch sb, Camera cam)
        {
            if (!Active) return;
            sb.Draw(texture, cam.WorldToScreen(Bounds), Color.Yellow);
        }
    }
}
