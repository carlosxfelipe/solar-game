using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SolarGame;

/// <summary>
/// Jogador em primeira pessoa. Lógica de câmera/movimento herdada do MonoCraft,
/// mas com colisão contra caixas (AABB) em vez de blocos voxel.
/// </summary>
public class Player
{
    private const float Width = 0.6f;
    private const float PlayerHeight = 1.8f;
    private const float EyeHeight = 1.65f;

    private const float Gravity = -24f;
    private const float JumpSpeed = 7.0f;
    private const float WalkSpeed = 4.0f;
    private const float SprintSpeed = 12.0f;
    private const float MouseSensitivity = 0.0025f;
    private const float StickLookSensitivity = 2.8f; // rad/s

    private readonly IReadOnlyList<BoundingBox> _colliders;

    public Vector3 Position; // pé do jogador
    public float Yaw;
    public float Pitch;

    private Vector3 _velocity;
    private bool _onGround;

    public Vector3 EyePosition => Position + new Vector3(0, EyeHeight, 0);

    public float HorizontalSpeed => new Vector2(_velocity.X, _velocity.Z).Length();
    public bool OnGround => _onGround;

    public Vector3 Forward =>
        new(
            MathF.Sin(Yaw) * MathF.Cos(Pitch),
            MathF.Sin(Pitch),
            -MathF.Cos(Yaw) * MathF.Cos(Pitch)
        );

    public Player(IReadOnlyList<BoundingBox> colliders, Vector3 spawnPosition, float yaw = 0f)
    {
        _colliders = colliders;
        Position = spawnPosition;
        Yaw = yaw;
    }

    public void Update(
        float dt,
        KeyboardState keyboard,
        int mouseDeltaX,
        int mouseDeltaY,
        GamePadState pad
    )
    {
        // Rotação da câmera: mouse/touch + right stick
        Yaw += mouseDeltaX * MouseSensitivity + pad.ThumbSticks.Right.X * StickLookSensitivity * dt;
        Pitch -= mouseDeltaY * MouseSensitivity;
        Pitch += pad.ThumbSticks.Right.Y * StickLookSensitivity * dt;
        Pitch = MathHelper.Clamp(Pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);

        // Direções no plano XZ
        var forward = new Vector3(MathF.Sin(Yaw), 0, -MathF.Cos(Yaw));
        var right = new Vector3(MathF.Cos(Yaw), 0, MathF.Sin(Yaw));

        Vector3 move = Vector3.Zero;
        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
            move += forward;
        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
            move -= forward;
        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
            move += right;
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
            move -= right;

        // Left stick (gamepad físico ou virtual)
        float lx = pad.ThumbSticks.Left.X;
        float ly = pad.ThumbSticks.Left.Y;
        if (MathF.Abs(lx) > 0.15f || MathF.Abs(ly) > 0.15f)
        {
            move += forward * ly;
            move += right * lx;
        }

        if (move.LengthSquared() > 1f)
            move.Normalize();

        bool sprint =
            keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift) || pad.Buttons.LeftStick == ButtonState.Pressed;
        float speed = sprint ? SprintSpeed : WalkSpeed;

        _velocity.X = move.X * speed;
        _velocity.Z = move.Z * speed;

        bool wantJump = keyboard.IsKeyDown(Keys.Space) || pad.Buttons.A == ButtonState.Pressed;

        _velocity.Y += Gravity * dt;
        _velocity.Y = MathF.Max(_velocity.Y, -50f);
        if (wantJump && _onGround)
        {
            _velocity.Y = JumpSpeed;
            _onGround = false;
        }

        // Movimento com colisão (eixo por eixo)
        MoveAxis(_velocity.X * dt, 0);
        MoveAxis(_velocity.Y * dt, 1);
        MoveAxis(_velocity.Z * dt, 2);

        // Segurança: se atravessar o chão por algum motivo, volta pra cima
        if (Position.Y < -5f)
        {
            Position.Y = 0.5f;
            _velocity = Vector3.Zero;
        }
    }

    private void MoveAxis(float amount, int axis)
    {
        if (amount == 0)
            return;

        Vector3 newPos = Position;
        switch (axis)
        {
            case 0:
                newPos.X += amount;
                break;
            case 1:
                newPos.Y += amount;
                break;
            case 2:
                newPos.Z += amount;
                break;
        }

        if (!Collides(newPos))
        {
            Position = newPos;
            if (axis == 1)
                _onGround = false;
        }
        else if (axis == 1)
        {
            if (amount < 0)
                _onGround = true;
            _velocity.Y = 0;
        }
    }

    private BoundingBox GetBounds(Vector3 pos)
    {
        float half = Width / 2f;
        return new BoundingBox(
            new Vector3(pos.X - half, pos.Y, pos.Z - half),
            new Vector3(pos.X + half, pos.Y + PlayerHeight, pos.Z + half)
        );
    }

    private bool Collides(Vector3 pos)
    {
        var box = GetBounds(pos);
        foreach (var c in _colliders)
        {
            // Interseção estrita (encostar não conta), para não "grudar" nas paredes
            if (
                box.Min.X < c.Max.X
                && box.Max.X > c.Min.X
                && box.Min.Y < c.Max.Y
                && box.Max.Y > c.Min.Y
                && box.Min.Z < c.Max.Z
                && box.Max.Z > c.Min.Z
            )
                return true;
        }
        return false;
    }
}
