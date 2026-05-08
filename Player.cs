using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Platformer2D
{
    /// <summary>
    /// Jogador com animações (Idle/Run/Jump/Die) e sons de salto e morte.
    /// Sprites retirados do MonoGame.Samples/Platformer2D.
    /// Sistema de disparo adaptado do SpaceInvaders (TP1).
    /// </summary>
    public class Player
    {
        public Vector2 Position;

        public int  Lives  { get; private set; } = 3;
        public bool IsDead => Lives <= 0;

        private Vector2       velocity;
        private bool          onGround  = false;

        // BUG FIX: sprite do jogador tem a face para a ESQUERDA por defeito.
        // Por isso quando olha para a direita, usamos FlipHorizontally.
        private SpriteEffects facing    = SpriteEffects.FlipHorizontally;
        private int           facingDir = 1; // 1=direita, -1=esquerda

        private KeyboardState prevKeys;

        // BUG FIX: invencibilidade após levar dano
        private int invincibleTimer          = 0;
        private const int InvincibleDuration = 90; // ~1.5 segundos a 60fps
        public bool IsInvincible => invincibleTimer > 0;

        private bool  isDying  = false;
        private float dyeTimer = 0f;

        // BUG FIX: Coyote time — permite saltar alguns frames após sair de uma plataforma
        private int coyoteTimer           = 0;
        private const int CoyoteFrames    = 8;

        // BUG FIX: Jump buffer — guarda pressão de salto para usar assim que aterrar
        private int jumpBuffer            = 0;
        private const int JumpBufferFrames = 12;

        private const float MoveSpeed    = 180f;
        private const float JumpForce    = -700f;
        private const float Gravity      = 1600f;
        private const float MaxFallSpeed = 700f;

        public const int Width  = 64;
        public const int Height = 64;

        public Rectangle Bounds =>
            new Rectangle((int)Position.X - Width / 2,
                          (int)Position.Y - Height,
                          Width, Height);

        private Animation       animIdle, animRun, animJump, animDie;
        private AnimationPlayer animPlayer;
        private SoundEffect     soundJump, soundKilled;
        private readonly Texture2D pixel;

        public Player(Texture2D pixel,
                      Animation idle, Animation run,
                      Animation jump, Animation die,
                      SoundEffect soundJump, SoundEffect soundKilled,
                      Vector2 startPos)
        {
            this.pixel       = pixel;
            this.animIdle    = idle;
            this.animRun     = run;
            this.animJump    = jump;
            this.animDie     = die;
            this.soundJump   = soundJump;
            this.soundKilled = soundKilled;
            this.Position    = startPos;
            prevKeys         = Keyboard.GetState();
            animPlayer.PlayAnimation(animIdle);
        }

        public void Update(GameTime gameTime,
                           List<Rectangle>  platforms,
                           List<Projectile> projectiles,
                           Texture2D        bulletTexture)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (invincibleTimer > 0) invincibleTimer--;

            // BUG FIX: após dyeTimer terminar, apenas decrementa 1 vida e sai do estado dying
            // Sem este fix, Lives era decrementado em cada frame após dyeTimer<=0 até 0
            if (isDying)
            {
                dyeTimer -= dt;
                if (dyeTimer <= 0)
                {
                    Lives--;
                    isDying  = false; // volta ao estado normal (com invencibilidade ativa)
                    dyeTimer = 0f;
                }
                return;
            }

            KeyboardState keys = Keyboard.GetState();

            // --- Movimento horizontal ---
            if (keys.IsKeyDown(Keys.A) || keys.IsKeyDown(Keys.Left))
            {
                velocity.X = -MoveSpeed;
                facing     = SpriteEffects.None;   // sprite vira para a esquerda (face natural)
                facingDir  = -1;
            }
            else if (keys.IsKeyDown(Keys.D) || keys.IsKeyDown(Keys.Right))
            {
                velocity.X = MoveSpeed;
                facing     = SpriteEffects.FlipHorizontally; // flip para ficar de frente à direita
                facingDir  = 1;
            }
            else
            {
                velocity.X = 0;
            }

            // --- Jump buffer: guarda pressão de salto ---
            bool jumpKeyPressed = (keys.IsKeyDown(Keys.W)     && prevKeys.IsKeyUp(Keys.W))    ||
                                  (keys.IsKeyDown(Keys.Up)    && prevKeys.IsKeyUp(Keys.Up))   ||
                                  (keys.IsKeyDown(Keys.Space) && prevKeys.IsKeyUp(Keys.Space));
            if (jumpKeyPressed) jumpBuffer = JumpBufferFrames;
            else if (jumpBuffer > 0) jumpBuffer--;

            // --- Coyote time: pode saltar pouco após sair da plataforma ---
            if (onGround) coyoteTimer = CoyoteFrames;
            else if (coyoteTimer > 0) coyoteTimer--;

            // --- Salto: executa se buffer ativo e coyote time ativo ---
            if (jumpBuffer > 0 && (onGround || coyoteTimer > 0))
            {
                velocity.Y   = JumpForce;
                onGround     = false;
                coyoteTimer  = 0;
                jumpBuffer   = 0;
                soundJump?.Play(0.5f, 0f, 0f);
            }

            // --- Disparo (F ou LeftCtrl) ---
            bool shootNow =
                (keys.IsKeyDown(Keys.F)           && prevKeys.IsKeyUp(Keys.F))           ||
                (keys.IsKeyDown(Keys.LeftControl)  && prevKeys.IsKeyUp(Keys.LeftControl));
            if (shootNow) Shoot(projectiles, bulletTexture);

            prevKeys = keys;

            // --- Gravidade ---
            velocity.Y += Gravity * dt;
            if (velocity.Y > MaxFallSpeed) velocity.Y = MaxFallSpeed;

            // --- Mover X e resolver colisões horizontais ---
            Position.X += velocity.X * dt;
            ResolveHorizontal(platforms);

            // BUG FIX: guardar Y antes de mover verticalmente
            // Usado em ResolveVertical para saber se o jogador veio de cima da plataforma
            float prevY = Position.Y;
            Position.Y += velocity.Y * dt;
            ResolveVertical(platforms, prevY);

            // --- Limites do mundo ---
            if (Position.X < Width / 2f)                        Position.X = Width / 2f;
            if (Position.X > Game1.WorldWidth - Width / 2f)     Position.X = Game1.WorldWidth - Width / 2f;

            // --- Escolher animação ---
            if (!onGround)
                animPlayer.PlayAnimation(animJump);
                else if (Math.Abs(velocity.X) > 5f)
                animPlayer.PlayAnimation(animRun);
            else
                animPlayer.PlayAnimation(animIdle);
        }

        public void TakeDamage()
        {
            if (IsInvincible || isDying) return;
            soundKilled?.Play(0.6f, 0f, 0f);
            isDying         = true;
            dyeTimer        = 0.6f;
            invincibleTimer = InvincibleDuration;
            animPlayer.PlayAnimation(animDie);
            velocity.Y = -400f;
        }

        private void Shoot(List<Projectile> projectiles, Texture2D bulletTexture)
        {
            float bx = facingDir == 1
                ? Position.X + Width / 2f + 2
                : Position.X - Width / 2f - Projectile.Width - 2;
            float by = Position.Y - Height / 2f - Projectile.Height / 2f;
            projectiles.Add(new Projectile(bulletTexture, new Vector2(bx, by), facingDir));
        }
private void ResolveHorizontal(List<Rectangle> platforms)
{
    foreach (Rectangle p in platforms)
    {
        if (!Bounds.Intersects(p)) continue;
        // LINHA NOVA: ignorar plataformas que o jogador está em cima (não a bater de lado)
        if (Bounds.Bottom <= p.Top + 8) continue;
        if      (velocity.X > 0) Position.X = p.Left  - Width / 2f;
        else if (velocity.X < 0) Position.X = p.Right + Width / 2f;
        velocity.X = 0;
    }
}
        // BUG FIX: colisão vertical com one-way platforms.
        // Usa prevY para saber se o jogador vinha de CIMA da plataforma.
        // Sem isto, o jogador não conseguia passar por baixo das plataformas
        // porque era empurrado para cima ao intersectar a parte inferior.
        private void ResolveVertical(List<Rectangle> platforms, float prevY)
        {
            onGround = false;
            foreach (Rectangle p in platforms)
            {
                if (!Bounds.Intersects(p)) continue;

                bool wasAbove = (prevY - Height) <= p.Top + 4;

                if (velocity.Y >= 0 && wasAbove)
                {
                    // Jogador cai e vinha de cima → aterra na plataforma
                    Position.Y = p.Top;
                    velocity.Y = 0;
                    onGround   = true;
                }
                else if (velocity.Y < 0)
                {
                    // Jogador sobe e bate no teto da plataforma
                    Position.Y = p.Bottom + Height;
                    velocity.Y = 0;
                }
                // Se veio de baixo e está a cair → não faz nada (passa por baixo)
            }
        }

        public void Draw(SpriteBatch sb, Camera cam, GameTime gameTime)
        {
            if (IsInvincible && !isDying && (invincibleTimer / 8) % 2 == 0) return;
            Vector2 drawPos = cam.WorldToScreen(Position);
            animPlayer.Draw(gameTime, sb, drawPos, facing);
        }
    }
}
