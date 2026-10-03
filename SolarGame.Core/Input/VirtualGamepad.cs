using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using SolarGame.UI;

namespace SolarGame.Input;

/// <summary>
/// Controle virtual por toque (Android/iOS), herdado do MonoCraft.
/// Metade esquerda da tela = analógico de movimento; metade direita = olhar ao redor.
/// Converte tudo para um GamePadState, assim o Player não precisa saber de touch.
/// </summary>
public class VirtualGamepad
{
    public GamePadState PadState { get; private set; }
    public Point LookDelta { get; private set; }
    public TouchCollection Touches { get; private set; }

    private readonly Texture2D _pixel;
    private Viewport _viewport;

    // Analógico de movimento
    private Vector2? _leftStickOrigin;
    private Vector2 _leftStickCurrent;
    private int? _leftStickTouchId;

    // Câmera (olhar ao redor)
    private Vector2 _lastLookPosition;
    private int? _lookTouchId;

    // Botões
    private readonly Dictionary<Buttons, Rectangle> _buttonAreas = new();

    public VirtualGamepad(Texture2D pixel)
    {
        _pixel = pixel;
    }

    public void Update(Viewport viewport)
    {
        _viewport = viewport;
        Touches = TouchPanel.GetState();
        int width = viewport.Width;
        int height = viewport.Height;

        LookDelta = Point.Zero;
        float leftThumbX = 0,
            leftThumbY = 0;
        Buttons pressedButtons = 0;

        int btnSize = Math.Min(width, height) / 8;
        int padding = 20;

        _buttonAreas.Clear();

        // Canto Superior Esquerdo: menu (Start)
        _buttonAreas[Buttons.Start] = new Rectangle(padding, padding, btnSize, btnSize);

        // Canto Inferior Direito: Pular (A), Correr (LeftStick)
        _buttonAreas[Buttons.A] = new Rectangle(
            width - btnSize - padding,
            height - btnSize - padding,
            btnSize,
            btnSize
        );
        _buttonAreas[Buttons.LeftStick] = new Rectangle(
            width - btnSize * 2 - padding * 2,
            height - btnSize - padding,
            btnSize,
            btnSize
        );
        // Pegar item (X), acima do Pular
        _buttonAreas[Buttons.X] = new Rectangle(
            width - btnSize - padding,
            height - btnSize * 2 - padding * 2,
            btnSize,
            btnSize
        );

        bool leftStickActive = false;

        foreach (var touch in Touches)
        {
            if (touch.State == TouchLocationState.Released)
            {
                if (_leftStickTouchId == touch.Id)
                    _leftStickTouchId = null;
                if (_lookTouchId == touch.Id)
                    _lookTouchId = null;
                continue;
            }

            bool hitButton = false;
            foreach (var kvp in _buttonAreas)
            {
                if (kvp.Value.Contains(touch.Position))
                {
                    pressedButtons |= kvp.Key;
                    hitButton = true;
                }
            }

            if (hitButton)
                continue;

            if (touch.Position.X < width / 2)
            {
                if (_leftStickTouchId == null && touch.State == TouchLocationState.Pressed)
                {
                    _leftStickTouchId = touch.Id;
                    _leftStickOrigin = touch.Position;
                }

                if (_leftStickTouchId == touch.Id && _leftStickOrigin.HasValue)
                {
                    leftStickActive = true;
                    _leftStickCurrent = touch.Position;
                    Vector2 delta = _leftStickCurrent - _leftStickOrigin.Value;
                    float maxDist = btnSize * 1.5f;
                    if (delta.Length() > maxDist)
                    {
                        delta.Normalize();
                        delta *= maxDist;
                    }
                    leftThumbX = delta.X / maxDist;
                    leftThumbY = -delta.Y / maxDist;
                }
            }
            else
            {
                if (_lookTouchId == null && touch.State == TouchLocationState.Pressed)
                {
                    _lookTouchId = touch.Id;
                    _lastLookPosition = touch.Position;
                }

                if (_lookTouchId == touch.Id && touch.State == TouchLocationState.Moved)
                {
                    Vector2 delta = touch.Position - _lastLookPosition;
                    LookDelta = new Point((int)delta.X, (int)delta.Y);
                    _lastLookPosition = touch.Position;
                }
            }
        }

        if (!leftStickActive)
            _leftStickOrigin = null;

        PadState = new GamePadState(
            new GamePadThumbSticks(new Vector2(leftThumbX, leftThumbY), Vector2.Zero),
            new GamePadTriggers(0f, 0f),
            new GamePadButtons(pressedButtons),
            new GamePadDPad(
                ButtonState.Released,
                ButtonState.Released,
                ButtonState.Released,
                ButtonState.Released
            )
        );
    }

    public void Draw(SpriteBatch sb, TextRenderer textRenderer)
    {
        foreach (var kvp in _buttonAreas)
        {
            Rectangle r = kvp.Value;
            sb.Draw(_pixel, r, Color.White * 0.2f);
            sb.Draw(
                _pixel,
                new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4),
                Color.Black * 0.3f
            );

            string label = kvp.Key switch
            {
                Buttons.A => "JUMP",
                Buttons.LeftStick => "RUN",
                Buttons.X => "GRAB",
                Buttons.Start => "MENU",
                _ => kvp.Key.ToString(),
            };

            int scale = Math.Max(1, r.Width / (label.Length * 4 + 4));
            int textWidth = label.Length * 4 * scale;
            int textHeight = 5 * scale;
            int tx = r.X + (r.Width - textWidth) / 2;
            int ty = r.Y + (r.Height - textHeight) / 2;
            textRenderer.DrawString(sb, label, tx, ty, scale, Color.LightGray, drawShadow: false);
        }

        if (_leftStickOrigin.HasValue)
        {
            var origin = _leftStickOrigin.Value;
            var current = _leftStickCurrent;
            int r1 = _viewport.Width / 12;
            int r2 = r1 / 2;

            sb.Draw(
                _pixel,
                new Rectangle((int)origin.X - r1, (int)origin.Y - r1, r1 * 2, r1 * 2),
                Color.Gray * 0.2f
            );
            sb.Draw(
                _pixel,
                new Rectangle((int)current.X - r2, (int)current.Y - r2, r2 * 2, r2 * 2),
                Color.White * 0.4f
            );
        }
    }
}
