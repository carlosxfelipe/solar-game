using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SolarGame.Input;
using SolarGame.UI;
using SolarGame.World;

namespace SolarGame;

/// <summary>
/// Jogo compartilhado entre Desktop, Android e iOS.
/// Os projetos de plataforma só instanciam esta classe e chamam Run().
/// </summary>
public class Game1 : Game
{
#if ANDROID || IOS
    private static readonly bool IsMobile = true;
#else
    private static readonly bool IsMobile = false;
#endif

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private BasicEffect _effect;
    private Texture2D _pixel;
    private TextRenderer _text;
    private VirtualGamepad _virtualGamepad;

    private Scene _scene;
    private Player _player;

    private KeyboardState _prevKeyboard;
    private MouseState _prevMouse;
    private GamePadState _prevPad;

    // Desktop: o jogo fica "pausado" enquanto o mouse não estiver capturado
    private bool _paused = true;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.SynchronizeWithVerticalRetrace = true;
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        IsFixedTimeStep = false;

#if ANDROID || IOS
        _graphics.IsFullScreen = true;
        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        _graphics.SupportedOrientations =
            DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        _paused = false;
#else
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        Window.AllowUserResizing = true;
        Window.Title = "Solar Game";
        Window.ClientSizeChanged += OnWindowClientSizeChanged;
#endif
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _text = new TextRenderer(_pixel);
        _virtualGamepad = new VirtualGamepad(_pixel);

        _effect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = false,
            FogEnabled = true,
            FogColor = new Vector3(0.85f, 0.87f, 0.9f),
            FogStart = 25f,
            FogEnd = 80f,
        };

        _scene = Supermarket.Build(GraphicsDevice);
        _player = new Player(_scene.Colliders, Supermarket.SpawnPosition);
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.1f);
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        int lookX = 0,
            lookY = 0;

        if (IsMobile)
        {
            _virtualGamepad.Update(GraphicsDevice.Viewport);
            var vPad = _virtualGamepad.PadState;
            if (vPad.ThumbSticks.Left != Vector2.Zero || vPad.Buttons.A == ButtonState.Pressed
                || vPad.Buttons.LeftStick == ButtonState.Pressed || vPad.Buttons.Start == ButtonState.Pressed)
                pad = vPad;

            lookX = _virtualGamepad.LookDelta.X * 2;
            lookY = _virtualGamepad.LookDelta.Y * 2;
        }

        bool escPressed =
            (keyboard.IsKeyDown(Keys.Escape) && _prevKeyboard.IsKeyUp(Keys.Escape))
            || (pad.Buttons.Start == ButtonState.Pressed && _prevPad.Buttons.Start == ButtonState.Released);

        if (escPressed)
        {
            if (!_paused)
                SetPaused(true);
            else if (!IsMobile)
                Exit();
            else
                SetPaused(false);
        }

        // Desktop: clique (ou Enter/A) para começar/voltar a jogar
        if (_paused && !IsMobile && IsActive)
        {
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            bool enter = keyboard.IsKeyDown(Keys.Enter) && _prevKeyboard.IsKeyUp(Keys.Enter);
            bool padA = pad.Buttons.A == ButtonState.Pressed && _prevPad.Buttons.A == ButtonState.Released;
            if (click || enter || padA)
                SetPaused(false);
        }

        if (!_paused)
        {
            if (!IsMobile && IsActive)
            {
                var center = GetWindowCenter();
                lookX = mouse.X - center.X;
                lookY = mouse.Y - center.Y;
                CenterMouse();
            }

            _player.Update(dt, keyboard, lookX, lookY, pad);
        }

        _prevKeyboard = keyboard;
        _prevMouse = Mouse.GetState();
        _prevPad = pad;

        base.Update(gameTime);
    }

    private void SetPaused(bool paused)
    {
        _paused = paused;
        if (IsMobile)
            return;

        IsMouseVisible = paused;
        if (!paused)
            CenterMouse();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(150, 195, 240)); // céu (visto pela fachada de vidro)

        _effect.View = Matrix.CreateLookAt(
            _player.EyePosition,
            _player.EyePosition + _player.Forward,
            Vector3.Up
        );
        _effect.Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(70f),
            GraphicsDevice.Viewport.AspectRatio,
            0.05f,
            200f
        );
        _effect.World = Matrix.Identity;

        _scene.Draw(GraphicsDevice, _effect);

        DrawHud();

        base.Draw(gameTime);
    }

    private void DrawHud()
    {
        int w = GraphicsDevice.Viewport.Width;
        int h = GraphicsDevice.Viewport.Height;
        int cx = w / 2;
        int cy = h / 2;

        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);

        if (_paused)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), Color.Black * 0.6f);

            int titleScale = Math.Max(2, h / 80);
            int textScale = Math.Max(1, titleScale / 2);
            DrawCentered("SOLAR GAME", cy - titleScale * 12, titleScale, new Color(230, 40, 60));
            DrawCentered(
                IsMobile ? "TAP MENU TO CONTINUE" : "CLICK TO PLAY",
                cy + titleScale * 2,
                textScale,
                Color.White
            );
            if (!IsMobile)
            {
                DrawCentered("WASD: MOVE   MOUSE: LOOK   SHIFT: RUN   SPACE: JUMP", cy + titleScale * 6, Math.Max(1, textScale / 2 + 1), Color.LightGray);
                DrawCentered("ESC: PAUSE / QUIT", cy + titleScale * 9, Math.Max(1, textScale / 2 + 1), Color.Gray);
            }
        }
        else
        {
            // Mira
            _spriteBatch.Draw(_pixel, new Rectangle(cx - 8, cy - 1, 16, 2), Color.White * 0.8f);
            _spriteBatch.Draw(_pixel, new Rectangle(cx - 1, cy - 8, 2, 16), Color.White * 0.8f);
        }

        if (IsMobile)
            _virtualGamepad.Draw(_spriteBatch, _text);

        _spriteBatch.End();
    }

    private void DrawCentered(string text, int y, int scale, Color color)
    {
        int width = text.Length * 4 * scale;
        _text.DrawString(_spriteBatch, text, GraphicsDevice.Viewport.Width / 2 - width / 2, y, scale, color);
    }

    private Point GetWindowCenter() =>
        new(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);

    private void CenterMouse()
    {
        var center = GetWindowCenter();
        Mouse.SetPosition(center.X, center.Y);
    }

    private void OnWindowClientSizeChanged(object sender, EventArgs e)
    {
        Window.ClientSizeChanged -= OnWindowClientSizeChanged;

        _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
        _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
        _graphics.ApplyChanges();

        Window.ClientSizeChanged += OnWindowClientSizeChanged;
    }
}
