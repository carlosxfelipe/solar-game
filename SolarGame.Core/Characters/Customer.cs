using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SolarGame.World;

namespace SolarGame.Characters;

/// <summary>
/// Exemplo de uma Skin específica herdando de CharacterModel.
/// </summary>
public class Customer : CharacterModel
{
    private enum NpcState { Idle, Walking }
    private NpcState _currentState = NpcState.Idle;
    private float _stateTimer = 2f;
    private float _targetYaw = 0f;
    private float _walkTime = 0f;
    private readonly Random _rng = new Random();

    // Cores personalizáveis para reutilizar a classe
    public Color SkinColor { get; set; } = new Color(222, 170, 128);
    public Color ShirtColor { get; set; } = new Color(80, 160, 100);
    public Color PantsColor { get; set; } = new Color(60, 80, 150);
    public Color ShoesColor { get; set; } = new Color(40, 40, 40);
    public Color HairColor { get; set; } = new Color(40, 30, 20);
    public Color PupilColor { get; set; } = new Color(40, 50, 120);
    public Color BeardColor { get; set; } = new Color(90, 60, 40);
    public Color NoseColor { get; set; } = new Color(190, 130, 90);

    public void ApplyColors()
    {
        Head.Clear(); Torso.Clear();
        LeftArm.Clear(); RightArm.Clear();
        LeftLeg.Clear(); RightLeg.Clear();
        BuildSkin();
    }

    public IReadOnlyList<BoundingBox> Colliders;

    private bool Collides(Vector3 pos)
    {
        if (Colliders == null) return false;
        float half = 0.3f;
        var box = new BoundingBox(
            new Vector3(pos.X - half, pos.Y, pos.Z - half),
            new Vector3(pos.X + half, pos.Y + 1.8f, pos.Z + half)
        );
        foreach (var c in Colliders)
        {
            if (box.Min.X < c.Max.X && box.Max.X > c.Min.X &&
                box.Min.Y < c.Max.Y && box.Max.Y > c.Min.Y &&
                box.Min.Z < c.Max.Z && box.Max.Z > c.Min.Z)
                return true;
        }
        return false;
    }

    public void Update(float dt)
    {
        _stateTimer -= dt;
        if (_stateTimer <= 0)
        {
            if (_currentState == NpcState.Idle)
            {
                // Decide passear
                _currentState = NpcState.Walking;
                _stateTimer = (float)(_rng.NextDouble() * 3.0 + 1.5); // Anda de 1.5 a 4.5 segundos
                _targetYaw = (float)(_rng.NextDouble() * MathHelper.TwoPi); // Direção aleatória
            }
            else
            {
                // Decide parar e observar
                _currentState = NpcState.Idle;
                _stateTimer = (float)(_rng.NextDouble() * 4.0 + 1.5); // Para de 1.5 a 5.5 segundos
            }
        }

        if (_currentState == NpcState.Walking)
        {
            // Gira suavemente pro lado escolhido
            float diff = MathHelper.WrapAngle(_targetYaw - Yaw);
            Yaw += diff * 4f * dt;

            // Se estiver quase de frente pro alvo, caminha
            if (Math.Abs(diff) < 0.3f)
            {
                float speed = 1.3f;
                float moveX = MathF.Sin(Yaw) * speed * dt;
                float moveZ = -MathF.Cos(Yaw) * speed * dt;

                Vector3 tryX = Position;
                tryX.X += moveX;
                if (!Collides(tryX)) Position.X += moveX;
                else _stateTimer = 0; // Bateu em algo, decide novo rumo imediatamente

                Vector3 tryZ = Position;
                tryZ.Z += moveZ;
                if (!Collides(tryZ)) Position.Z += moveZ;
                else _stateTimer = 0; // Bateu em algo, decide novo rumo imediatamente

                _walkTime += dt * speed;

                AnimateWalk(_walkTime, speed);
            }
            else
            {
                AnimateWalk(_walkTime, 0f); // Para as pernas enquanto vira o corpo
            }
        }
        else
        {
            HeadYaw = 0f;
            AnimateWalk(_walkTime, 0f); // Volta a postura neutra
        }
    }

    protected override void BuildSkin()
    {
        // Cabeça (Pivô no pescoço Y=1.5) - Local Y de 0 a 0.38
        SceneBuilder.AddBoxGeometry(Head, new Vector3(-0.19f, 0f, -0.19f), new Vector3(0.19f, 0.38f, 0.19f), SkinColor, SceneBuilder.Faces.All, true);

        // Cabelo (Dark)
        SceneBuilder.AddBoxGeometry(Head, new Vector3(-0.195f, 0.28f, -0.195f), new Vector3(0.195f, 0.385f, 0.195f), HairColor, SceneBuilder.Faces.All, true);

        // Face Details (Steve style facing -Z)
        Color scleraColor = new Color(250, 250, 250);

        // Left Eye (Sclera outer, pupil inner)
        SceneBuilder.AddBoxGeometry(Head, new Vector3(-0.142f, 0.142f, -0.195f), new Vector3(-0.095f, 0.19f, -0.19f), scleraColor, SceneBuilder.Faces.All, false);
        SceneBuilder.AddBoxGeometry(Head, new Vector3(-0.095f, 0.142f, -0.195f), new Vector3(-0.047f, 0.19f, -0.19f), PupilColor, SceneBuilder.Faces.All, false);

        // Right Eye (Pupil inner, Sclera outer)
        SceneBuilder.AddBoxGeometry(Head, new Vector3(0.047f, 0.142f, -0.195f), new Vector3(0.095f, 0.19f, -0.19f), PupilColor, SceneBuilder.Faces.All, false);
        SceneBuilder.AddBoxGeometry(Head, new Vector3(0.095f, 0.142f, -0.195f), new Vector3(0.142f, 0.19f, -0.19f), scleraColor, SceneBuilder.Faces.All, false);

        // Nose
        SceneBuilder.AddBoxGeometry(Head, new Vector3(-0.047f, 0.095f, -0.195f), new Vector3(0.047f, 0.142f, -0.19f), NoseColor, SceneBuilder.Faces.All, true);

        // Mouth / Beard
        SceneBuilder.AddBoxGeometry(Head, new Vector3(-0.095f, 0.047f, -0.195f), new Vector3(0.095f, 0.095f, -0.19f), BeardColor, SceneBuilder.Faces.All, true);

        // Tronco (Pivô no quadril Y=0.8)
        SceneBuilder.AddBoxGeometry(Torso, new Vector3(-0.25f, 0f, -0.12f), new Vector3(0.25f, 0.7f, 0.12f), ShirtColor, SceneBuilder.Faces.All, true);

        // Braço Esquerdo (Pivô no ombro Y=1.5, X=-0.35)
        SceneBuilder.AddBoxGeometry(LeftArm, new Vector3(-0.1f, -0.7f, -0.12f), new Vector3(0.1f, -0.2f, 0.12f), SkinColor, SceneBuilder.Faces.All, true);
        SceneBuilder.AddBoxGeometry(LeftArm, new Vector3(-0.1f, -0.2f, -0.12f), new Vector3(0.1f, 0f, 0.12f), ShirtColor, SceneBuilder.Faces.All, true); // Manga

        // Braço Direito (Pivô no ombro Y=1.5, X=0.35)
        SceneBuilder.AddBoxGeometry(RightArm, new Vector3(-0.1f, -0.7f, -0.12f), new Vector3(0.1f, -0.2f, 0.12f), SkinColor, SceneBuilder.Faces.All, true);
        SceneBuilder.AddBoxGeometry(RightArm, new Vector3(-0.1f, -0.2f, -0.12f), new Vector3(0.1f, 0f, 0.12f), ShirtColor, SceneBuilder.Faces.All, true); // Manga

        // Perna Esquerda (Pivô no quadril Y=0.8, X=-0.125)
        SceneBuilder.AddBoxGeometry(LeftLeg, new Vector3(-0.125f, -0.65f, -0.12f), new Vector3(0.125f, 0f, 0.12f), PantsColor, SceneBuilder.Faces.All, true);
        SceneBuilder.AddBoxGeometry(LeftLeg, new Vector3(-0.125f, -0.8f, -0.12f), new Vector3(0.125f, -0.65f, 0.12f), ShoesColor, SceneBuilder.Faces.All, true); // Sapato

        // Perna Direita (Pivô no quadril Y=0.8, X=0.125)
        SceneBuilder.AddBoxGeometry(RightLeg, new Vector3(-0.125f, -0.65f, -0.12f), new Vector3(0.125f, 0f, 0.12f), PantsColor, SceneBuilder.Faces.All, true);
        SceneBuilder.AddBoxGeometry(RightLeg, new Vector3(-0.125f, -0.8f, -0.12f), new Vector3(0.125f, -0.65f, 0.12f), ShoesColor, SceneBuilder.Faces.All, true); // Sapato
    }
}
