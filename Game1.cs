using System.IO;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Net.Mime;

namespace Platformer2D
{
    /// Classe principal do jogo. Herda de Game (MonoGame).
    /// Integra:
    ///   - Sistema de animação do MonoGame.Samples/Platformer2D
    ///   - Sons do MonoGame.Samples/Platformer2D
    ///   - Sistema de disparo adaptado do SpaceInvaders (TP1)
    ///   - Câmara, plataformas, inimigos e moedas desenvolvidos neste projeto

    public class Game1 : Game
    {
        public const int ScreenWidth  = 800;
        public const int ScreenHeight = 500;
        public const int WorldWidth   = 3200;

        private GraphicsDeviceManager graphics;
        private SpriteBatch           spriteBatch;
        private Texture2D             pixel;
        private SpriteFont            font;      // fonte para texto no ecrã

        private Texture2D texBackground;

        private enum GameState { Menu, Playing, Win, GameOver }
        private GameState state        = GameState.Menu;
        private int       score        = 0;
        private int       totalEnemies = 0;
        private KeyboardState prevKeys;

        private int highScore = 0; //high score para o jogo

        private Player           player;
        private List<Enemy>      enemies;
        private List<Projectile> projectiles;
        private List<Rectangle>  platforms;
        private List<Coin>       coins;
        private Camera           camera;

        // Animações do MonoGame.Samples/Platformer2D
        private Animation animPlayerIdle, animPlayerRun, animPlayerJump, animPlayerDie;
        private Animation animMonsterAIdle, animMonsterARun;
        private Animation animMonsterBIdle, animMonsterBRun;

        // Sons do MonoGame.Samples/Platformer2D
        private SoundEffect soundJump, soundKilled, soundMonsterKilled, soundCoin;

        public Game1()
{
    graphics = new GraphicsDeviceManager(this);

    Content.RootDirectory = "Content";
    IsMouseVisible = true;

    graphics.PreferredBackBufferWidth  = ScreenWidth;
    graphics.PreferredBackBufferHeight = ScreenHeight;

    // Limitar FPS
    IsFixedTimeStep = true;
    TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 30.0);
}
        protected override void Initialize()
        {
            camera   = new Camera(ScreenWidth, WorldWidth);
            prevKeys = Keyboard.GetState();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            // Carregar fonte — usada para texto no menu, HUD e ecrãs de fim
            try { font = Content.Load<SpriteFont>("Font"); }
            catch { font = null; }

            // Animações do jogador
            animPlayerDie  = new Animation(Content.Load<Texture2D>("Sprites/Player/Die"),  0.08f, false, 12);
            animPlayerJump = new Animation(Content.Load<Texture2D>("Sprites/Player/Jump"), 0.1f,  false, 11);
            animPlayerRun = new Animation(Content.Load<Texture2D>("Sprites/Player/Run"), 0.1f,true,10);
            animPlayerIdle = new Animation(Content.Load<Texture2D>("Sprites/Player/Idle"), 0.1f,false,1);

            // Animações dos monstros
            animMonsterAIdle = new Animation(Content.Load<Texture2D>("Sprites/MonsterA/Idle"), 0.15f, true,1);
            animMonsterARun  = new Animation(Content.Load<Texture2D>("Sprites/MonsterA/Run"),  0.1f,  true,10);
            animMonsterBIdle = new Animation(Content.Load<Texture2D>("Sprites/MonsterB/Idle"), 0.15f, true,1);
            animMonsterBRun  = new Animation(Content.Load<Texture2D>("Sprites/MonsterB/Run"),  0.08f, true,10);

            // Sons
            soundJump          = Content.Load<SoundEffect>("Sounds/PlayerJump");
            soundKilled        = Content.Load<SoundEffect>("Sounds/PlayerKilled");
            soundMonsterKilled = Content.Load<SoundEffect>("Sounds/MonsterKilled");
            soundCoin          = Content.Load<SoundEffect>("Sounds/GemCollected");

            //background
            texBackground = Content.Load<Texture2D>("background_LunarDriftStudios");

            LoadHighScore(); // Para o metodo de HighScores permamentes
        }

        private void SetupLevel()
        {
            score       = 0;
            projectiles = new List<Projectile>();

            // BUG FIX: plataformas subidas para criar espaço suficiente para o jogador passar por baixo
            // Jogador tem 64px de altura. Chão em y=440. Espaço mínimo necessário: >64px
            platforms = new List<Rectangle>
            {
                new Rectangle(0,    440, WorldWidth, 60), // chão

                new Rectangle(300,  340, 140, 18),  // gap para chão = 82px (>64 → passa por baixo)
                new Rectangle(580,  290, 140, 18),
                new Rectangle(860,  240, 160, 18),
                new Rectangle(1120, 300, 140, 18),
                new Rectangle(1400, 250, 160, 18),
                new Rectangle(1680, 290, 140, 18),
                new Rectangle(1960, 240, 180, 18),
                new Rectangle(2240, 310, 140, 18),
                new Rectangle(2530, 260, 160, 18),
                new Rectangle(2820, 320, 140, 18),
            };

            player = new Player(
                pixel,
                animPlayerIdle, animPlayerRun, animPlayerJump, animPlayerDie,
                soundJump, soundKilled,
                new Vector2(120, 376));

            enemies = new List<Enemy>
            {
                new Enemy(pixel, new Vector2(450,  440), animMonsterAIdle, animMonsterARun, EnemyType.Normal),
                new Enemy(pixel, new Vector2(800,  440), animMonsterBIdle, animMonsterBRun, EnemyType.Fast),
                new Enemy(pixel, new Vector2(1150, 440), animMonsterAIdle, animMonsterARun, EnemyType.Normal),
                new Enemy(pixel, new Vector2(1500, 440), animMonsterBIdle, animMonsterBRun, EnemyType.Fast),
                new Enemy(pixel, new Vector2(1850, 440), animMonsterAIdle, animMonsterARun, EnemyType.Normal),
                new Enemy(pixel, new Vector2(2200, 440), animMonsterBIdle, animMonsterBRun, EnemyType.Fast),
                new Enemy(pixel, new Vector2(2550, 440), animMonsterAIdle, animMonsterARun, EnemyType.Normal),
                new Enemy(pixel, new Vector2(2900, 440), animMonsterBIdle, animMonsterBRun, EnemyType.Fast),
            };
            totalEnemies = enemies.Count;

            coins = new List<Coin>
            {
                new Coin(pixel, new Vector2(340,  310)), new Coin(pixel, new Vector2(370, 310)),
                new Coin(pixel, new Vector2(625,  260)), new Coin(pixel, new Vector2(655, 260)),
                new Coin(pixel, new Vector2(905,  210)), new Coin(pixel, new Vector2(1165, 270)),
                new Coin(pixel, new Vector2(1445, 220)), new Coin(pixel, new Vector2(1725, 260)),
                new Coin(pixel, new Vector2(2005, 210)), new Coin(pixel, new Vector2(2285, 280)),
                new Coin(pixel, new Vector2(2575, 230)), new Coin(pixel, new Vector2(2865, 290)),
            };
        }

        // ── Ciclo MonoGame ─────────────────────────────────────────────────────

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.Escape)) Exit();

            switch (state)
            {
                case GameState.Menu:
                    bool start = (keys.IsKeyDown(Keys.Enter) && prevKeys.IsKeyUp(Keys.Enter)) ||
                                 (keys.IsKeyDown(Keys.Space) && prevKeys.IsKeyUp(Keys.Space));
                    if (start) { SetupLevel(); state = GameState.Playing; }
                    break;

                case GameState.Playing:
                    UpdatePlaying(gameTime);
                    break;

                case GameState.Win:
                case GameState.GameOver:
                    if (keys.IsKeyDown(Keys.R) && prevKeys.IsKeyUp(Keys.R))
                        state = GameState.Menu;
                    break;
            }

            prevKeys = keys;
            base.Update(gameTime);
        }

        private void UpdatePlaying(GameTime gameTime)
        {
            player.Update(gameTime, platforms, projectiles, pixel);
            camera.Follow(player.Position);

            foreach (Enemy e in enemies)
                if (e.Active) e.Update(gameTime);

            foreach (Projectile p in projectiles)
                if (p.Active) p.Update();

            foreach (Coin c in coins)
                if (c.Active) c.Update();

            // Colisões: projétil ↔ inimigo
            foreach (Projectile p in projectiles)
            {
                if (!p.Active) continue;
                foreach (Enemy e in enemies)
                {
                    if (!e.Active) continue;
                    if (p.Bounds.Intersects(e.Bounds))
                    {
                        p.Active = false;
                        e.Active = false;
                        score   += e.Type == EnemyType.Fast ? 200 : 100;
                        soundMonsterKilled?.Play(0.5f, 0f, 0f);
                    }
                }
            }

            // Colisões: jogador ↔ inimigo
            foreach (Enemy e in enemies)
                if (e.Active && player.Bounds.Intersects(e.Bounds))
                    player.TakeDamage();

            // Colisões: jogador ↔ moeda
            foreach (Coin c in coins)
            {
                if (!c.Active) continue;
                if (player.Bounds.Intersects(c.Bounds))
                {
                    c.Active = false;
                    score   += Coin.Value;
                    soundCoin?.Play(0.5f, 0f, 0f);
                }
            }

            projectiles.RemoveAll(p => !p.Active);
            enemies.RemoveAll(e     => !e.Active);
            coins.RemoveAll(c       => !c.Active);

          if (player.IsDead)
        {
            if (score > highScore) 
            highScore = score;
            SaveHighScore(); //permanente
            state = GameState.GameOver;
        }

            if (enemies.Count == 0)
        {
            if (score > highScore) 
            highScore = score;
            SaveHighScore(); //permanente
            state = GameState.Win;
            }
        }

        // ── Rendering ──────────────────────────────────────────────────────────

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(15, 15, 40));
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            switch (state)
            {
                case GameState.Menu:
                    DrawMenu();
                    break;
                case GameState.Playing:
                    DrawPlaying(gameTime);
                    break;
                case GameState.Win:
                    DrawPlaying(gameTime);
                    DrawEndScreen(true);
                    break;
                case GameState.GameOver:
                    DrawPlaying(gameTime);
                    DrawEndScreen(false);
                    break;
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawMenu()
        {
            // Fundo com gradiente simulado
            DrawRect(0, 0, ScreenWidth, ScreenHeight, new Color(10, 10, 30));
            DrawRect(0, ScreenHeight / 2, ScreenWidth, ScreenHeight / 2, new Color(5, 5, 20));

            // Recorde — canto superior direito, fora do painel
            DrawRect(ScreenWidth - 200, 10, 190, 36, new Color(20, 20, 60));
            DrawRect(ScreenWidth - 200, 10, 190, 2,  Color.Gold); // borda dourada no topo
            DrawText("RECORDE",       ScreenWidth - 105, 14, Color.Gold,  center: true, scale: 0.85f);
            DrawText($"{highScore}",  ScreenWidth - 105, 28, Color.White, center: true, scale: 1f);
            
            // Painel principal
            DrawRect(ScreenWidth / 2 - 220, 80, 440, 340, new Color(20, 20, 60));
            DrawRect(ScreenWidth / 2 - 218, 82, 436, 3,   new Color(100, 100, 255)); // borda topo
            DrawRect(ScreenWidth / 2 - 218, 82, 3,   336, new Color(60, 60, 180));   // borda esquerda
            DrawRect(ScreenWidth / 2 + 215, 82, 3,   336, new Color(60, 60, 180));   // borda direita

            // Título
            DrawText("PLATFORMER", ScreenWidth / 2, 110, Color.White, center: true, scale: 2f);
            DrawText("2D",         ScreenWidth / 2, 145, new Color(100, 180, 255), center: true, scale: 1.5f);

            // Linha separadora
            DrawRect(ScreenWidth / 2 - 180, 175, 360, 2, new Color(60, 60, 120));

            // Controlos
            DrawText("CONTROLOS:", ScreenWidth / 2 - 160, 190, new Color(150, 150, 220));

            DrawRect(ScreenWidth / 2 - 160, 215, 80, 24, new Color(40, 40, 100));
            DrawText("A / D",  ScreenWidth / 2 - 120, 219, Color.Yellow, center: true);
            DrawText("Mover",  ScreenWidth / 2 - 40,  219, Color.White);

            DrawRect(ScreenWidth / 2 - 160, 245, 80, 24, new Color(40, 40, 100));
            DrawText("W / ESP", ScreenWidth / 2 - 120, 249, Color.Yellow, center: true);
            DrawText("Saltar",  ScreenWidth / 2 - 40,  249, Color.White);

            DrawRect(ScreenWidth / 2 - 160, 275, 80, 24, new Color(40, 40, 100));
            DrawText("F / CTRL", ScreenWidth / 2 - 120, 279, Color.Yellow, center: true);
            DrawText("Disparar", ScreenWidth / 2 - 40,  279, Color.White);

            DrawRect(ScreenWidth / 2 - 160, 305, 80, 24, new Color(40, 40, 100));
            DrawText("ESC",   ScreenWidth / 2 - 120, 309, Color.Yellow, center: true);
            DrawText("Sair",  ScreenWidth / 2 - 40,  309, Color.White);
            

            // Botão Enter
            DrawRect(ScreenWidth / 2 - 140, 350, 280, 50, new Color(22, 100, 22));
            DrawRect(ScreenWidth / 2 - 138, 352, 276, 46, new Color(34, 160, 34));
            DrawText("ENTER para jogar", ScreenWidth / 2, 368, Color.White, center: true, scale: 1.3f);
        }

        private void DrawPlaying(GameTime gameTime)
        {

            // Parallax suave: background move a 30% da velocidade da câmara
int bgW = texBackground.Width;
int bgH = texBackground.Height;
int offsetX = -(int)(camera.Position.X * 0.3f) % bgW;
if (offsetX > 0) offsetX -= bgW;

for (int x = offsetX; x < ScreenWidth; x += bgW)
    spriteBatch.Draw(texBackground,
        new Rectangle(x, ScreenHeight - bgH, bgW, bgH),
        Color.White);
            // Plataformas
            foreach (Rectangle p in platforms)
            {
                DrawRect(camera.WorldToScreen(p), Color.ForestGreen);
                DrawRect(camera.WorldToScreen(new Rectangle(p.X, p.Y, p.Width, 4)), new Color(100, 200, 100));
            }

            foreach (Coin c in coins)            c.Draw(spriteBatch, camera);
            foreach (Enemy e in enemies)         e.Draw(spriteBatch, camera, gameTime);
            foreach (Projectile p in projectiles) p.Draw(spriteBatch, camera);
            player.Draw(spriteBatch, camera, gameTime);

            DrawHUD();
        }

        private void DrawHUD()
        {
            // Barra do HUD
            DrawRect(0, 0, ScreenWidth, 44, new Color(0, 0, 0, 200));
            DrawRect(0, 43, ScreenWidth, 2,  new Color(50, 50, 100));

            // --- Vidas ---
            DrawText("VIDAS:", 10, 12, new Color(200, 200, 200));
            for (int i = 0; i < 3; i++)
            {
                Color cor = i < player.Lives ? Color.Red : new Color(60, 20, 20);
                DrawRect(75 + i * 28, 10, 22, 22, cor);
                // mini coração: detalhe branco
                if (i < player.Lives)
                    DrawRect(79 + i * 28, 13, 6, 6, new Color(255, 120, 120));
            }

            // --- Score ---
            DrawText($"SCORE: {score}", ScreenWidth / 2, 12, Color.Gold, center: true);

            // --- Barra de progresso de inimigos ---
            int killed = totalEnemies - enemies.Count;
            DrawText("INIMIGOS:", ScreenWidth - 220, 6, new Color(200, 200, 200), scale: 0.85f);
            DrawRect(ScreenWidth - 130, 8,  120, 12, new Color(40, 10, 10));
            DrawRect(ScreenWidth - 130, 8,
                     totalEnemies > 0 ? (int)(120f * killed / totalEnemies) : 0,
                     12, Color.OrangeRed);
            DrawText($"{killed}/{totalEnemies}", ScreenWidth - 70, 22, Color.OrangeRed,
                     center: true, scale: 0.8f);

            // --- Moedas restantes ---
            DrawText($"MOEDAS: {(score % (Coin.Value + 1) == 0 ? score / Coin.Value : "?")}", 10, 28,
         new Color(180, 180, 180), scale: 0.75f);
        }

        private void DrawEndScreen(bool win)
        {
            Color bgColor = win ? new Color(0, 120, 0, 160) : new Color(120, 0, 0, 160);
            DrawRect(0, 0, ScreenWidth, ScreenHeight, bgColor);

            // Painel central
            DrawRect(ScreenWidth / 2 - 220, ScreenHeight / 2 - 80, 440, 220,
                     new Color(0, 0, 0, 210));
            DrawRect(ScreenWidth / 2 - 218, ScreenHeight / 2 - 78, 436, 3,
                     win ? Color.LimeGreen : Color.OrangeRed);

            // Título
            string title = win ? "VITORIA!" : "GAME OVER";
            Color  titleColor = win ? Color.LimeGreen : Color.OrangeRed;
            DrawText(title, ScreenWidth / 2, ScreenHeight / 2 - 60, titleColor,
                     center: true, scale: 2f);

            // Score final
            DrawText($"Score final: {score}", ScreenWidth / 2, ScreenHeight / 2 - 20,
                     Color.White, center: true, scale: 1.2f);
                    
            // Barra de score final
            int maxScore = totalEnemies * 200 + 12 * Coin.Value;
            float pct    = maxScore > 0 ? score / (float)maxScore : 1f;
            DrawText($"Recorde: {highScore}", ScreenWidth / 2, ScreenHeight / 2 + 5,
            Color.Gold, center: true, scale: 1f);

            DrawText($"Score atual: {score}", ScreenWidth / 2, ScreenHeight / 2 + 25,
            score >= highScore ? Color.LimeGreen : Color.White,
            center: true, scale: 1f);


            // Instrução reiniciar
            DrawText("Prima R para voltar ao menu", ScreenWidth / 2, ScreenHeight / 2 + 68,
                     new Color(200, 200, 200), center: true);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void DrawRect(int x, int y, int w, int h, Color c) =>
            spriteBatch.Draw(pixel, new Rectangle(x, y, w, h), c);

        private void DrawRect(Rectangle r, Color c) =>
            spriteBatch.Draw(pixel, r, c);

        /// <summary>
        /// Desenha texto se a fonte estiver disponível.
        /// Se não houver fonte, desenha um retângulo como placeholder.
        /// </summary>
        private void DrawText(string text, int x, int y, Color color,
                              bool center = false, float scale = 1f)
        {
            if (font == null)
            {
                // Fallback: retângulo proporcional ao texto
                int w = (int)(text.Length * 8 * scale);
                int h = (int)(14 * scale);
                int rx = center ? x - w / 2 : x;
                DrawRect(rx, y, w, h, new Color(color, 120));
                return;
            }

            Vector2 size = font.MeasureString(text) * scale;
            Vector2 pos  = center
                ? new Vector2(x - size.X / 2f, y)
                : new Vector2(x, y);

            spriteBatch.DrawString(font, text, pos, color,
                                   0f, Vector2.Zero, scale,
                                   SpriteEffects.None, 0f);
        }
        private void SaveHighScore()
{
    File.WriteAllText("highscore.txt", highScore.ToString());
}

private void LoadHighScore()
{
    if (File.Exists("highscore.txt"))
        int.TryParse(File.ReadAllText("highscore.txt"), out highScore);
}
    }
}
