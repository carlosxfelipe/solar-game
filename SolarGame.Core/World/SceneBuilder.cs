using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SolarGame.World;

/// <summary>
/// Monta a geometria do cenário a partir de caixas coloridas (sem texturas/assets),
/// no mesmo espírito dos blocos do MonoCraft: cada face recebe um sombreamento fixo
/// para dar sensação de volume sem precisar de iluminação.
/// </summary>
public class SceneBuilder
{
    private readonly List<VertexPositionColor> _opaque = new();
    private readonly List<VertexPositionColor> _transparent = new();
    private readonly List<BoundingBox> _colliders = new();
    private readonly List<VertexPositionColor> _pickupVerts = new();
    private readonly List<PickupItem> _pickups = new();

    // Sombreamento por face (igual ao estilo Minecraft)
    private const float ShadeTop = 1.0f;
    private const float ShadeBottom = 0.55f;
    private const float ShadeX = 0.8f;
    private const float ShadeZ = 0.9f;

    [Flags]
    public enum Faces
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4, // -X
        Right = 8, // +X
        Back = 16, // -Z
        Front = 32, // +Z
        All = Top | Bottom | Left | Right | Back | Front,
    }

    /// <summary>Adiciona uma caixa sólida (com colisão).</summary>
    public void Box(Vector3 min, Vector3 max, Color color, Faces faces = Faces.All)
    {
        AddBoxGeometry(_opaque, min, max, color, faces, shaded: true);
        _colliders.Add(new BoundingBox(min, max));
    }

    /// <summary>Adiciona uma caixa apenas visual (sem colisão), ex.: produtos dentro da prateleira.</summary>
    public void Decor(Vector3 min, Vector3 max, Color color, Faces faces = Faces.All)
    {
        AddBoxGeometry(_opaque, min, max, color, faces, shaded: true);
    }

    /// <summary>Caixa sem sombreamento (parece "emitir luz"), ex.: luminárias e letreiros.</summary>
    public void Emissive(Vector3 min, Vector3 max, Color color, Faces faces = Faces.All)
    {
        AddBoxGeometry(_opaque, min, max, color, faces, shaded: false);
    }

    /// <summary>Superfície translúcida (vidro). Desenhada depois dos opacos.</summary>
    public void Glass(Vector3 min, Vector3 max, Color color, bool solid = true)
    {
        AddBoxGeometry(_transparent, min, max, color, Faces.All, shaded: false);
        if (solid)
            _colliders.Add(new BoundingBox(min, max));
    }

    /// <summary>Apenas colisão invisível.</summary>
    public void Collider(Vector3 min, Vector3 max) => _colliders.Add(new BoundingBox(min, max));

    /// <summary>Item que o jogador pode pegar (sem colisão), formado por uma ou mais caixas.</summary>
    public void Pickup(Product product, params PickupPart[] parts)
    {
        int start = _pickupVerts.Count;
        var bounds = new BoundingBox(parts[0].Min, parts[0].Max);
        foreach (var p in parts)
        {
            AddBoxGeometry(_pickupVerts, p.Min, p.Max, p.Color, p.Faces, shaded: true);
            bounds = BoundingBox.CreateMerged(bounds, new BoundingBox(p.Min, p.Max));
        }
        _pickups.Add(new PickupItem(product, bounds, start, _pickupVerts.Count - start));
    }

    /// <summary>Quad horizontal (chão/teto) sem colisão.</summary>
    public void FloorQuad(float x0, float z0, float x1, float z1, float y, Color color)
    {
        var a = new Vector3(x0, y, z0);
        var b = new Vector3(x1, y, z0);
        var c = new Vector3(x1, y, z1);
        var d = new Vector3(x0, y, z1);
        Quad(_opaque, a, b, c, d, color);
    }

    public Scene Build(GraphicsDevice device) =>
        new(
            device,
            _opaque.ToArray(),
            _transparent.ToArray(),
            _colliders,
            new PickupSet(device, _pickups, _pickupVerts.ToArray())
        );

    internal static void AddBoxGeometry(
        List<VertexPositionColor> target,
        Vector3 min,
        Vector3 max,
        Color color,
        Faces faces,
        bool shaded
    )
    {
        Vector3 p000 = new(min.X, min.Y, min.Z);
        Vector3 p100 = new(max.X, min.Y, min.Z);
        Vector3 p010 = new(min.X, max.Y, min.Z);
        Vector3 p110 = new(max.X, max.Y, min.Z);
        Vector3 p001 = new(min.X, min.Y, max.Z);
        Vector3 p101 = new(max.X, min.Y, max.Z);
        Vector3 p011 = new(min.X, max.Y, max.Z);
        Vector3 p111 = new(max.X, max.Y, max.Z);

        Color Shade(float s) =>
            shaded ? new Color(color.ToVector3() * s) * (color.A / 255f) : color;

        if (faces.HasFlag(Faces.Top))
            Quad(target, p010, p110, p111, p011, Shade(ShadeTop));
        if (faces.HasFlag(Faces.Bottom))
            Quad(target, p000, p001, p101, p100, Shade(ShadeBottom));
        if (faces.HasFlag(Faces.Left))
            Quad(target, p000, p010, p011, p001, Shade(ShadeX));
        if (faces.HasFlag(Faces.Right))
            Quad(target, p100, p101, p111, p110, Shade(ShadeX));
        if (faces.HasFlag(Faces.Back))
            Quad(target, p000, p100, p110, p010, Shade(ShadeZ));
        if (faces.HasFlag(Faces.Front))
            Quad(target, p001, p011, p111, p101, Shade(ShadeZ));
    }

    private static void Quad(
        List<VertexPositionColor> target,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Color color
    )
    {
        target.Add(new VertexPositionColor(a, color));
        target.Add(new VertexPositionColor(b, color));
        target.Add(new VertexPositionColor(c, color));
        target.Add(new VertexPositionColor(a, color));
        target.Add(new VertexPositionColor(c, color));
        target.Add(new VertexPositionColor(d, color));
    }
}

/// <summary>Cenário pronto para desenhar: buffers na GPU + lista de colisores.</summary>
public class Scene
{
    // Limite seguro de primitivas por draw call (perfil Reach)
    private const int MaxPrimitivesPerDraw = 60000;

    private readonly VertexBuffer _opaque;
    private readonly VertexBuffer _transparent;
    private readonly int _opaqueTriangles;
    private readonly int _transparentTriangles;

    public IReadOnlyList<BoundingBox> Colliders { get; }
    public PickupSet Pickups { get; }

    public Scene(
        GraphicsDevice device,
        VertexPositionColor[] opaque,
        VertexPositionColor[] transparent,
        List<BoundingBox> colliders,
        PickupSet pickups
    )
    {
        Colliders = colliders;
        Pickups = pickups;

        _opaqueTriangles = opaque.Length / 3;
        if (opaque.Length > 0)
        {
            _opaque = new VertexBuffer(
                device,
                VertexPositionColor.VertexDeclaration,
                opaque.Length,
                BufferUsage.WriteOnly
            );
            _opaque.SetData(opaque);
        }

        _transparentTriangles = transparent.Length / 3;
        if (transparent.Length > 0)
        {
            _transparent = new VertexBuffer(
                device,
                VertexPositionColor.VertexDeclaration,
                transparent.Length,
                BufferUsage.WriteOnly
            );
            _transparent.SetData(transparent);
        }
    }

    public void Draw(GraphicsDevice device, BasicEffect effect)
    {
        // Sem culling: evita problemas de winding e permite ver os dois lados das superfícies
        device.RasterizerState = RasterizerState.CullNone;

        device.BlendState = BlendState.Opaque;
        device.DepthStencilState = DepthStencilState.Default;
        DrawBuffer(device, effect, _opaque, _opaqueTriangles);
        Pickups?.Draw(device, effect);

        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.DepthRead;
        DrawBuffer(device, effect, _transparent, _transparentTriangles);

        device.BlendState = BlendState.Opaque;
        device.DepthStencilState = DepthStencilState.Default;
    }

    internal static void DrawBuffer(
        GraphicsDevice device,
        BasicEffect effect,
        VertexBuffer buffer,
        int triangles
    )
    {
        if (buffer == null || triangles == 0)
            return;

        device.SetVertexBuffer(buffer);
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            for (int start = 0; start < triangles; start += MaxPrimitivesPerDraw)
            {
                int count = Math.Min(MaxPrimitivesPerDraw, triangles - start);
                device.DrawPrimitives(PrimitiveType.TriangleList, start * 3, count);
            }
        }
    }
}
