using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolarGame.World;

namespace SolarGame;

/// <summary>
/// Braço em primeira pessoa no estilo Minecraft: um bloco de "pele" com manga,
/// que balança ao andar, faz o movimento de "soco" ao pegar algo e segura o item selecionado.
/// Desenhado em espaço de câmera, por cima do cenário.
/// </summary>
public class Hand
{
    private static readonly Color Skin = new(222, 170, 128);
    private static readonly Color SleeveColor = new(40, 90, 170);
    private static readonly Color CanTop = new(200, 202, 208);
    private const float SwingDuration = 0.3f;

    private float _swing = -1f; // < 0 = sem animação
    private float _bobPhase;
    private float _bobAmount;
    private float _equip = 1f;
    private string _heldName;
    private Vector2 _sway;

    private readonly List<VertexPositionColor> _arm = new();
    private readonly List<VertexPositionColor> _item = new();

    public Hand()
    {
        // Braço (aponta para -Z = frente; a parte de trás sai da tela)
        SceneBuilder.AddBoxGeometry(_arm, new Vector3(-0.05f, -0.05f, -0.06f), new Vector3(0.05f, 0.05f, 0.30f), Skin, SceneBuilder.Faces.All, true);
        SceneBuilder.AddBoxGeometry(_arm, new Vector3(-0.056f, -0.056f, 0.16f), new Vector3(0.056f, 0.056f, 0.6f), SleeveColor, SceneBuilder.Faces.All, true);
    }

    /// <summary>Animação de "soco"/pegar.</summary>
    public void Swing()
    {
        if (_swing < 0 || _swing > SwingDuration * 0.5f)
            _swing = 0f;
    }

    public void Update(float dt, float speed, bool onGround, Vector2 look, Product? held)
    {
        if (_swing >= 0)
        {
            _swing += dt;
            if (_swing >= SwingDuration)
                _swing = -1f;
        }

        // Balanço ao andar
        float target = onGround ? MathHelper.Clamp(speed / 4f, 0f, 1.6f) : 0f;
        _bobAmount = MathHelper.Lerp(_bobAmount, target, 1f - MathF.Exp(-10f * dt));
        if (onGround && speed > 0.1f)
            _bobPhase += dt * (3f + speed * 1.6f);

        // A mão "atrasa" um pouco em relação ao movimento da câmera
        var swayTarget = Vector2.Clamp(look * 0.0015f, new Vector2(-0.06f), new Vector2(0.06f));
        _sway = Vector2.Lerp(_sway, swayTarget, 1f - MathF.Exp(-12f * dt));

        // Ao trocar de item, a mão desce e sobe de novo
        string name = held?.Name;
        if (name != _heldName)
        {
            _heldName = name;
            _equip = 0f;
            _item.Clear();
            if (held.HasValue)
                BuildItem(_item, held.Value);
        }
        _equip = MathF.Min(1f, _equip + dt * 5f);
    }

    public void Draw(GraphicsDevice device, BasicEffect effect)
    {
        float s = _swing >= 0 ? _swing / SwingDuration : 0f;
        float f = MathF.Sin(s * MathHelper.Pi);
        float f2 = MathF.Sin(MathF.Sqrt(s) * MathHelper.Pi);

        Vector3 pos = new(0.24f, -0.22f, -0.42f);
        pos += new Vector3(MathF.Sin(_bobPhase) * 0.012f, -MathF.Abs(MathF.Cos(_bobPhase)) * 0.018f, 0f) * _bobAmount;
        pos += new Vector3(-_sway.X, _sway.Y, 0f);
        pos += new Vector3(-0.10f * f2, 0.05f * MathF.Sin(MathF.Sqrt(s) * MathHelper.TwoPi) - 0.06f * f, -0.10f * f2);
        pos.Y -= (1f - _equip) * (1f - _equip) * 0.4f;

        bool holding = _item.Count > 0;
        float pitch = (holding ? 0.35f : 0.55f) - f * 0.6f;
        float yaw = 0.45f + f2 * 0.35f;
        Matrix armWorld = Matrix.CreateRotationX(pitch) * Matrix.CreateRotationY(yaw) * Matrix.CreateTranslation(pos);

        var oldView = effect.View;
        var oldProj = effect.Projection;
        var oldWorld = effect.World;

        effect.View = Matrix.Identity;
        effect.Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(70f),
            device.Viewport.AspectRatio,
            0.01f,
            10f
        );

        // A mão sempre aparece por cima do cenário
        device.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1f, 0);
        device.BlendState = BlendState.Opaque;
        device.DepthStencilState = DepthStencilState.Default;
        device.RasterizerState = RasterizerState.CullNone;

        DrawList(device, effect, _arm, armWorld);

        if (holding)
        {
            Matrix itemWorld =
                Matrix.CreateRotationY(-0.4f + f2 * 0.3f)
                * Matrix.CreateRotationX(-f * 0.4f)
                * Matrix.CreateTranslation(pos + new Vector3(-0.03f, 0.03f, -0.04f));
            DrawList(device, effect, _item, itemWorld);
        }

        effect.View = oldView;
        effect.Projection = oldProj;
        effect.World = oldWorld;
    }

    private static void DrawList(GraphicsDevice device, BasicEffect effect, List<VertexPositionColor> verts, Matrix world)
    {
        if (verts.Count == 0)
            return;
        effect.World = world;
        var arr = verts.ToArray();
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleList, arr, 0, arr.Length / 3);
        }
    }

    /// <summary>Geometria da bebida segurada (mesmas proporções das que estão na geladeira).</summary>
    private static void BuildItem(List<VertexPositionColor> t, Product p)
    {
        const float w = 0.09f;
        const float hw = w / 2f;
        float h = p.IsBottle ? 0.3f : 0.13f;
        float y0 = -0.06f;

        SceneBuilder.AddBoxGeometry(t, new Vector3(-hw, y0, -hw), new Vector3(hw, y0 + h, hw), p.Body, SceneBuilder.Faces.All, true);

        // Rótulo envolvendo a embalagem
        const float e = 0.003f;
        var sides = SceneBuilder.Faces.Left | SceneBuilder.Faces.Right | SceneBuilder.Faces.Front | SceneBuilder.Faces.Back;
        SceneBuilder.AddBoxGeometry(t, new Vector3(-hw - e, y0 + h * 0.45f, -hw - e), new Vector3(hw + e, y0 + h * 0.7f, hw + e), p.Accent, sides, true);

        if (p.IsBottle)
            SceneBuilder.AddBoxGeometry(t, new Vector3(-0.015f, y0 + h, -0.015f), new Vector3(0.015f, y0 + h + 0.03f, 0.015f), p.CapColor, SceneBuilder.Faces.All, true);
        else
            SceneBuilder.AddBoxGeometry(t, new Vector3(-hw + 0.005f, y0 + h, -hw + 0.005f), new Vector3(hw - 0.005f, y0 + h + 0.008f, hw - 0.005f), CanTop, SceneBuilder.Faces.All, true);
    }
}
