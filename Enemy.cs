using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer2D
{
    public enum EnemyType { Normal, Fast }

    /// <summary>
    /// Inimigo com movimento autónomo de patrulha e animação de sprite.
    /// Sprites retirados do MonoGame.Samples/Platformer2D.
    /// Dois tipos: Normal (lento) e Fast (rápido).
    /// </summary>
    public class Enemy
    {
        public Vector2   Position;
        public bool      Active = true;
        public EnemyType Type;

        private float speed;
        private float patrolRange;
        private float startX;
        private int   direction = 1;

        private Animation       animRun;
        private AnimationPlayer animPlayer;
        private readonly Texture2D pixel;

        public const int Width  = 64;
        public const int Height = 64;

        public Rectangle Bounds =>
            new Rectangle((int)Position.X - Width / 2,
                          (int)Position.Y - Height,
                          Width, Height);

        public Enemy(Texture2D pixel, Vector2 position,
                     Animation idle, Animation run,
                     EnemyType type = EnemyType.Normal)
        {
            this.pixel    = pixel;
            this.Position = position;
            this.startX   = position.X;
            this.Type     = type;
            this.animRun  = run;

            if (type == EnemyType.Normal) { speed = 60f;  patrolRange = 150f; }
            else                          { speed = 130f; patrolRange = 260f; }

            animPlayer.PlayAnimation(animRun);
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position.X += speed * direction * dt;

            if (Position.X >= startX + patrolRange) direction = -1;
            if (Position.X <= startX)               direction =  1;

            animPlayer.PlayAnimation(animRun);
        }

        public void Draw(SpriteBatch sb, Camera cam, GameTime gameTime)
        {
            if (!Active) return;

            // sprite do inimigo tem a face para a ESQUERDA por defeito.
            // Quando direction= 1 (vai para a direita) → flip para ficar de frente

            SpriteEffects effect = direction > 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            animPlayer.Draw(gameTime, sb, cam.WorldToScreen(Position), effect);
        }
    }
}
