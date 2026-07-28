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

    [Tooltip("外部重力場")]
    public Vector2 externalField = Vector2.zero;

    public const int KindNormal = 0;
    public const int KindPlayer = 1;
    public const int KindSatellite = 2;

    private static readonly List<GravBody> gravBodies = new();
    public static IReadOnlyList<GravBody> GravBodies => gravBodies;

    public static void Register(GravBody inGravBody)
    {
        if (gravBodies.Contains(inGravBody)) return;

        gravBodies.Add(inGravBody);
    }

    public static void Unregister(GravBody inGravBody)
    {
        gravBodies.Remove(inGravBody);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        ComputeAccelerations();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void FixedUpdate()
    {
        var physDt = Time.fixedDeltaTime / substeps;
        for (var step = 0; step < substeps; step++) Step(physDt);

        //各ティックに天体ごとの位置を更新する
        for (var i = 0; i < gravBodies.Count; i++)
        {
            var body = gravBodies[i];
            var bodyTransform = body.transform;

            bodyTransform.position = new Vector3(body.position.x, body.position.y, bodyTransform.position.z);
        }
    }

    private void Step(float dt)
    {
        var halfStep = dt / 2f;

        for (var i = 0; i < gravBodies.Count; i++)
        {
            var body = gravBodies[i];

            if (body.isAnchored) continue;

            body.velocity += body.acceleration * halfStep;
            body.position += body.velocity * dt;
        }

        ComputeAccelerations();

        for (var i = 0; i < gravBodies.Count; i++)
        {
            var body = gravBodies[i];

            if (body.isAnchored) continue;

            body.velocity += body.acceleration * halfStep;
        }
    }

    /// <summary>
    /// 天体ごとの加速度を計算
    /// </summary>
    private void ComputeAccelerations()
    {
        //天体ごとの加速度をクリア
        for (var i = 0; i < gravBodies.Count; i++) gravBodies[i].acceleration = Vector2.zero;

        for (var i = 0; i < gravBodies.Count; i++)
        {
            var bodyA = gravBodies[i];

            for (var j = i + 1; j < gravBodies.Count; j++)
            {
                var bodyB = gravBodies[j];

                if (bodyA.CompareTag("Player") && bodyB.CompareTag("Player")) continue;

                if ((bodyA.CompareTag("Satellite") && bodyA.GetOrbitingBody() != bodyB) ||
                    (bodyB.CompareTag("Satellite") && bodyB.GetOrbitingBody() != bodyA))
                    continue;

                var pairSoft = Mathf.Max(softening, Mathf.Max(bodyA.softeningRadius, bodyB.softeningRadius));
                var softSqr = pairSoft * pairSoft;

                var displacement = bodyB.position - bodyA.position;
                var rSqr = displacement.sqrMagnitude + softSqr;
                var invRCubed = 1f / (rSqr * Mathf.Sqrt(rSqr));

                var unitForce = gravitationalConstant * invRCubed * displacement;

                if (!bodyA.isAnchored) bodyA.acceleration += unitForce * bodyB.mass;

                if (!bodyB.isAnchored) bodyB.acceleration -= unitForce * bodyA.mass;
            }
        }

        if (externalField != Vector2.zero)
            for (var i = 0; i < gravBodies.Count; i++)
                if (!gravBodies[i].isAnchored)
                    gravBodies[i].acceleration += externalField;
    }

    /// <summary>
    /// 軌道予測システム用
    /// </summary>
    /// <param name="pos">シーンにある天体の位置</param>
    /// <param name="mass">天体ごとの質量</param>
    /// <param name="anchored">天体ごとが動けるか</param>
    /// <param name="kind">天体の種別</param>
    /// <param name="orbitIndex">衛星の親のインデックス。衛星以外は-1</param>
    /// <param name="softRadii">天体ごとの最小距離</param>
    /// <param name="acc">天体の加速度</param>
    /// <param name="bodyCount">天体の数</param>
    /// <param name="gravConst">重力定数</param>
    /// <param name="softening">最小距離</param>
    public void ComputeAccelerations(Vector2[] pos, float[] mass, bool[] anchored, int[] kind, int[] orbitIndex,
        float[] softRadii, Vector2[] acc, int bodyCount, float gravConst, float softening)
    {
        for (var i = 0; i < bodyCount; i++) acc[i] = Vector2.zero;

        for (var i = 0; i < bodyCount; i++)
        for (var j = i + 1; j < bodyCount; j++)
        {
            if (kind[i] == KindPlayer && kind[j] == KindPlayer) continue;

            if ((kind[i] == KindSatellite && orbitIndex[i] != j) ||
                (kind[j] == KindSatellite && orbitIndex[j] != i))
                continue;

            var pairSoft = Mathf.Max(softening, Mathf.Max(softRadii[i], softRadii[j]));
            var softSqr = pairSoft * pairSoft;

            var displacement = pos[j] - pos[i];
            var rSqr = displacement.sqrMagnitude + softSqr;
            var invRCubed = 1f / (rSqr * Mathf.Sqrt(rSqr));
            var unitForce = gravConst * invRCubed * displacement;

            if (!anchored[i]) acc[i] += unitForce * mass[j];

            if (!anchored[j]) acc[j] -= unitForce * mass[i];
        }
    }

    public float CircularOrbitSpeed(float centralMass, float orbitalRadius)
    {
        return Mathf.Sqrt(gravitationalConstant * centralMass / Mathf.Max(orbitalRadius, 0.001f));
    }
}