using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SolarGame.World;

/// <summary>
/// Layout do supermercado (em metros). Eixo X = largura, Z = profundidade, Y = altura.
/// A entrada fica em Z alto (frente) e as geladeiras na parede do fundo (Z = 0).
/// </summary>
public static class Supermarket
{
    public const float Width = 30f;
    public const float Depth = 22f;
    public const float Height = 4.5f;

    /// <summary>Posição inicial do jogador (perto da entrada, olhando para o fundo da loja).</summary>
    public static readonly Vector3 SpawnPosition = new(15f, 0f, 20.5f);

    // ---------- Paleta ----------
    private static readonly Color CokeRed = new(200, 16, 46);
    private static readonly Color Wall = new(240, 234, 220);
    private static readonly Color TileA = new(232, 232, 228);
    private static readonly Color TileB = new(205, 208, 212);
    private static readonly Color Ceiling = new(236, 236, 236);
    private static readonly Color Metal = new(200, 202, 208);
    private static readonly Color DarkMetal = new(55, 57, 62);
    private static readonly Color GlassTint = new Color(170, 210, 240) * 0.22f;

    /// <summary>Produtos da The Coca-Cola Company (cor do corpo, cor da tampa/rótulo, é garrafa?).</summary>
    public static readonly Product[] Beverages =
    {
        new("Coca-Cola", CokeRed, Color.White, true),
        new("Coca-Cola Zero", new Color(25, 25, 25), CokeRed, true),
        new("Coca-Cola Lata", CokeRed, new Color(220, 220, 225), false),
        new("Sprite", new Color(0, 135, 70), new Color(240, 230, 60), true),
        new("Fanta Laranja", new Color(255, 125, 0), new Color(0, 70, 160), true),
        new("Fanta Uva", new Color(115, 40, 145), new Color(0, 160, 70), false),
        new("Schweppes", new Color(235, 205, 60), new Color(20, 20, 20), false),
        new("Kuat", new Color(120, 190, 60), new Color(240, 240, 240), true),
        new("Del Valle", new Color(230, 150, 40), new Color(30, 120, 50), false),
        new("Crystal", new Color(150, 205, 240), new Color(0, 90, 190), true),
        new("Powerade", new Color(0, 95, 205), new Color(20, 20, 20), true),
        new("Monster", new Color(30, 30, 30), new Color(120, 220, 40), false),
    };

    private static readonly Color[] Groceries =
    {
        new(230, 190, 60), new(200, 60, 50), new(60, 110, 180), new(240, 240, 235),
        new(110, 70, 40), new(230, 120, 40), new(90, 160, 80), new(180, 40, 90),
        new(250, 220, 120), new(70, 70, 140), new(210, 170, 120), new(40, 140, 160),
    };

    public static Scene Build(GraphicsDevice device)
    {
        var b = new SceneBuilder();
        var rng = new Random(42);

        BuildShell(b);
        BuildFridges(b, rng);
        BuildGondolas(b, rng);
        BuildPromoIsland(b);
        BuildCheckouts(b);

        return b.Build(device);
    }

    // ---------------------------------------------------------------
    // Estrutura: chão, teto, paredes, luzes e fachada de vidro
    // ---------------------------------------------------------------
    private static void BuildShell(SceneBuilder b)
    {
        // Colisor do chão (bem maior que a loja)
        b.Collider(new Vector3(-50, -1, -50), new Vector3(80, 0, 80));

        // Piso quadriculado
        for (int x = 0; x < (int)Width; x++)
            for (int z = 0; z < (int)Depth; z++)
                b.FloorQuad(x, z, x + 1, z + 1, 0f, (x + z) % 2 == 0 ? TileA : TileB);

        // Área externa (estacionamento)
        b.FloorQuad(-30, Depth + 0.2f, Width + 30, Depth + 40, -0.01f, new Color(95, 97, 102));
        for (int i = 0; i < 12; i++)
        {
            float x = -2 + i * 3f;
            b.Emissive(
                new Vector3(x, 0f, Depth + 6f),
                new Vector3(x + 0.12f, 0.01f, Depth + 11f),
                new Color(235, 235, 235)
            );
        }

        // Teto
        b.FloorQuad(0, 0, Width, Depth, Height, Ceiling);

        // Luminárias
        for (float x = 2.5f; x < Width; x += 5f)
            for (float z = 2.5f; z < Depth; z += 3.5f)
            {
                b.Emissive(
                    new Vector3(x - 0.9f, Height - 0.06f, z - 0.2f),
                    new Vector3(x + 0.9f, Height, z + 0.2f),
                    new Color(255, 255, 248)
                );
            }

        const float t = 0.2f;
        // Paredes laterais e do fundo
        b.Box(new Vector3(-t, 0, -t), new Vector3(0, Height, Depth + t), Wall); // esquerda
        b.Box(new Vector3(Width, 0, -t), new Vector3(Width + t, Height, Depth + t), Wall); // direita
        b.Box(new Vector3(-t, 0, -t), new Vector3(Width + t, Height, 0), Wall); // fundo

        // Faixa vermelha (identidade Coca-Cola) nas paredes internas
        b.Emissive(new Vector3(0, 2.9f, 0), new Vector3(0.02f, 3.3f, Depth), CokeRed);
        b.Emissive(new Vector3(Width - 0.02f, 2.9f, 0), new Vector3(Width, 3.3f, Depth), CokeRed);
        b.Emissive(new Vector3(0, 2.9f, 0), new Vector3(Width, 3.3f, 0.02f), CokeRed);
        b.Emissive(new Vector3(0, 3.05f, 0.02f), new Vector3(Width, 3.12f, 0.03f), Color.White);

        // Fachada: viga superior + pilares + vidro
        b.Box(new Vector3(-t, 3f, Depth), new Vector3(Width + t, Height, Depth + t), Wall);

        // Pilares (removemos o do meio em x=15 para a entrada)
        float[] pillars = { 0f, 5f, 10f, 20f, 25f, 30f };
        foreach (float px in pillars)
        {
            float x0 = MathF.Max(-t, px - 0.25f);
            float x1 = MathF.Min(Width + t, px + 0.25f);
            b.Box(new Vector3(x0 - 0.01f, 0, Depth - 0.01f), new Vector3(x1 + 0.01f, 3f, Depth + t + 0.01f), DarkMetal);
        }

        // Vidros fixos e portas automáticas
        b.Glass(new Vector3(0.25f, 0, Depth + 0.08f), new Vector3(4.75f, 3f, Depth + 0.12f), GlassTint);
        b.Glass(new Vector3(5.25f, 0, Depth + 0.08f), new Vector3(9.75f, 3f, Depth + 0.12f), GlassTint);

        // Vão central (10 a 20) com portas deslizantes abertas
        b.Glass(new Vector3(10.25f, 0, Depth + 0.08f), new Vector3(13.0f, 3f, Depth + 0.12f), GlassTint); // Fixo esquerdo
        b.Glass(new Vector3(11.5f, 0, Depth + 0.15f), new Vector3(13.0f, 3f, Depth + 0.19f), GlassTint);  // Porta esquerda recuada

        b.Glass(new Vector3(17.0f, 0, Depth + 0.15f), new Vector3(18.5f, 3f, Depth + 0.19f), GlassTint);  // Porta direita recuada
        b.Glass(new Vector3(17.0f, 0, Depth + 0.08f), new Vector3(19.75f, 3f, Depth + 0.12f), GlassTint); // Fixo direito

        b.Glass(new Vector3(20.25f, 0, Depth + 0.08f), new Vector3(24.75f, 3f, Depth + 0.12f), GlassTint);
        b.Glass(new Vector3(25.25f, 0, Depth + 0.08f), new Vector3(29.75f, 3f, Depth + 0.12f), GlassTint);

        // Tapete da entrada
        b.Decor(new Vector3(13.0f, 0, Depth - 1.5f), new Vector3(17.0f, 0.01f, Depth + 1.0f), new Color(40, 40, 45));
    }

    // ---------------------------------------------------------------
    // Geladeiras da Coca-Cola na parede do fundo
    // ---------------------------------------------------------------
    private static void BuildFridges(SceneBuilder b, Random rng)
    {
        const float fw = 1.3f; // largura de cada geladeira
        const float z0 = 0.2f;
        const float z1 = 1.0f;
        const float top = 2.3f;
        float[] shelfY = { 0.25f, 0.65f, 1.05f, 1.45f, 1.85f };

        int count = 20;
        float startX = (Width - count * fw) / 2f;

        for (int i = 0; i < count; i++)
        {
            float x0 = startX + i * fw;
            float x1 = x0 + fw;

            // Corpo
            b.Decor(new Vector3(x0, 0, 0), new Vector3(x1, top, z0 + 0.05f), new Color(35, 35, 40)); // fundo
            b.Decor(new Vector3(x0, 0, 0), new Vector3(x0 + 0.05f, top, z1), DarkMetal); // lateral esq.
            b.Decor(new Vector3(x1 - 0.05f, 0, 0), new Vector3(x1, top, z1), DarkMetal); // lateral dir.
            b.Decor(new Vector3(x0, 0, 0), new Vector3(x1, shelfY[0] - 0.02f, z1), DarkMetal); // base

            // Letreiro vermelho com "onda" branca
            b.Emissive(new Vector3(x0, top, 0), new Vector3(x1, top + 0.35f, z1), CokeRed);
            b.Emissive(
                new Vector3(x0 + 0.1f, top + 0.13f, z1),
                new Vector3(x1 - 0.1f, top + 0.21f, z1 + 0.01f),
                Color.White,
                SceneBuilder.Faces.Front
            );

            // Prateleiras + produtos
            for (int s = 0; s < shelfY.Length; s++)
            {
                float y = shelfY[s];
                b.Decor(new Vector3(x0 + 0.05f, y - 0.02f, z0), new Vector3(x1 - 0.05f, y, z1 - 0.05f), Metal);

                // Cada prateleira tem um produto "principal", com alguns itens fora do lugar
                var main = Beverages[rng.Next(Beverages.Length)];
                for (int k = 0; k < 10; k++)
                {
                    if (rng.NextDouble() < 0.08)
                        continue; // espaço vazio (ruptura)

                    var p = rng.NextDouble() < 0.12 ? Beverages[rng.Next(Beverages.Length)] : main;
                    float px = x0 + 0.08f + k * 0.115f;
                    AddBeverage(b, p, px, y, z0 + 0.1f, z1 - 0.1f);
                }
            }

            // Porta de vidro + puxador
            b.Glass(new Vector3(x0 + 0.02f, shelfY[0], z1), new Vector3(x1 - 0.02f, top, z1 + 0.03f), GlassTint);
            b.Decor(new Vector3(x0, shelfY[0], z1), new Vector3(x0 + 0.04f, top, z1 + 0.04f), DarkMetal);
            b.Decor(new Vector3(x1 - 0.04f, shelfY[0], z1), new Vector3(x1, top, z1 + 0.04f), DarkMetal);
            b.Decor(new Vector3(x1 - 0.12f, 1.0f, z1 + 0.04f), new Vector3(x1 - 0.09f, 1.6f, z1 + 0.08f), Metal);

            b.Collider(new Vector3(x0, 0, 0), new Vector3(x1, top + 0.35f, z1 + 0.08f));
        }
    }

    private static void AddBeverage(SceneBuilder b, Product p, float x, float y, float zBack, float zFront)
    {
        const float w = 0.09f;
        const float d = 0.09f;
        const float gap = 0.012f;
        const float slotHeight = 0.34f; // cabe uma garrafa com tampa

        // Fileira de itens individuais (o da frente primeiro); cada um pode ser pego e devolvido
        for (float zf = zFront; zf - d >= zBack - 0.001f; zf -= d + gap)
        {
            float z = zf;
            var slot = new BoundingBox(new Vector3(x, y, z - d), new Vector3(x + w, y + slotHeight, z + 0.002f));
            b.Pickup(p, prod => BeverageParts(prod, x, y, z), slot);
        }
    }

    /// <summary>Geometria de uma bebida com a frente em <paramref name="zf"/>.</summary>
    private static PickupPart[] BeverageParts(Product p, float x, float y, float zf)
    {
        const float w = 0.09f;
        const float d = 0.09f;
        float h = p.IsBottle ? 0.3f : 0.13f;

        var body = new PickupPart(new Vector3(x, y, zf - d), new Vector3(x + w, y + h, zf), p.Body);
        // Rótulo/faixa
        var label = new PickupPart(
            new Vector3(x, y + h * 0.45f, zf),
            new Vector3(x + w, y + h * 0.7f, zf + 0.002f),
            p.Accent,
            SceneBuilder.Faces.Front
        );
        // Tampa (garrafa) ou topo metálico (lata)
        var top = p.IsBottle
            ? new PickupPart(new Vector3(x + 0.03f, y + h, zf - 0.06f), new Vector3(x + w - 0.03f, y + h + 0.03f, zf - 0.03f), p.Accent)
            : new PickupPart(new Vector3(x + 0.005f, y + h, zf - d + 0.005f), new Vector3(x + w - 0.005f, y + h + 0.008f, zf - 0.005f), Metal);

        return new[] { body, label, top };
    }

    // ---------------------------------------------------------------
    // Gôndolas (corredores) com mercearia em geral
    // ---------------------------------------------------------------
    private static void BuildGondolas(SceneBuilder b, Random rng)
    {
        float[] centers = { 6f, 10.5f, 15f, 19.5f, 24f };
        const float z0 = 4f;
        const float z1 = 13f;
        const float half = 0.5f;
        const float height = 1.9f;
        float[] shelfY = { 0.15f, 0.55f, 0.95f, 1.35f };

        foreach (float c in centers)
        {
            // Base, painel central e laterais
            b.Decor(new Vector3(c - half, 0, z0), new Vector3(c + half, 0.15f, z1), new Color(85, 85, 92));
            b.Decor(new Vector3(c - 0.03f, 0.15f, z0), new Vector3(c + 0.03f, height, z1), new Color(215, 215, 220));
            b.Decor(new Vector3(c - half, 0, z0 - 0.04f), new Vector3(c + half, height, z0), Metal);
            b.Decor(new Vector3(c - half, 0, z1), new Vector3(c + half, height, z1 + 0.04f), Metal);

            for (int side = -1; side <= 1; side += 2)
            {
                float inner = c + side * 0.03f;
                float outer = c + side * half;
                float xMin = MathF.Min(inner, outer);
                float xMax = MathF.Max(inner, outer);

                foreach (float y in shelfY)
                {
                    if (y > 0.2f)
                        b.Decor(new Vector3(xMin, y - 0.02f, z0), new Vector3(xMax, y, z1), new Color(225, 225, 230));

                    // Etiqueta de preço na borda
                    float edge = outer;
                    b.Emissive(
                        new Vector3(MathF.Min(edge, edge + side * 0.01f), y - 0.06f, z0),
                        new Vector3(MathF.Max(edge, edge + side * 0.01f), y, z1),
                        new Color(250, 245, 200)
                    );

                    // Produtos
                    float z = z0 + 0.05f;
                    while (z < z1 - 0.1f)
                    {
                        float pw = 0.15f + (float)rng.NextDouble() * 0.15f;
                        float ph = 0.15f + (float)rng.NextDouble() * 0.18f;
                        if (z + pw > z1 - 0.05f)
                            break;
                        if (rng.NextDouble() > 0.06)
                        {
                            var col = Groceries[rng.Next(Groceries.Length)];
                            float a = c + side * 0.08f;
                            float d = c + side * 0.45f;
                            b.Decor(
                                new Vector3(MathF.Min(a, d), y, z),
                                new Vector3(MathF.Max(a, d), y + ph, z + pw - 0.02f),
                                col
                            );
                        }
                        z += pw;
                    }
                }
            }

            // Placa do corredor pendurada no teto
            float zc = (z0 + z1) / 2f;
            b.Emissive(new Vector3(c - 0.03f, 3.0f, zc - 0.8f), new Vector3(c + 0.03f, 3.4f, zc + 0.8f), new Color(0, 75, 150));
            b.Emissive(new Vector3(c - 0.035f, 3.17f, zc - 0.6f), new Vector3(c + 0.035f, 3.23f, zc + 0.6f), Color.White);
            b.Decor(new Vector3(c - 0.01f, 3.4f, zc - 0.6f), new Vector3(c + 0.01f, Height, zc - 0.58f), DarkMetal);
            b.Decor(new Vector3(c - 0.01f, 3.4f, zc + 0.58f), new Vector3(c + 0.01f, Height, zc + 0.6f), DarkMetal);

            b.Collider(new Vector3(c - half, 0, z0 - 0.04f), new Vector3(c + half, height, z1 + 0.04f));

            // Ponta de gôndola (end cap) promocional da Coca-Cola, virada para a entrada
            BuildEndCap(b, c, z1 + 0.04f);
        }
    }

    private static void BuildEndCap(SceneBuilder b, float c, float z)
    {
        const float d = 0.6f;
        b.Box(new Vector3(c - 0.5f, 0, z), new Vector3(c + 0.5f, 0.3f, z + d), CokeRed);

        Color[] tiers = { CokeRed, new(25, 25, 25), CokeRed };
        for (int t = 0; t < tiers.Length; t++)
        {
            float y = 0.3f + t * 0.32f;
            for (int k = 0; k < 4; k++)
            {
                float x = c - 0.48f + k * 0.24f;
                b.Decor(new Vector3(x, y, z + 0.05f), new Vector3(x + 0.22f, y + 0.3f, z + d - 0.05f), tiers[t]);
                b.Decor(
                    new Vector3(x, y + 0.12f, z + d - 0.05f),
                    new Vector3(x + 0.22f, y + 0.18f, z + d - 0.048f),
                    Color.White,
                    SceneBuilder.Faces.Front
                );
            }
        }
        b.Emissive(new Vector3(c - 0.5f, 1.35f, z + 0.1f), new Vector3(c + 0.5f, 1.75f, z + 0.15f), CokeRed);
        b.Emissive(new Vector3(c - 0.4f, 1.52f, z + 0.15f), new Vector3(c + 0.4f, 1.58f, z + 0.16f), Color.White, SceneBuilder.Faces.Front);
        b.Collider(new Vector3(c - 0.5f, 0, z), new Vector3(c + 0.5f, 1.3f, z + d));
    }

    // ---------------------------------------------------------------
    // Ilha promocional perto da entrada (pallet de fardos)
    // ---------------------------------------------------------------
    private static void BuildPromoIsland(SceneBuilder b)
    {
        float x0 = 13.6f, x1 = 16.4f, z0 = 15.4f, z1 = 16.8f;
        b.Box(new Vector3(x0, 0, z0), new Vector3(x1, 0.15f, z1), new Color(150, 110, 70)); // pallet

        Color[] layers = { CokeRed, CokeRed, new(25, 25, 25), CokeRed };
        for (int l = 0; l < layers.Length; l++)
        {
            float y = 0.15f + l * 0.3f;
            for (float x = x0; x < x1 - 0.01f; x += 0.7f)
                for (float z = z0; z < z1 - 0.01f; z += 0.7f)
                {
                    b.Decor(new Vector3(x + 0.01f, y, z + 0.01f), new Vector3(x + 0.69f, y + 0.29f, z + 0.69f), layers[l]);
                }
        }
        float topY = 0.15f + layers.Length * 0.3f;

        // Totem com placa
        float cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f;
        b.Decor(new Vector3(cx - 0.03f, topY, cz - 0.03f), new Vector3(cx + 0.03f, 2.4f, cz + 0.03f), Metal);
        b.Emissive(new Vector3(cx - 0.6f, 2.4f, cz - 0.03f), new Vector3(cx + 0.6f, 3.0f, cz + 0.03f), CokeRed);
        b.Emissive(new Vector3(cx - 0.5f, 2.65f, cz - 0.04f), new Vector3(cx + 0.5f, 2.73f, cz + 0.04f), Color.White);

        b.Collider(new Vector3(x0, 0, z0), new Vector3(x1, topY, z1));
    }

    // ---------------------------------------------------------------
    // Caixas registradoras (checkouts) perto da fachada
    // ---------------------------------------------------------------
    private static void BuildCheckouts(SceneBuilder b)
    {
        float[] xs = { 2.5f, 6.5f, 21f, 25f };
        const float z0 = 18.3f;
        const float z1 = 19.0f;

        for (int i = 0; i < xs.Length; i++)
        {
            float x0 = xs[i];
            float x1 = x0 + 2.5f;
            b.Box(new Vector3(x0, 0, z0), new Vector3(x1, 0.9f, z1), new Color(225, 225, 230));
            b.Decor(new Vector3(x0 + 0.05f, 0.9f, z0 + 0.1f), new Vector3(x0 + 1.8f, 0.92f, z1 - 0.1f), new Color(20, 20, 22)); // esteira
            b.Decor(new Vector3(x0 + 1.95f, 0.9f, z0 + 0.15f), new Vector3(x1 - 0.1f, 1.15f, z1 - 0.15f), DarkMetal); // caixa
            b.Emissive(new Vector3(x0 + 2.0f, 1.15f, z0 + 0.3f), new Vector3(x1 - 0.15f, 1.4f, z0 + 0.33f), new Color(60, 140, 220)); // tela

            // Poste com número luminoso
            b.Decor(new Vector3(x0 + 0.05f, 0.9f, z0 + 0.05f), new Vector3(x0 + 0.1f, 2.3f, z0 + 0.1f), Metal);
            b.Emissive(new Vector3(x0 - 0.1f, 2.3f, z0 - 0.05f), new Vector3(x0 + 0.25f, 2.55f, z0 + 0.2f), new Color(40, 200, 90));
        }
    }
}

/// <summary>Produto de bebida exibido nas geladeiras.</summary>
public readonly record struct Product(string Name, Color Body, Color Accent, bool IsBottle);
