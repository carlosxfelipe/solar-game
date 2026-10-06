using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SolarGame.Characters;

public class NpcManager
{
    private readonly List<Customer> _customers = new();

    public NpcManager(IReadOnlyList<BoundingBox> colliders)
    {
        // NPC 1 (Original)
        var c1 = new Customer() { Colliders = colliders, Position = new Vector3(6f, 0f, 15f) };
        _customers.Add(c1);

        // NPC 2
        var c2 = new Customer() { Colliders = colliders, Position = new Vector3(8.25f, 0f, 8f) };
        c2.ShirtColor = new Color(200, 50, 50); // Red
        c2.PantsColor = new Color(50, 50, 50);  // Black
        c2.HairColor = new Color(200, 180, 50); // Blonde
        c2.ApplyColors();
        _customers.Add(c2);

        // NPC 3
        var c3 = new Customer() { Colliders = colliders, Position = new Vector3(4f, 0f, 8f) };
        c3.ShirtColor = new Color(50, 100, 200); // Blue
        c3.SkinColor = new Color(130, 80, 50);   // Dark skin
        c3.HairColor = new Color(10, 10, 10);    // Black
        c3.ApplyColors();
        _customers.Add(c3);

        // NPC 4
        var c4 = new Customer() { Colliders = colliders, Position = new Vector3(12f, 0f, 16f) };
        c4.ShirtColor = new Color(220, 220, 220); // White
        c4.PantsColor = new Color(100, 150, 200); // Jeans
        c4.SkinColor = new Color(240, 200, 170);  // Light skin
        c4.HairColor = new Color(150, 80, 30);    // Ginger
        c4.ApplyColors();
        _customers.Add(c4);
    }

    public void Update(float dt)
    {
        foreach (var c in _customers)
            c.Update(dt);
    }

    public void Draw(BasicEffect effect)
    {
        foreach (var c in _customers)
            c.Draw(effect);
    }
}
