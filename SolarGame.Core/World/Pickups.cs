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
    public Product Product { get; internal set; }
    public BoundingBox Bounds { get; internal set; }
    public bool Taken { get; internal set; }

    /// <summary>Espaço ocupado pelo "lugar" do item (usado para mirar quando está vazio).</summary>
    public BoundingBox SlotBounds { get; }

    /// <summary>Gera a geometria de um produto neste lugar (null = não aceita devolução).</summary>
    internal Func<Product, PickupPart[]> Builder { get; }

    public bool CanPlace => Builder != null;

    internal int VertexStart { get; }
    internal int VertexCount { get; }

    internal PickupItem(
        Product product,
        BoundingBox bounds,
        int vertexStart,
        int vertexCount,
        Func<Product, PickupPart[]> builder = null,
        BoundingBox? slotBounds = null
    )
    {
        Product = product;
        Bounds = bounds;
        VertexStart = vertexStart;
        VertexCount = vertexCount;
        Builder = builder;
        SlotBounds = slotBounds ?? bounds;
    }

    /// <summary>Caixa que o produto ocuparia neste lugar.</summary>
    public BoundingBox BoundsFor(Product product) =>
        Builder == null ? Bounds : PickupSet.Merge(Builder(product));
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
    public PickupItem Raycast(Ray ray, float maxDistance) => Raycast(ray, maxDistance, out _);

    public PickupItem Raycast(Ray ray, float maxDistance, out float distance)
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
        distance = bestDistance;
        return best;
    }

    /// <summary>Retorna o lugar vazio mais próximo atingido pelo raio (ou null).</summary>
    public PickupItem RaycastEmpty(Ray ray, float maxDistance, out float distance)
    {
        PickupItem best = null;
        float bestDistance = maxDistance;
        foreach (var item in _items)
        {
            if (!item.Taken || !item.CanPlace)
                continue;
            float? d = item.SlotBounds.Intersects(ray);
            if (d.HasValue && d.Value < bestDistance)
            {
                bestDistance = d.Value;
                best = item;
            }
        }
        distance = bestDistance;
        return best;
    }

    /// <summary>Coloca um produto de volta em um lugar vazio. Retorna false se não couber.</summary>
    public bool Place(PickupItem item, Product product)
    {
        if (item == null || !item.Taken || !item.CanPlace)
            return false;

        var parts = item.Builder(product);
        var verts = new List<VertexPositionColor>(item.VertexCount);
        foreach (var p in parts)
            SceneBuilder.AddBoxGeometry(verts, p.Min, p.Max, p.Color, p.Faces, shaded: true);
        if (verts.Count > item.VertexCount)
            return false;

        // Completa o trecho com vértices degenerados, caso a nova geometria seja menor
        while (verts.Count < item.VertexCount)
            verts.Add(default);

        if (_buffer != null)
        {
            int stride = VertexPositionColor.VertexDeclaration.VertexStride;
            _buffer.SetData(item.VertexStart * stride, verts.ToArray(), 0, item.VertexCount, stride);
        }

        item.Product = product;
        item.Bounds = Merge(parts);
        item.Taken = false;
        return true;
    }

    internal static BoundingBox Merge(PickupPart[] parts)
    {
        var bounds = new BoundingBox(parts[0].Min, parts[0].Max);
        foreach (var p in parts)
            bounds = BoundingBox.CreateMerged(bounds, new BoundingBox(p.Min, p.Max));
        return bounds;
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
