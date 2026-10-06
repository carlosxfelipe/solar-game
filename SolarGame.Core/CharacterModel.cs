using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolarGame.World;

namespace SolarGame;

/// <summary>
/// Modelo dinâmico genérico para personagens (Players, NPCs) com suporte a animação.
/// As partes do corpo são renderizadas de forma independente, permitindo movimentos
/// como caminhar, correr, levantar os braços, etc.
/// </summary>
public abstract class CharacterModel
{
    protected readonly List<VertexPositionColor> Head = new();
    protected readonly List<VertexPositionColor> Torso = new();
    protected readonly List<VertexPositionColor> LeftArm = new();
    protected readonly List<VertexPositionColor> RightArm = new();
    protected readonly List<VertexPositionColor> LeftLeg = new();
    protected readonly List<VertexPositionColor> RightLeg = new();

    // Transformações globais (posição no mundo)
    public Vector3 Position = Vector3.Zero;
    public float Yaw = 0f;

    // Ângulos de animação local (radianos)
    public float HeadPitch = 0f;
    public float HeadYaw = 0f;
    public float LeftArmPitch = 0f;
    public float RightArmPitch = 0f;
    public float LeftLegPitch = 0f;
    public float RightLegPitch = 0f;

    public CharacterModel()
    {
        BuildSkin();
    }

    /// <summary>
    /// Deve ser implementado pelas classes derivadas para definir a aparência (skin)
    /// do personagem, preenchendo as listas de vértices de cada membro em relação aos
    /// seus respectivos pivôs locais.
    /// </summary>
    protected abstract void BuildSkin();

    /// <summary>
    /// Simula a animação de caminhada baseada num tempo contínuo.
    /// </summary>
    /// <param name="walkTime">Tempo ou distância acumulada da caminhada.</param>
    /// <param name="speed">Velocidade do movimento (se 0, volta à pose neutra).</param>
    public void AnimateWalk(float walkTime, float speed)
    {
        if (speed <= 0.01f)
        {
            // Transição suave de volta para a pose de descanso (em pé parado)
            LeftArmPitch = MathHelper.Lerp(LeftArmPitch, 0f, 0.1f);
            RightArmPitch = MathHelper.Lerp(RightArmPitch, 0f, 0.1f);
            LeftLegPitch = MathHelper.Lerp(LeftLegPitch, 0f, 0.1f);
            RightLegPitch = MathHelper.Lerp(RightLegPitch, 0f, 0.1f);
            return;
        }

        // Movimento alternado estilo Minecraft
        float swing = MathF.Sin(walkTime * 10f) * 0.5f;

        LeftArmPitch = swing;
        RightArmPitch = -swing;
        LeftLegPitch = -swing;
        RightLegPitch = swing;
    }

    /// <summary>
    /// Renderiza o modelo inteiro aplicando a hierarquia de transformações.
    /// É necessário definir as propriedades de câmera (View/Projection) no 'effect' 
    /// antes de chamar este método.
    /// </summary>
    public void Draw(BasicEffect effect)
    {
        // Posição base do personagem. Usamos -Yaw porque o sistema de câmera do MonoGame
        // e o sistema de rotação de Matrizes são invertidos no eixo Y.
        Matrix rootTransform = Matrix.CreateRotationY(-Yaw) * Matrix.CreateTranslation(Position);

        // 1. Torso (Pivô no quadril, Y=0.8)
        Matrix torsoTransform = Matrix.CreateTranslation(0, 0.8f, 0) * rootTransform;
        DrawPart(effect, Torso, torsoTransform);

        // 2. Cabeça (Pivô no pescoço, Y=1.5 do chão)
        Matrix headTransform = Matrix.CreateRotationX(HeadPitch) * Matrix.CreateRotationY(HeadYaw) *
                               Matrix.CreateTranslation(0, 1.5f, 0) * rootTransform;
        DrawPart(effect, Head, headTransform);

        // 3. Braços (Pivô nos ombros, altura Y=1.5, offset X nas laterais)
        Matrix leftArmTransform = Matrix.CreateRotationX(LeftArmPitch) *
                                  Matrix.CreateTranslation(-0.35f, 1.5f, 0) * rootTransform;
        DrawPart(effect, LeftArm, leftArmTransform);

        Matrix rightArmTransform = Matrix.CreateRotationX(RightArmPitch) *
                                   Matrix.CreateTranslation(0.35f, 1.5f, 0) * rootTransform;
        DrawPart(effect, RightArm, rightArmTransform);

        // 4. Pernas (Pivô no quadril, Y=0.8, offset X entre elas)
        Matrix leftLegTransform = Matrix.CreateRotationX(LeftLegPitch) *
                                  Matrix.CreateTranslation(-0.125f, 0.8f, 0) * rootTransform;
        DrawPart(effect, LeftLeg, leftLegTransform);

        Matrix rightLegTransform = Matrix.CreateRotationX(RightLegPitch) *
                                   Matrix.CreateTranslation(0.125f, 0.8f, 0) * rootTransform;
        DrawPart(effect, RightLeg, rightLegTransform);
    }

    private void DrawPart(BasicEffect effect, List<VertexPositionColor> vertices, Matrix transform)
    {
        if (vertices.Count == 0) return;

        effect.World = transform;
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            effect.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Count / 3);
        }
    }
}
