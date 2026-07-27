using System.Collections.Generic;
using UnityEngine;

public class GravSystem : MonoBehaviour
{
    public static GravSystem Instance { get; private set; }

    [Tooltip("重力定数")]
    public float gravitationalConstant = 1f;

    [Tooltip("最小距離")]
    public float softening = 0.5f;

    [Tooltip("物理システムのステップ数")]
    [Range(1, 16)] public int substeps = 1;

    private static readonly List<GravBody> gravBodies = new List<GravBody>();
    public static IReadOnlyList<GravBody> GravBodies => gravBodies;

    public static void Register(GravBody inGravBody)
    {
        if (gravBodies.Contains(inGravBody))
        {
            return;
        }

        gravBodies.Add(inGravBody);
    }

    public static void Unregister(GravBody inGravBody)
    {
        gravBodies.Remove(inGravBody);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ComputeAccelerations();   
    }

    private void Awake()
    {
        Instance = this;
    }

    private void FixedUpdate()
    {
        float physDt = Time.fixedDeltaTime / substeps;
        for (int step = 0; step < substeps; step++)
        {
            Step(physDt);
        }

        //各ティックに天体ごとの位置を更新する
        for (int i = 0; i < gravBodies.Count; i++)
        {
            GravBody body = gravBodies[i];
            Transform bodyTransform = body.transform;

            bodyTransform.position = new Vector3(body.position.x, body.position.y, bodyTransform.position.z);
        }
    }

    private void Step(float dt)
    {
        float halfStep = dt / 2f;

        for (int i = 0; i < gravBodies.Count; i++)
        {
            GravBody body = gravBodies[i];

            if (body.isAnchored)
            {
                continue;
            }

            body.velocity += body.acceleration * halfStep;
            body.position += body.velocity * dt;
        }

        ComputeAccelerations();

        for (int i = 0; i < gravBodies.Count; i++)
        {
            GravBody body = gravBodies[i];

            if (body.isAnchored)
            {
                continue;
            }

            body.velocity += body.acceleration * halfStep;
        }
    }

    /// <summary>
    /// 天体ごとの加速度を計算
    /// </summary>
    private void ComputeAccelerations()
    {
        //天体ごとの加速度をクリア
        for (int i = 0; i < gravBodies.Count;i++)
        {
            gravBodies[i].acceleration = Vector2.zero;
        }

        float softSqr = softening * softening;

        for (int i = 0; i < gravBodies.Count; i++)
        {
            GravBody bodyA = gravBodies[i];

            for (int j = i + 1; j < gravBodies.Count; j++)
            {
                GravBody bodyB = gravBodies[j];

                Vector2 displacement = bodyB.position - bodyA.position;
                float rSqr = displacement.sqrMagnitude + softSqr;
                float invRCubed = 1f / (rSqr * Mathf.Sqrt(rSqr));

                Vector2 unitForce = gravitationalConstant * invRCubed * displacement;

                if (!bodyA.isAnchored)
                {
                    bodyA.acceleration += unitForce * bodyB.mass;
                }

                if (!bodyB.isAnchored)
                {
                    bodyB.acceleration -= unitForce * bodyA.mass;
                }
            }
        }
    }

    /// <summary>
    /// 軌道予測システム用
    /// </summary>
    /// <param name="pos">シーンにある天体の位置</param>
    /// <param name="mass">天体ごとの質量</param>
    /// <param name="anchored">天体ごとが動けるか</param>
    /// <param name="acc">天体の加速度</param>
    /// <param name="bodyCount">天体の数</param>
    /// <param name="gravConst">重力定数</param>
    /// <param name="softening">最小距離</param>
    public static void ComputeAccelerations(Vector2[] pos, float[] mass, bool[] anchored, Vector2[] acc, int bodyCount, float gravConst, float softening)
    {
        for (int i = 0;i < bodyCount;i++)
        {
            acc[i] = Vector2.zero;
        }

        float softSqr = softening * softening;

        for (int i = 0; i < bodyCount;i++)
        {
            for (int j = i + 1; j < bodyCount;j++)
            {
                Vector2 displacement = pos[j] - pos[i];
                float rSqr = displacement.sqrMagnitude + softSqr;
                float invRCubed = 1f / (rSqr * Mathf.Sqrt(rSqr));
                Vector2 unitForce = gravConst * invRCubed * displacement;

                if (!anchored[i])
                {
                    acc[i] += unitForce * mass[j];
                }

                if (!anchored[j])
                {
                    acc[j] -= unitForce * mass[i];
                }
            }
        }
    }
}
