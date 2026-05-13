# Platformer 2D — Técnicas de Desenvolvimento de Videojogos

Técnicas de Desenvolvimento de Videojogos  
IPCA — Instituto Politécnico do Cávado e do Ave  
2025/2026  
**Trabalho Prático 02 **

---

## Identificação do Grupo

- Tiago Gonçalves => 34617
- Gonçalo Ribeiro => 36854

**Repositório:** https://github.com/TiagoPG98/Platformer2D

---

## Descrição do Jogo

Platformer 2D é um jogo de plataformas de scroll horizontal desenvolvido em **C# com MonoGame**. O jogador controla um soldado que atravessa um mundo de selva com 3200 pixels de largura, eliminando inimigos com disparos e recolhendo moedas espalhadas pelas plataformas elevadas. O objetivo é eliminar todos os 8 inimigos sem perder as 3 vidas disponíveis.

O projeto integra elementos de dois repositórios externos com sistemas desenvolvidos de raiz:

- **Sistema de animação + assets:** retirado e adaptado do `MonoGame.Samples/Platformer2D` (sample oficial do MonoGame — https://github.com/MonoGame/MonoGame.Samples)
- **Inspiração para o sistema de disparo:** adaptado do `SpaceInvaders` (TP1, `joshberc/SpaceInvaders` — https://github.com/joshberc/SpaceInvaders)
- **Câmara, física, colisões, HUD, menu, HighScore, moedas:** desenvolvidos de raiz, com base em conceitos e padrões de outros projetos MonoGame referenciados ao longo do documento

---

## O que foi retirado de outros projetos vs. o que é original

### Retirado diretamente do MonoGame.Samples/Platformer2D

| Componente | Ficheiro | Notas |
|---|---|---|
| Classe `Animation` | `Animation.cs` | Adaptada — adicionado `frameCount` explícito |
| Struct `AnimationPlayer` | `AnimationPlayer.cs` | Adaptada — corrigido `source Rectangle` |
| Sprites do jogador | `Sprites/Player/*.png` | Idle, Run, Jump, Die — sem alteração |
| Sprites dos monstros | `Sprites/MonsterA/*.png`, `Sprites/MonsterB/*.png` | Idle, Run — sem alteração |
| Efeitos sonoros | `Sounds/*.wav` | PlayerJump, PlayerKilled, MonsterKilled, GemCollected |

### Inspirado no SpaceInvaders (TP1 — joshberc/SpaceInvaders)

| Conceito | Aplicação neste projeto |
|---|---|
| Projétil com movimento autónomo e desaparecimento | `Projectile.cs` — move-se, verifica limites, fica inativo |
| Remoção segura de objetos com flag `Active` | `RemoveAll(p => !p.Active)` — mesmo padrão do `TManager.DeleteList` |
| Deteção de colisão AABB por `Rectangle.Intersects()` | Todas as colisões do jogo |
| Edge detection no disparo via `prevKeys` | Evita disparo contínuo ao segurar tecla |

### Desenvolvido de raiz — com referências a outros projetos

| Componente | Ficheiro | Baseado em / Referência |
|---|---|---|
| Câmara com scroll | `Camera.cs` | Padrão do MonoGame.Samples/Platformer2D (câmara que segue a personagem) |
| Física do jogador | `Player.cs` | Aula 17 TDJV ("Implementação de Gravidade num jogo 2D"); padrão do MonoGame.Samples/Platformer2D |
| Coyote time + Jump buffer | `Player.cs` | Documentado por Maddy Thorson (Celeste, 2018); padrão de platformers modernos |
| One-way platforms | `Player.cs` | Técnica padrão em platformers 2D; presente no MonoGame.Samples/Platformer2D |
| Sistema de vidas e invencibilidade | `Player.cs` | Baseado no `liveCount` e `rDown` do Sokoban do professor |
| Dois tipos de inimigo com patrulha | `Enemy.cs` | Baseado nos `InvaderSprite` do SpaceInvaders (TP1) — bounce entre limites |
| Moedas colecionáveis | `Coin.cs` | Equivalente às gems do MonoGame.Samples/Platformer2D |
| Menu com estados | `Game1.cs` | Baseado no sistema `isWin`/`liveCount` do Sokoban do professor |
| HUD com vidas e score | `Game1.cs` | Baseado no HUD de tempo e vidas do Sokoban do professor |
| HighScore persistente | `Game1.cs` | Mesmo padrão de I/O do Sokoban (`File.ReadAllLines` para níveis) |
| Background com parallax | `Game1.cs` | Técnica padrão de platformers 2D; asset de Lunar Drift Studios |
| Limite de balas e distância | `Projectile.cs`, `Player.cs` | Baseado nas limitações de mísseis do SpaceInvaders (TP1) |

---

## Descrição da Implementação

### Arquitetura Geral

O projeto segue uma arquitetura orientada a objetos com responsabilidades separadas por classe. A abordagem é diferente da do SpaceInvaders (TP1) que usava um `TManager<T>` genérico para gerir grupos de objetos — aqui, em vez de um gestor centralizado, cada tipo de objeto é gerido diretamente em listas em `Game1`, com iteração e remoção explícitas, tornando o código mais direto e fácil de compreender:

```
Game1 (classe principal — herda de Game do MonoGame)
├── Camera            — câmara de scroll horizontal
├── Player            — jogador com física, animações, disparo
├── List<Enemy>       — inimigos com patrulha autónoma (2 tipos)
├── List<Projectile>  — projéteis ativos
├── List<Coin>        — moedas colecionáveis
├── Animation         — spritesheet com frames (do MonoGame.Samples)
└── AnimationPlayer   — reprodução de frames por tempo (do MonoGame.Samples)
```

### Estados do Jogo

O jogo usa um enum `GameState` com 4 estados: `Menu → Playing → Win / GameOver → Menu`. Esta abordagem é baseada no sistema de estados do **Sokoban do professor**, que usava variáveis booleanas (`isWin`, `rDown`) para controlar o estado do jogo. Aqui, em vez de variáveis individuais, usa-se um enum com um `switch` no `Update()` e `Draw()`, tornando a gestão de estados mais explícita e extensível:

```csharp
private enum GameState { Menu, Playing, Win, GameOver }

protected override void Update(GameTime gameTime)
{
    switch (state)
    {
        case GameState.Menu:    /* aguarda Enter/Espaço */ break;
        case GameState.Playing: UpdatePlaying(gameTime);  break;
        case GameState.Win:
        case GameState.GameOver: /* aguarda R */          break;
    }
}
```

### Mundo, Câmara e Parallax

O mundo tem **3200 pixels** de largura — 4× o tamanho da janela (800px). A câmara segue o jogador horizontalmente usando `MathHelper.Clamp()` para nunca mostrar fora dos limites — o mesmo padrão de câmara do `MonoGame.Samples/Platformer2D`, onde a câmara é centrada na personagem e clampada aos limites do nível. No SpaceInvaders (TP1), o mundo era fixo ao tamanho da janela (`Global.ScreenWidth = 800`); aqui o mundo é 4× maior, exigindo a câmara de scroll.

O nível tem **10 plataformas elevadas** além do chão principal. A primeira plataforma começa em `x=300, y=340`. As restantes alternam entre `y=240` e `y=320`, criando um percurso com subidas e descidas ao longo do mundo. Cada plataforma tem 18px de altura e entre 140 e 180px de largura, posicionadas com pelo menos **82px de gap** até ao chão (superior à altura do jogador de 64px), garantindo que é sempre possível passar por baixo.

O background de selva move-se a **30% da velocidade da câmara**, criando um efeito de parallax com uma única imagem tileable — técnica padrão em platformers 2D de scroll horizontal, implementada em MonoGame seguindo o padrão descrito em jgallant.com/how-to-create-a-parallax-effect-in-monogame e documentada no MonoGame.Samples/Platformer2D (ref: github.com/MonoGame/MonoGame.Samples/blob/3.8.0/Platformer2D/Documentation/3_advanced_platformer_features.md):

```csharp
int offsetX = -(int)(camera.Position.X * 0.3f) % bgW;
if (offsetX > 0) offsetX -= bgW;
for (int x = offsetX; x < ScreenWidth; x += bgW)
    spriteBatch.Draw(texBackground, new Rectangle(x, ScreenHeight - bgH, bgW, bgH), Color.White);
```

### Sistema de Animação (MonoGame.Samples — retirado e adaptado)

O sistema de animação foi retirado do sample oficial do MonoGame e é composto por duas classes:

**`Animation`** representa uma spritesheet onde os frames estão dispostos horizontalmente lado a lado. Calcula `FrameWidth = Texture.Width / frameCount`.

**`AnimationPlayer`** (struct) controla a reprodução. A cada `Draw()`, acumula o tempo decorrido e avança `FrameIndex` quando ultrapassa `FrameTime`. Recorta o frame correto com um `source Rectangle`. O uso de struct em vez de classe é o padrão do sample original — evita alocações no heap a cada frame, importante para performance:

```csharp
Rectangle source = new Rectangle(
    FrameIndex * Animation.FrameWidth, 0,
    Animation.FrameWidth, Animation.FrameHeight);
sb.Draw(Animation.Texture, position, source, Color.White, 0f, Origin, 1f, effect, 0f);
```

A `Origin` é `(FrameWidth/2, FrameHeight)` — centro-baixo do frame — para que os pés da personagem estejam sempre na posição do objeto, independentemente da direção. Este é o mesmo padrão do MonoGame.Samples/Platformer2D original.

**Adaptação crítica ao `frameCount`:** a versão original assumia frames quadrados (`FrameWidth = Texture.Height`). Como diferentes animações têm números de frames diferentes, adicionou-se `frameCount` como parâmetro obrigatório:

```csharp
animPlayerDie  = new Animation(texture, 0.08f, false, 12); // Die.png:  704×64, 12 frames
animPlayerJump = new Animation(texture, 0.1f,  false, 11); // Jump.png: 11 frames
animPlayerRun  = new Animation(texture, 0.1f,  true,  10); // Run.png:  10 frames
animPlayerIdle = new Animation(texture, 0.1f,  false,  1); // Idle.png:  1 frame
```

### Física do Jogador

A física do jogador foi desenvolvida de raiz, baseada nos conceitos de gravidade e movimento contínuo lecionados na **aula 17 de TDJV** ("Implementação de Gravidade num jogo 2D") e no padrão de física do `MonoGame.Samples/Platformer2D`. Ao contrário do Sokoban do professor (movimento discreto tile-a-tile) e do SpaceInvaders (movimento em pixels por frame sem gravidade), aqui o movimento é contínuo com acumulação de velocidade por tempo real:

- **Gravidade:** `1600f px/s²` acumulada em `velocity.Y` a cada frame via `dt`
- **Velocidade máxima de queda:** clampada a `700f px/s` para evitar tunneling em framerates baixos
- **Velocidade horizontal:** `±180f px/s` enquanto tecla pressionada, `0` ao largar
- **Força de salto:** `-700f px/s` aplicada instantaneamente

O movimento é aplicado separadamente nos eixos X e Y com resolução de colisões após cada eixo — técnica padrão em platformers 2D presente no `MonoGame.Samples/Platformer2D` que evita artefactos quando o jogador toca simultaneamente numa parede e num chão.

#### Coyote Time

Permite saltar até **8 frames** após sair do chão. Desenvolvido de raiz baseado no padrão de design documentado em artigos de desenvolvimento de jogos e popularizado por jogos como Celeste (2018). Não existe no MonoGame.Samples/Platformer2D nem no Sokoban do professor — é uma adição original (ref: thealmightyguru.com/Wiki/index.php?title=Coyote_time e ketra-games.com/2021/08/coyote-time-and-jump-buffering.html):

```csharp
if (onGround) coyoteTimer = 8;
else if (coyoteTimer > 0) coyoteTimer--;

if (jumpBuffer > 0 && (onGround || coyoteTimer > 0))
    velocity.Y = JumpForce;
```

#### Jump Buffer

Guarda a pressão do botão de salto por **12 frames**. Se o jogador pressionar o salto antes de aterrar, o salto executa automaticamente ao tocar o chão. Desenvolvido de raiz, complementa o coyote time para tornar os saltos completamente responsivos sem alterar a física base (ref: ketra-games.com/2021/08/coyote-time-and-jump-buffering.html):

```csharp
if (jumpKeyPressed) jumpBuffer = 12;
else if (jumpBuffer > 0) jumpBuffer--;
```

#### One-Way Platforms

Para permitir passar por baixo das plataformas elevadas, a resolução de colisão vertical verifica se o jogador **vinha de cima** antes de aterrar, usando `prevY` guardado antes do movimento vertical. Esta técnica é padrão em platformers 2D, documentada na documentação oficial do MonoGame (ref: docs.monogame.net/articles/tutorials/building_2d_games/12_collision_detection) e presente no `MonoGame.Samples/Platformer2D`. Sem ela, o jogador seria empurrado para cima ao tentar passar por baixo, como acontece em sistemas de colisão simples:

```csharp
float prevY = Position.Y;
Position.Y += velocity.Y * dt;

bool wasAbove = (prevY - Height) <= p.Top + 4;
if (velocity.Y >= 0 && wasAbove) // cai e vinha de cima → aterra
{
    Position.Y = p.Top;
    velocity.Y = 0;
    onGround   = true;
}
// se vinha de baixo e está a cair → não faz nada (passa por baixo)
```

### Sistema de Disparo (inspirado no SpaceInvaders — TP1)

O sistema de disparo foi desenvolvido com base no `MissileSprite` e no padrão de remoção com `Active` flag do `TManager` do SpaceInvaders (TP1). Em vez de um `TManager` genérico, os projéteis são geridos numa `List<Projectile>` com `RemoveAll()` — o mesmo resultado mas de forma mais direta:

```csharp
// SpaceInvaders (TP1) — TManager com DeleteList
public void MarkForRemoval(Int32 id) => DeleteList.Add(id);
foreach (Int32 id in DeleteList) Delete(id);

// Este projeto — mesma ideia, forma mais direta
projectiles.RemoveAll(p => !p.Active);
```

O disparo usa **edge detection** com `prevKeys` — idêntico ao flag `shooting` do `PlayerSprite` no SpaceInvaders que impedia disparos contínuos ao segurar a tecla:

```csharp
bool shootNow =
    (keys.IsKeyDown(Keys.F)           && prevKeys.IsKeyUp(Keys.F))           ||
    (keys.IsKeyDown(Keys.LeftControl)  && prevKeys.IsKeyUp(Keys.LeftControl));
```

**Limite de distância (original):** diferente do SpaceInvaders onde os mísseis percorriam o ecrã todo, aqui cada projétil desaparece ao fim de **400 pixels**, obrigando o jogador a aproximar dos inimigos. Esta é uma decisão de design original que aumenta o desafio:

```csharp
distanceTravelled += Speed;
if (distanceTravelled >= 400f) Active = false;
```

**Limite de balas simultâneas:** máximo de **2 projéteis** ativos. No SpaceInvaders (TP1) existia um único míssil de cada vez (flag `shooting`); aqui aumentou-se para 2 para dar mais liberdade sem tornar o jogo trivial:

```csharp
if (shootNow && projectiles.Count < 2)
    Shoot(projectiles, bulletTexture);
```

### Inimigos (inspirado no SpaceInvaders — TP1)

O conceito de inimigos com movimento autónomo é baseado nos `InvaderSprite` do SpaceInvaders (TP1), que bounceavam entre as bordas do ecrã. Aqui, em vez de todos os inimigos partilharem os mesmos limites do ecrã, cada inimigo tem a sua **própria zona de patrulha independente** definida por `startX` e `patrolRange`:

```csharp
// SpaceInvaders (TP1) — todos os invasores usam os mesmos limites do ecrã
if (position.X < 0) Speed = 1.5f;
else if (position.X > Global.ScreenWidth - invaderWidth) Speed = -1.5f;

// Este projeto — cada inimigo tem a sua própria zona
if (Position.X >= startX + patrolRange) direction = -1;
if (Position.X <= startX)               direction =  1;
```

Existem dois tipos com características distintas, ambos com animação de corrida em loop (`isLooping = true`):

- **`EnemyType.Normal`** => velocidade `60px/s`, patrulha `150px`, vale 100 pontos
- **`EnemyType.Fast`** => velocidade `130px/s`, patrulha `260px`, vale 200 pontos

O flip do sprite usa a lógica inversa ao que seria intuitivo — os sprites têm a face voltada para a esquerda por defeito, por isso flipam quando se movem para a direita:

```csharp
SpriteEffects effect = direction > 0
    ? SpriteEffects.FlipHorizontally  // move para a direita → flip para ficar de frente
    : SpriteEffects.None;             // move para a esquerda → face natural do sprite
```

### Sistema de Vidas e Invencibilidade (baseado no Sokoban do professor)

O sistema de vidas foi desenvolvido de raiz, baseado na mecânica de `liveCount` e reinício do Sokoban do professor. No Sokoban, premir `R` decrementava uma vida e reiniciava o nível; aqui, ao ser atingido por um inimigo, o jogador entra num estado `isDying` de 0.6 segundos após o qual perde uma vida e fica invencível por **90 frames (~1.5 segundos)** — durante a invencibilidade o sprite pisca a cada 8 frames, padrão visual clássico em platformers:

```csharp
// Piscar durante invencibilidade
if (IsInvincible && !isDying && (invincibleTimer / 8) % 2 == 0) return;
```

### Moedas (baseado nas gems do MonoGame.Samples/Platformer2D)

As moedas são o equivalente às gems do `MonoGame.Samples/Platformer2D`. No sample original, as gems eram desenhadas com sprite próprio e apanhadas pelo jogador para completar o nível. Aqui, as moedas são desenhadas como retângulos dourados (sem sprite) e valem pontos em vez de serem condição de vitória. A animação de flutuação usa `MathF.Sin(bobTimer)` para criar um movimento vertical suave de ±4 pixels — técnica comum para itens colecionáveis em jogos 2D:

```csharp
bobTimer += 0.08f;
// posição visual = posição base + oscilação senoidal
int visualY = (int)(Position.Y + MathF.Sin(bobTimer) * 4);
```

### HUD (baseado no Sokoban do professor)

O HUD foi desenvolvido de raiz, baseado no HUD de tempo e vidas do Sokoban do professor. No Sokoban, o HUD mostrava tempo (`levelTime`) e vidas (`liveCount`) com `DrawString()`; aqui usa-se o mesmo padrão mas com uma barra preta semitransparente no topo (44px) e elementos adicionais:

- **Vidas** (esquerda) => 3 quadrados vermelhos; quadrado escuro = vida perdida (equivalente à lógica `liveCount` do Sokoban)
- **Score** (centro) => texto dourado centrado com score atual
- **Progresso de inimigos** (direita) => barra laranja com `killed/total` (original — não existe no Sokoban)
- **Moedas** (esquerda inferior) => contador de moedas recolhidas

A função `DrawText()` tem um fallback para retângulos quando a fonte não está disponível — inspirado no uso de `DrawString` com SpriteFont do Sokoban do professor, mas com resiliência a falhas de carregamento.

### Score e HighScore Persistente (I/O baseado no Sokoban do professor)

O sistema de HighScore persistente usa o mesmo padrão de I/O de ficheiros do **Sokoban do professor**, que usa `File.ReadAllLines()` para carregar os ficheiros de nível em texto. Aqui, aplica-se o mesmo conceito mas para persistência de dados entre sessões:

```csharp
// Sokoban do professor — leitura de nível
string[] linhas = File.ReadAllLines($"Content/{levelFile}");

// Este projeto — leitura de highscore (mesmo padrão, aplicação diferente)
private void LoadHighScore()
{
    if (File.Exists("highscore.txt"))
        int.TryParse(File.ReadAllText("highscore.txt"), out highScore);
}

private void SaveHighScore() =>
    File.WriteAllText("highscore.txt", highScore.ToString());
```

O recorde é visível no **menu principal** (canto superior direito) e no **ecrã de fim de jogo**, onde o score atual aparece a verde se igualou ou bateu o recorde. O ficheiro `highscore.txt` fica em `bin/Debug/net6.0/` e persiste entre sessões — para resetar, basta apagar o ficheiro.

---

## Organização das Pastas e Ficheiros

```
Platformer2D/
│
├── Content/                              <- Assets compilados pelo MonoGame Content Pipeline
│   ├── Sprites/
│   │   ├── Player/
│   │   │   ├── Idle.png                  <- 1 frame
│   │   │   ├── Run.png                   <- 10 frames (spritesheet horizontal)
│   │   │   ├── Jump.png                  <- 11 frames (spritesheet horizontal)
│   │   │   └── Die.png                   <- 12 frames (704×64px)
│   │   ├── MonsterA/
│   │   │   ├── Idle.png                  <- 1 frame  — inimigo Normal
│   │   │   └── Run.png                   <- 10 frames
│   │   └── MonsterB/
│   │       ├── Idle.png                  <- 1 frame  — inimigo Fast
│   │       └── Run.png                   <- 10 frames
│   ├── Sounds/
│   │   ├── PlayerJump.wav                <- toca ao saltar
│   │   ├── PlayerKilled.wav              <- toca ao ser atingido
│   │   ├── MonsterKilled.wav             <- toca ao eliminar inimigo
│   │   └── GemCollected.wav              <- toca ao recolher moeda
│   ├── background_LunarDriftStudios.png  <- background tileable de selva com parallax
│   ├── bullets_Master484.png             <- pack de balas (não utilizado — asset reservado)
│   ├── Font.spritefont                   <- fonte Arial 14pt para HUD e menus
│   └── Content.mgcb                      <- manifesto do MonoGame Content Pipeline
│
├── Animation.cs        <- Spritesheet horizontal (MonoGame.Samples — adaptado)
├── AnimationPlayer.cs  <- Reprodutor de frames por tempo (MonoGame.Samples — adaptado)
├── Camera.cs           <- Câmara de scroll (original, padrão MonoGame.Samples)
├── Coin.cs             <- Moeda colecionável com flutuação (original, baseado em gems do MonoGame.Samples)
├── Enemy.cs            <- Inimigo com patrulha, 2 tipos (original, inspirado no SpaceInvaders TP1)
├── Game1.cs            <- Classe principal: estados, rendering, HighScore (original)
├── Player.cs           <- Jogador: física, animação, disparo, vidas (original)
├── Projectile.cs       <- Projétil com limite de distância (original, inspirado no SpaceInvaders TP1)
├── Program.cs          <- Ponto de entrada
└── Platformer2D.csproj <- Configuração .NET 6.0, MonoGame 3.8.1
```

---

## Análise do Código

### `Program.cs`
Ponto de entrada. Cria instância de `Game1` e chama `Run()`, que inicia o ciclo MonoGame (Initialize → LoadContent → Update/Draw em loop). Idêntico ao `Program.cs` do SpaceInvaders (TP1) e do Sokoban do professor.

---

### `Camera.cs`
Câmara de scroll horizontal que converte posições do mundo para coordenadas do ecrã. Desenvolvida de raiz seguindo o padrão de câmara do `MonoGame.Samples/Platformer2D`.

- `Follow(Vector2 target)` => centra a câmara no alvo com `MathHelper.Clamp()` para não sair dos limites
- `WorldToScreen(Rectangle)` => subtrai `Position.X` para converter coordenadas do mundo para o ecrã
- `WorldToScreen(Vector2)` => mesma lógica para pontos

---

### `Animation.cs`
Retirada do `MonoGame.Samples/Platformer2D` e adaptada com `frameCount` explícito no construtor.

- `FrameCount` => número de frames (passado no construtor)
- `FrameWidth` => `Texture.Width / FrameCount`
- `FrameHeight` => `Texture.Height`
- `FrameTime` => duração de cada frame em segundos
- `IsLooping` => se a animação reinicia ao chegar ao último frame

---

### `AnimationPlayer.cs`
Struct retirada do `MonoGame.Samples/Platformer2D`. Usa struct (valor) em vez de classe (referência) para evitar alocações no heap.

- `PlayAnimation(Animation)` => inicia animação; não reinicia se já estiver a correr (comparação por referência)
- `Draw(GameTime, SpriteBatch, Vector2, SpriteEffects)` => acumula tempo, avança frame, recorta e desenha
- `Origin` => `(FrameWidth/2, FrameHeight)` — pivot no centro-baixo, padrão do MonoGame.Samples

---

### `Coin.cs`
Moeda colecionável baseada nas gems do `MonoGame.Samples/Platformer2D`. Desenhada como retângulo `Color.Gold` usando a textura `pixel` 1×1 em vez de sprite próprio. Anima com `MathF.Sin(bobTimer)` para flutuação de ±4px. Ao ser apanhada, fica inativa e adiciona 50 pontos.

---

### `Projectile.cs`
Projétil desenvolvido de raiz, inspirado no `MissileSprite` do SpaceInvaders (TP1).

- Move-se `10px/frame` na direção definida no construtor
- `distanceTravelled` acumula distância; inativo ao atingir `400px` (original — não existe no TP1)
- Desenhado como retângulo `Color.Yellow` com textura `pixel` 1×1

---

### `Enemy.cs`
Inimigo desenvolvido de raiz, inspirado nos `InvaderSprite` do SpaceInvaders (TP1), mas com zona de patrulha própria por inimigo em vez de limites globais do ecrã.

**Variáveis:**
- `direction` => `+1` ou `-1`, inverte ao atingir os limites da zona
- `speed` => px/s (Normal: `60f`, Fast: `130f`)
- `patrolRange` => distância da zona em px (Normal: `150f`, Fast: `260f`)
- `startX` => limite esquerdo da patrulha

**`Update(GameTime)`** => move por `speed * direction * dt`, inverte se necessário, mantém animação Run ativa

**`Draw(SpriteBatch, Camera, GameTime)`** => calcula `SpriteEffects` (invertido porque sprites têm face para a esquerda) e delega para `animPlayer.Draw()`

---

### `Player.cs`
Classe mais complexa. Desenvolvida de raiz baseada nos conceitos da aula 17 de TDJV e no padrão do `MonoGame.Samples/Platformer2D`.

**Constantes de física:**
- `MoveSpeed = 180f` px/s
- `JumpForce = -700f` px/s
- `Gravity   = 1600f` px/s²
- `MaxFallSpeed = 700f` px/s

**Variáveis de estado:**
- `velocity` => velocidade atual; X por input, Y por gravidade
- `onGround` => controlado por `ResolveVertical()`
- `facing` => `SpriteEffects` (None = esquerda, FlipH = direita); inicial FlipH porque sprite tem face para esquerda
- `facingDir` => `+1` ou `-1`; define direção do projétil
- `isDying` => bloqueia input durante 0.6s após dano
- `invincibleTimer` => 90 frames de invencibilidade após dying
- `coyoteTimer` => 8 frames de coyote time
- `jumpBuffer` => 12 frames de jump buffer

**Ordem de execução do `Update()`:**
1. Decrementar `invincibleTimer`
2. Se `isDying` → decrementar timer, perder vida ao terminar, return
3. Ler teclado — movimento, jump buffer, disparo (edge detection com `prevKeys`)
4. Aplicar coyote time e jump buffer → executar salto se condições cumpridas
5. Acumular gravidade em `velocity.Y`
6. Mover X → `ResolveHorizontal()`
7. Guardar `prevY` → Mover Y → `ResolveVertical(prevY)`
8. Clampar aos limites do mundo
9. Selecionar animação: Jump se `!onGround`, Run se `Math.Abs(velocity.X) > 5f`, Idle caso contrário

**`TakeDamage()`** => verifica invencibilidade; se pode ser atingido: toca som, `isDying = true`, inicia timers, reproduce animação Die

**`Draw()`** => pisca com `(invincibleTimer / 8) % 2 == 0`; delega para `animPlayer.Draw()`

---

### `Game1.cs`
Classe principal que herda de `Game` (MonoGame). Desenvolvida de raiz, estruturada de forma similar ao `Game1.cs` do Sokoban do professor mas com sistema de estados explícito.

**`Initialize()`** => cria `Camera`; guarda estado inicial do teclado em `prevKeys`

**`LoadContent()`** => cria textura `pixel` 1×1; carrega fonte com `try/catch`; carrega animações com `frameCount` explícito; carrega sons; carrega background; chama `LoadHighScore()`

**`SetupLevel()`** => reseta score e projéteis; cria plataformas (chão + 10 elevadas); cria jogador em `(120, 376)`; cria 8 inimigos alternando Normal e Fast; cria 12 moedas acima das plataformas

**`UpdatePlaying()`** => atualiza jogador, câmara, inimigos, projéteis, moedas; verifica colisões (projétil↔inimigo: +100/200pts + som; jogador↔inimigo: `TakeDamage()`; jogador↔moeda: +50pts + som); remove inativos com `RemoveAll()`; verifica fim (morte ou zero inimigos) e atualiza HighScore

**`DrawPlaying()`** => background com parallax → plataformas → moedas → inimigos → projéteis → jogador → HUD

**`DrawHUD()`** => barra preta 44px no topo; vidas (3 quadrados vermelhos/escuros); score centrado a dourado; barra de progresso de inimigos; contador de moedas

**`DrawEndScreen(bool win)`** => overlay verde (vitória) ou vermelho (game over); painel central com título, score final, recorde a dourado, score atual a verde (novo recorde) ou branco; instrução R para menu

**`DrawMenu()`** => fundo gradiente; painel de recorde canto superior direito; painel central com título, controlos, botão Enter

**`SaveHighScore()` / `LoadHighScore()`** => `File.WriteAllText` / `File.ReadAllText` em `highscore.txt`

---

## Instruções de Jogo

### Objetivo
Eliminar todos os 8 inimigos da selva sem perder as 3 vidas. Recolhe moedas nas plataformas para aumentar o score.

### Controlos

| Tecla | Ação |
|---|---|
| `A` / `←` | Mover para a esquerda |
| `D` / `→` | Mover para a direita |
| `W` / `↑` / `Espaço` | Saltar |
| `F` / `LeftCtrl` | Disparar |
| `Escape` | Sair do jogo |
| `R` | Voltar ao menu (ecrã de fim) |

### Regras
- O jogador tem **3 vidas**. Ao ser atingido perde uma vida e fica invencível ~1.5 segundos (sprite pisca).
- Os projéteis desaparecem ao fim de **400 pixels** — é necessário aproximar dos inimigos.
- Máximo de **2 projéteis** ativos em simultâneo.
- É possível **passar por baixo** das plataformas elevadas.
- O **recorde** persiste entre sessões. Para resetar, apagar `highscore.txt` em `bin/Debug/net6.0/`.
- No ecrã de fim, o score aparece a **verde** se igualou ou bateu o recorde.

### Pontuação
| Ação | Pontos |
|---|---|
| Eliminar inimigo Normal | 100 |
| Eliminar inimigo Fast | 200 |
| Recolher moeda | 50 |

---

## Como Executar

1. Clonar o repositório:
```
git clone https://github.com/TiagoPG98/Platformer2D.git
```
2. Garantir que **.NET 6.0** e **MonoGame 3.8.1** estão instalados
3. Na pasta do projeto:
```
dotnet tool restore
dotnet run
```

---

## Decisões Técnicas

- **`frameCount` explícito na `Animation`** => a versão original do `MonoGame.Samples/Platformer2D` assumia frames quadrados. Como `Die.png` (12 frames), `Jump.png` (11 frames) e `Run.png` (10 frames) têm números diferentes, adicionou-se `frameCount` como parâmetro obrigatório para calcular `FrameWidth` corretamente

- **`source Rectangle` com `FrameWidth`** => corrigido em `AnimationPlayer.Draw()`. A versão adaptada usava `FrameHeight` onde devia usar `FrameWidth`, causando sprites cortados incorretamente em animações com frames não quadrados

- **One-way platforms com `prevY`** => técnica padrão de platformers 2D presente no `MonoGame.Samples/Platformer2D`; guardar `prevY` antes do movimento vertical permite distinguir "vir de cima" (aterrar) de "vir de baixo" (passar por baixo)

- **Coyote time (8 frames) + Jump buffer (12 frames)** => padrão documentado por Maddy Thorson (Celeste, 2018) para responsividade de saltos; desenvolvido de raiz sem equivalente no MonoGame.Samples ou Sokoban

- **`Math.Abs(velocity.X) > 5f` como deadzone para animação de corrida** => substituição de `velocity.X != 0` para evitar flickering causado por oscilações mínimas de velocidade; valor de `5f` suficientemente pequeno para não ser percetível no movimento

- **`IsFixedTimeStep = true` a 30fps** => implementado para resolver flickering de animação causado por variações no `dt`. Com timestep variável, pequenas diferenças de timing faziam a animação alternar entre Idle e Run. O timestep fixo garante `dt = 1/30` segundos em cada frame, tornando o timing determinístico. O Sokoban do professor não precisa disto por ter movimento discreto sem física contínua

- **HighScore em `highscore.txt`** => mesmo padrão de I/O do Sokoban do professor (`File.ReadAllLines` para níveis); simples, sem dependências externas, persistente entre sessões

- **Textura `pixel` 1×1 branca** => permite desenhar qualquer retângulo colorido sem assets externos; usado para HUD, menus, plataformas, projéteis e moedas; técnica comum em MonoGame académico

- **Parallax a 30%** => rácio escolhido para dar profundidade sem ser excessivo; o background `background_LunarDriftStudios.png` é tileable (sem costuras), repetido em loop horizontal

- **Limite de 2 balas + 400px de alcance** => decisão de design para evitar que o jogador elimine todos os inimigos sem se mover; sem estas limitações o jogo tornava-se trivial (testado durante desenvolvimento)

- **`SpriteEffects` invertido nos sprites** => sprites do jogador e dos monstros têm a face voltada para a esquerda por defeito; o flip é aplicado ao mover para a direita (`FlipHorizontally`) em vez do contrário — comportamento contra-intuitivo mas necessário para os assets específicos do `MonoGame.Samples/Platformer2D`

---

## Bugs Corrigidos durante o Desenvolvimento

**1. Bug das vidas (crítico)**
`isDying` nunca era resetado após `Lives--`, decrementando as vidas em cada frame até 0. Corrigido: `isDying = false` e `dyeTimer = 0` após `Lives--`.

**2. Moonwalk dos inimigos**
`SpriteEffects.FlipHorizontally` aplicado quando `direction < 0` (esquerda) — ao contrário do correto. Os sprites têm face para a esquerda, por isso o flip devia ser quando `direction > 0`.

**3. Sprite do jogador e disparo para a esquerda por defeito**
`facing` inicial era `SpriteEffects.None` (face esquerda) com `facingDir = 1` (direita) — jogador parecia virar para a esquerda mas disparava para a direita. Corrigido: `facing = SpriteEffects.FlipHorizontally` inicialmente.

**4. Jogador não passava por baixo das plataformas**
Resolução vertical aplicava aterragem independentemente da origem. Corrigido com `prevY`.

**5. Delay percetível no salto**
Edge detection com `prevKeys` requeria timing preciso. Corrigido com coyote time (8 frames) e jump buffer (12 frames).

**6. Flickering entre Idle e Run**
`velocity.X != 0` causava trocas de animação por oscilações. Corrigido com deadzone `Math.Abs(velocity.X) > 5f` e `IsFixedTimeStep = true` a 30fps.

**7. Animações partidas — sprite cortado**
`FrameCount = Texture.Width / Texture.Height` dava resultado incorreto para spritesheets não quadradas (`Die.png` com 12 frames calculava 11). `source Rectangle` usava `FrameHeight` em vez de `FrameWidth`. Ambos corrigidos.

**8. Animação de corrida parava**
`animPlayerRun` criado com `isLooping = false`. Corrigido para `true`.

**9. Erro `CS0106 / CS1513` — chaveta em falta**
Operador ternário numa string interpolada (`$"MOEDAS: {(cond ? a : b)}"`) fez o compilador interpretar métodos seguintes como estando dentro de `DrawHUD()`. Corrigido adicionando `}` em falta.

**10. Erro `CS0121` — `Color` ambíguo**
`new Color(accent.R, accent.G, accent.B, 160)` ambíguo entre overloads `int` e `byte`. Corrigido com cast `(byte)160`.

---

## Assets Utilizados e Não Utilizados

### Utilizados
- **Sprites do jogador e monstros + sons:** MonoGame.Samples/Platformer2D — Microsoft — https://github.com/MonoGame/MonoGame.Samples
- **Background de selva:** Lunar Drift Studios — `background_LunarDriftStudios.png`
- **Fonte Arial 14pt:** sistema Windows via `Font.spritefont`

### Não Utilizados
- **`bullets_Master484.png`:** pack de balas de Master484, presente na pasta `Content` como asset reservado para futura substituição visual dos projéteis. Os projéteis atuais são retângulos amarelos com textura `pixel` 1×1.

---

## Créditos

- **Sistema de animação + sprites + sons:** MonoGame.Samples/Platformer2D — Microsoft XNA Community Game Platform — https://github.com/MonoGame/MonoGame.Samples
- **Inspiração para sistema de disparo:** joshberc/SpaceInvaders (TP1) — Licença MIT — https://github.com/joshberc/SpaceInvaders
- **Background de selva:** Lunar Drift Studios — `background_LunarDriftStudios.png`
- **Pack de balas (não utilizado):** Master484 — `bullets_Master484.png`

---

*Trabalho realizado no âmbito da Unidade Curricular de Técnicas de Desenvolvimento de Videojogos — IPCA 2025/2026*
