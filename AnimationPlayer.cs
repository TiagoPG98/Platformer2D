// AnimationPlayer.cs
// Retirado de: MonoGame.Samples / Platformer2D
// Fonte: https://github.com/MonoGame/MonoGame.Samples
// Licença: Microsoft XNA Community Game Platform
// Adaptado para o projeto Platformer2D — IPCA TDJV 2025/2026

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer2D
{
    /// <summary>
    /// Controla a reprodução de uma Animation.
    /// Avança os frames com base no tempo decorrido (GameTime).
    /// Retirado do sample oficial MonoGame.Samples/Platformer2D.
    /// </summary>
    public struct AnimationPlayer
    {
        public Animation Animation  { get; private set; }
        public int       FrameIndex { get; private set; }
        private float    time;

        public bool HasAnimation => Animation != null;

        public Vector2 Origin =>
            new Vector2(Animation.FrameWidth / 2f, Animation.FrameHeight);

        /// <summary>
        /// Inicia ou continua a reprodução de uma animação.
        /// Se a animação já estiver a correr, não reinicia.
        /// </summary>
        public void PlayAnimation(Animation animation)
        {
            if (Animation == animation) return;
            Animation  = animation;
            FrameIndex = 0;
            time       = 0f;
        }

        /// <summary>
        /// Avança o tempo e desenha o frame atual.
        /// Chamado a cada Draw().
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch sb,
                         Vector2 position, SpriteEffects effect)
        {
            // Protecção: se não houver animação carregada, não desenha
            if (Animation == null) return;

            time += (float)gameTime.ElapsedGameTime.TotalSeconds;

            while (time > Animation.FrameTime)
            {
                time -= Animation.FrameTime;
                if (Animation.IsLooping)
                    FrameIndex = (FrameIndex + 1) % Animation.FrameCount;
                else
                    FrameIndex = Math.Min(FrameIndex + 1, Animation.FrameCount - 1);
            }

            // PARA:
Rectangle source = new Rectangle(
    FrameIndex * Animation.FrameWidth, 0,
    Animation.FrameWidth, Animation.FrameHeight);

            sb.Draw(Animation.Texture, position, source,
                    Color.White, 0f, Origin, 1f, effect, 0f);
        }
    }
}
