using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SolarGame.World;

/// <summary>Uma parte (caixa) da geometria de um item pegável.</summary>
public readonly record struct PickupPart(
    Vector3 Min,
    Vector3 Max,
    Color Color,
    SceneBuilder.Faces Faces = SceneBuilder.Faces.All
);

/// <summary>Item que pode ser pego pelo jogador (ex.: uma garrafa na geladeira).</summary>
public class PickupItem
{
    public Product Product { get; }
    public BoundingBox Bounds { get; }
    public bool Taken { get; internal set; }

    internal int VertexStart { get; }
    internal int VertexCount { get; }

    internal PickupItem(Product product, BoundingBox bounds, int vertexStart, int vertexCount)
    {
        Product = product;
        Bounds = bounds;
        VertexStart = vertexStart;
        VertexCount = vertexCount;
    }
}

/// <summary>
/// Conjunto de itens pegáveis. Todos ficam em um único vertex buffer; quando um item
/// é pego, só o trecho dele no buffer é sobrescrito com vértices degenerados (invisíveis),
/// sem precisar reconstruir a cena.
/// </summary>
public class PickupSet
{
    private readonly List<PickupItem> _items;
    private readonly VertexBuffer _buffer;
    private readonly int _triangles;
    private VertexPositionColor[] _blank = Array.Empty<VertexPositionColor>();

    public IReadOnlyList<PickupItem> Items => _items;

    public PickupSet(GraphicsDevice device, List<PickupItem> items, VertexPositionColor[] vertices)
    {
        _items = items;
        _triangles = vertices.Length / 3;
        if (vertices.Length > 0)
        {
            _buffer = new VertexBuffer(
                device,
                VertexPositionColor.VertexDeclaration,
                vertices.Length,
                BufferUsage.WriteOnly
            );
            _buffer.SetData(vertices);
        }
    }

    /// <summary>Retorna o item mais próximo atingido pelo raio (ou null).</summary>
    public PickupItem Raycast(Ray ray, float maxDistance)
    {
        PickupItem best = null;
        float bestDistance = maxDistance;
        foreach (var item in _items)
        {
            if (item.Taken)
                continue;
            float? d = item.Bounds.Intersects(ray);
            if (d.HasValue && d.Value < bestDistance)
            {
                bestDistance = d.Value;
                best = item;
            }
        }
        return best;
    }

    /// <summary>Remove o item do mundo.</summary>
    public void Take(PickupItem item)
    {
        if (item == null || item.Taken)
            return;
        item.Taken = true;

        if (_buffer == null)
            return;
        if (_blank.Length < item.VertexCount)
            _blank = new VertexPositionColor[item.VertexCount];

        int stride = VertexPositionColor.VertexDeclaration.VertexStride;
        _buffer.SetData(item.VertexStart * stride, _blank, 0, item.VertexCount, stride);
    }

    public void Draw(GraphicsDevice device, BasicEffect effect) =>
        Scene.DrawBuffer(device, effect, _buffer, _triangles);

    /// <summary>Contorno (wireframe) ao redor de uma caixa, como a seleção de blocos do Minecraft.</summary>
    public static void DrawOutline(GraphicsDevice device, BasicEffect effect, BoundingBox box, Color color)
    {
        const float grow = 0.004f;
        var b = new BoundingBox(box.Min - new Vector3(grow), box.Max + new Vector3(grow));
        var c = b.GetCorners();
        int[] edges = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };
        var verts = new VertexPositionColor[edges.Length];
        for (int i = 0; i < edges.Length; i++)
            verts[i] = new VertexPositionColor(c[edges[i]], color);

        device.BlendState = BlendState.Opaque;
        device.DepthStencilState = DepthStencilState.Default;
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, edges.Length / 2);
        }
    }
}
