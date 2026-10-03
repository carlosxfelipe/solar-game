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
    private Hand _hand;
    private readonly Inventory _inventory = new();

    // Item sob a mira (dentro do alcance)
    private PickupItem _target;
    // Lugar vazio sob a mira, onde o item selecionado pode ser devolvido
    private PickupItem _placeTarget;
    private const float ReachDistance = 3f;

    // Mensagens rápidas no HUD
    private string _message;
    private float _messageTimer;
    private string _selectedName;
    private float _selectedNameTimer;

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
        _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24;
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
        Window.Title = "Supermarket Simulator";
#endif
    }

    protected override void Initialize()
    {
        base.Initialize();

        _prevKeyboard = Keyboard.GetState();
        _prevMouse = Mouse.GetState();
        _prevPad = GamePad.GetState(PlayerIndex.One);

#if !(ANDROID || IOS)
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
        _hand = new Hand();
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
                || vPad.Buttons.LeftStick == ButtonState.Pressed || vPad.Buttons.Start == ButtonState.Pressed
                || vPad.Buttons.X == ButtonState.Pressed || vPad.Buttons.Y == ButtonState.Pressed)
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
        bool justUnpaused = false;
        if (_paused && !IsMobile && IsActive)
        {
            bool inside = mouse.X >= 0 && mouse.Y >= 0 && mouse.X < GraphicsDevice.Viewport.Width && mouse.Y < GraphicsDevice.Viewport.Height;
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released && inside;
            bool padA = pad.Buttons.A == ButtonState.Pressed && _prevPad.Buttons.A == ButtonState.Released;
            if (click || padA)
            {
                SetPaused(false);
                justUnpaused = true; // o mesmo clique não deve pegar um item
            }
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

            UpdateHotbarSelection(keyboard, mouse, pad);

            // Item sob a mira
            UpdateTargets();

            bool grab =
                !justUnpaused
                && (
                    (!IsMobile && mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                    || (pad.Buttons.X == ButtonState.Pressed && _prevPad.Buttons.X == ButtonState.Released)
                    || (pad.Triggers.Right > 0.5f && _prevPad.Triggers.Right <= 0.5f)
                );

            bool place =
                !justUnpaused
                && (
                    (!IsMobile && mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released)
                    || (pad.Buttons.Y == ButtonState.Pressed && _prevPad.Buttons.Y == ButtonState.Released)
                    || (pad.Triggers.Left > 0.5f && _prevPad.Triggers.Left <= 0.5f)
                );

            if (grab)
                TryGrab();
            else if (place)
                TryPlace();
        }

        // Nome do item selecionado aparece por alguns segundos quando muda
        var held = _inventory.SelectedStack;
        if (held?.Product.Name != _selectedName)
        {
            _selectedName = held?.Product.Name;
            _selectedNameTimer = _selectedName != null ? 2f : 0f;
        }
        _selectedNameTimer = MathF.Max(0f, _selectedNameTimer - dt);
        _messageTimer = MathF.Max(0f, _messageTimer - dt);

        _hand.Update(dt, _player.HorizontalSpeed, _player.OnGround, new Vector2(lookX, lookY), held?.Product);

        _prevKeyboard = keyboard;
        _prevMouse = Mouse.GetState();
        _prevPad = pad;

        base.Update(gameTime);
    }

    private void TryGrab()
    {
        _hand.Swing();
        if (_target == null)
            return;

        int slot = _inventory.TryAdd(_target.Product);
        if (slot < 0)
        {
            _message = "INVENTORY FULL";
            _messageTimer = 1.5f;
            return;
        }

        _scene.Pickups.Take(_target);
        _inventory.Select(slot);
        UpdateTargets();
    }

    private void TryPlace()
    {
        var held = _inventory.SelectedStack;
        if (held == null || _placeTarget == null)
            return;

        if (!_scene.Pickups.Place(_placeTarget, held.Product))
            return;

        _hand.Swing();
        _inventory.RemoveOneFromSelected();
        UpdateTargets();
    }

    /// <summary>
    /// Atualiza o item sob a mira (para pegar) e o lugar vazio (para devolver).
    /// O lugar vazio só vale se estiver na frente de qualquer item cheio.
    /// </summary>
    private void UpdateTargets()
    {
        var ray = new Ray(_player.EyePosition, _player.Forward);
        _target = _scene.Pickups.Raycast(ray, ReachDistance, out float grabDistance);

        _placeTarget = null;
        if (_inventory.SelectedStack != null)
        {
            var empty = _scene.Pickups.RaycastEmpty(ray, ReachDistance, out float emptyDistance);
            if (empty != null && (_target == null || emptyDistance <= grabDistance))
                _placeTarget = empty;
        }
    }

    private void UpdateHotbarSelection(KeyboardState keyboard, MouseState mouse, GamePadState pad)
    {
        // Teclas 1-9
        for (int i = 0; i < Inventory.SlotCount; i++)
        {
            var key = Keys.D1 + i;
            if (keyboard.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key))
                _inventory.Select(i);
        }

        // Roda do mouse
        if (!IsMobile)
        {
            int wheel = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
            if (wheel != 0)
                _inventory.Scroll(wheel > 0 ? -1 : 1);
        }

        // LB / RB no controle
        if (pad.Buttons.LeftShoulder == ButtonState.Pressed && _prevPad.Buttons.LeftShoulder == ButtonState.Released)
            _inventory.Scroll(-1);
        if (pad.Buttons.RightShoulder == ButtonState.Pressed && _prevPad.Buttons.RightShoulder == ButtonState.Released)
            _inventory.Scroll(1);
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
            0.1f,
            200f
        );
        _effect.World = Matrix.Identity;

        _scene.Draw(GraphicsDevice, _effect);

        if (_target != null && !_paused)
            PickupSet.DrawOutline(GraphicsDevice, _effect, _target.Bounds, new Color(20, 20, 20));

        // Prévia de onde o item selecionado será devolvido
        var heldStack = _inventory.SelectedStack;
        if (_placeTarget != null && heldStack != null && !_paused)
            PickupSet.DrawOutline(GraphicsDevice, _effect, _placeTarget.BoundsFor(heldStack.Product), Color.White);

        _hand.Draw(GraphicsDevice, _effect);

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
            DrawCentered("SUPERMARKET SIMULATOR", cy - titleScale * 12, titleScale, new Color(230, 40, 60));
            DrawCentered(
                IsMobile ? "TAP MENU TO CONTINUE" : "CLICK TO PLAY",
                cy + titleScale * 2,
                textScale,
                Color.White
            );
            if (!IsMobile)
            {
                DrawCentered("WASD: MOVE   MOUSE: LOOK   SHIFT: RUN   SPACE: JUMP", cy + titleScale * 6, Math.Max(1, textScale / 2 + 1), Color.LightGray);
                DrawCentered("LEFT CLICK: GRAB   RIGHT CLICK: PUT BACK   1-9 / WHEEL: SELECT ITEM", cy + titleScale * 8, Math.Max(1, textScale / 2 + 1), Color.LightGray);
                DrawCentered("ESC: PAUSE / QUIT", cy + titleScale * 10, Math.Max(1, textScale / 2 + 1), Color.Gray);
            }
        }
        else
        {
            // Mira
            _spriteBatch.Draw(_pixel, new Rectangle(cx - 8, cy - 1, 16, 2), Color.White * 0.8f);
            _spriteBatch.Draw(_pixel, new Rectangle(cx - 1, cy - 8, 2, 16), Color.White * 0.8f);

            int s = Math.Max(2, h / 300);

            // Nome do item sob a mira
            if (_target != null)
                DrawCentered(_target.Product.Name, cy + 16, s, Color.White);

            if (_messageTimer > 0 && _message != null)
                DrawCentered(_message, cy + 16 + s * 8, s, new Color(255, 90, 90) * MathF.Min(1f, _messageTimer * 2f));

            // Total de itens (canto superior direito)
            string total = "ITEMS: " + _inventory.TotalItems;
            _text.DrawString(_spriteBatch, total, w - total.Length * 4 * s - 16, 16, s, Color.White);
        }

        DrawHotbar(w, h);

        if (IsMobile)
            _virtualGamepad.Draw(_spriteBatch, _text);

        _spriteBatch.End();
    }

    private void DrawCentered(string text, int y, int scale, Color color)
    {
        int width = text.Length * 4 * scale;
        _text.DrawString(_spriteBatch, text, GraphicsDevice.Viewport.Width / 2 - width / 2, y, scale, color);
    }

    /// <summary>Hotbar estilo Minecraft na parte de baixo da tela.</summary>
    private void DrawHotbar(int w, int h)
    {
        int slot = Math.Clamp(h / 13, 36, 72);
        int total = Inventory.SlotCount * slot;
        int x0 = w / 2 - total / 2;
        int y0 = h - slot - (IsMobile ? slot / 3 : 12);
        int textScale = Math.Max(2, slot / 22);

        // Nome do item selecionado (some após alguns segundos)
        if (_selectedNameTimer > 0 && _selectedName != null)
            DrawCentered(_selectedName, y0 - textScale * 9, textScale, Color.White * MathF.Min(1f, _selectedNameTimer * 2f));

        _spriteBatch.Draw(_pixel, new Rectangle(x0 - 4, y0 - 4, total + 8, slot + 8), Color.Black * 0.5f);

        for (int i = 0; i < Inventory.SlotCount; i++)
        {
            var r = new Rectangle(x0 + i * slot, y0, slot, slot);
            _spriteBatch.Draw(_pixel, new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), new Color(70, 70, 78) * 0.8f);

            var stack = _inventory[i];
            if (stack != null)
            {
                DrawItemIcon(stack.Product, r);
                if (stack.Count > 1)
                    _text.DrawNumber(_spriteBatch, stack.Count, r.Right - 3, r.Bottom - 5 * textScale - 4, textScale);
            }

            if (i == _inventory.Selected)
                DrawRectOutline(new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), 3, Color.White);
        }
    }

    /// <summary>Ícone "pixelado" da bebida: garrafa alta com tampa ou lata baixa.</summary>
    private void DrawItemIcon(Product p, Rectangle r)
    {
        int bw = (int)(r.Width * (p.IsBottle ? 0.3f : 0.4f));
        int bh = (int)(r.Height * (p.IsBottle ? 0.6f : 0.45f));
        int bx = r.Center.X - bw / 2;
        int by = r.Bottom - (int)(r.Height * 0.15f) - bh;

        var shade = Color.Lerp(p.Body, Color.Black, 0.3f);
        _spriteBatch.Draw(_pixel, new Rectangle(bx, by, bw, bh), p.Body);
        _spriteBatch.Draw(_pixel, new Rectangle(bx + bw * 2 / 3, by, bw - bw * 2 / 3, bh), shade);
        _spriteBatch.Draw(_pixel, new Rectangle(bx, by + (int)(bh * 0.4f), bw, Math.Max(2, bh / 5)), p.Accent);

        if (p.IsBottle)
        {
            int cw = Math.Max(2, bw / 2);
            int ch = Math.Max(2, r.Height / 12);
            _spriteBatch.Draw(_pixel, new Rectangle(r.Center.X - cw / 2, by - ch, cw, ch), p.Accent);
        }
        else
        {
            _spriteBatch.Draw(_pixel, new Rectangle(bx, by - 2, bw, 3), new Color(200, 202, 208));
        }
    }

    private void DrawRectOutline(Rectangle r, int t, Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, t), color);
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - t, r.Width, t), color);
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, t, r.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(r.Right - t, r.Y, t, r.Height), color);
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
