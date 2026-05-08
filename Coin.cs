using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer2D
{
    /// <summary>
    /// Item colecionável. O jogador recolhe ao tocar — dá pontos.
    /// Anima-se flutuando suavemente para cima e para baixo.
    /// </summary>
    public class Coin
    {
        public Vector2 Position;
        public bool    Active = true;

        private readonly Texture2D texture;
        private float bobTimer = 0f;

        public const int Width  = 20;
        public const int Height = 20;
        public const int Value  = 50;

        public Rectangle Bounds =>
            new Rectangle((int)Position.X,
                          (int)(Position.Y + MathF.Sin(bobTimer) * 4f),
                          Width, Height);

        public Coin(Texture2D texture, Vector2 position)
        {
            this.texture  = texture;
            this.Position = position;
        }

        public void Update() => bobTimer += 0.08f;

        public void Draw(SpriteBatch sb, Camera cam)
        {
            if (!Active) return;
            sb.Draw(texture, cam.WorldToScreen(Bounds), Color.Gold);
        }
    }
}
