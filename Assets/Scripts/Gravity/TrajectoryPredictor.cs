using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class TrajectoryPredictor : MonoBehaviour
{
    public GravBody target;

    [Range(20, 3000)] public int lookaheadSteps = 600;

    [Range(1, 20)] public int stepsPerPt = 5;

    public float cullingRadius = 600f;

    private LineRenderer line;

    [Header("ライン表示")]
    [Tooltip("1ワールド単位あたりの繰り返し数")]
    [SerializeField] private float textureTiling = 1f;

    [Tooltip("スクロール速度。0で停止")]
    [SerializeField] private float scrollSpeed = 0.5f;

    [Tooltip("進行方向に流す")]
    [SerializeField] private bool scrollForward = true;

    [Header("衝突予測")]
    [Tooltip("衝突が予測された地点で軌道を打ち切る")]
    [SerializeField] private bool stopOnImpact = true;

    [Tooltip("この半径未満の天体は衝突判定から除外")]
    [SerializeField] private float minImpactRadius = 0.05f;

    [Tooltip("SetTargetParamで与えた仮想天体の半径")]
    [SerializeField] private float previewRadius = 0.25f;

    /// <summary>今フレームの予測で衝突が発生したか</summary>
    public bool HasImpact { get; private set; }

    /// <summary>衝突地点（ワールド座標）</summary>
    public Vector2 ImpactPoint { get; private set; }

    /// <summary>衝突までの予測時間（秒）</summary>
    public float ImpactTime { get; private set; }

    /// <summary>衝突相手。仮想天体の場合はnull</summary>
    public GravBody ImpactBody { get; private set; }

    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    private MaterialPropertyBlock mpb;
    private float scrollOffset;

    private Vector2[] pos, vel, acc;
    private float[] mass;
    private bool[] anchored;
    private int[] kind;
    private int[] orbitIndex;
    private float[] softRadii;
    private float[] radii;
    private Vector3[] points;
    private int targetIndex;

    // 衝突判定用
    private Vector2[] prevPos;
    private int[] collidables;
    private bool[] ignoreOverlap;
    private int collidableCount;
    public Vector2 ImpactNormal { get; private set; } // 接触面の法線
    public Vector2 ImpactOffset { get; private set; } // 衝突相手からの相対位置

    private Vector2 targetPos, targetVel, targetAcc;
    private bool isParamSet = false;

    private int drawnCnt;

    public Vector2 ImpactOtherPos { get; private set; } // 衝突時の相手の予測位置
    public float ImpactGap { get; private set; } // 検証用

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;

        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.textureMode = LineTextureMode.Tile; // 軌道が伸びてもダッシュのサイズが一定
        mpb = new MaterialPropertyBlock();

        EnsureBuffers(8); // 初期確保。足りなければ後で自動的に拡張される
    }

    private void FixedUpdate()
    {
        Predict();
    }

    private void LateUpdate()
    {
        if (points == null)
        {
            line.positionCount = 0;
            return;
        }

        line.positionCount = drawnCnt;
        line.SetPositions(points);

        ApplyTextureScroll();
    }

    private void Predict()
    {
        HasImpact = false;
        ImpactBody = null;

        var bodies = GravSystem.GravBodies;
        GravSystem system = GravSystem.Instance;

        if (system == null || bodies == null)
        {
            drawnCnt = 0;
            return;
        }

        // バッファサイズを確保
        EnsureBuffers(bodies.Count + 1);

        targetIndex = -1;
        int bodyCount = bodies.Count;

        for (int i = 0; i < bodies.Count; i++)
        {
            pos[i] = bodies[i].position;
            vel[i] = bodies[i].velocity;
            acc[i] = bodies[i].acceleration;
            mass[i] = bodies[i].mass;
            radii[i] = bodies[i].collisionRadius;
            softRadii[i] = bodies[i].softeningRadius;
            kind[i] = bodies[i].CompareTag("Player") ? GravSystem.KindPlayer
                : bodies[i].CompareTag("Satellite") ? GravSystem.KindSatellite
                : GravSystem.KindNormal;

            anchored[i] = bodies[i].isAnchored;
            if (bodies[i] == target) targetIndex = i;
        }

        //衛星の親をインデックスに変換
        for (int i = 0; i < bodies.Count; i++)
        {
            orbitIndex[i] = -1;

            if (kind[i] != GravSystem.KindSatellite)
            {
                continue;
            }

            GravBody parent = bodies[i].GetOrbitingBody();

            for (int j = 0; j < bodies.Count; j++)
            {
                if (bodies[j] == parent)
                {
                    orbitIndex[i] = j;
                    break;
                }
            }
        }

        if (targetIndex < 0)
        {
            if (!isParamSet)
            {
                drawnCnt = 0;
                return;
            }

            pos[bodies.Count] = targetPos;
            acc[bodies.Count] = targetAcc;
            vel[bodies.Count] = targetVel;
            mass[bodies.Count] = 0;
            anchored[bodies.Count] = false;
            radii[bodies.Count] = previewRadius;
            kind[bodies.Count] = GravSystem.KindPlayer;
            orbitIndex[bodies.Count] = -1;
            softRadii[bodies.Count] = 0f;
            bodyCount = bodies.Count + 1;
            targetIndex = bodies.Count;
        }

        float dt = Time.fixedDeltaTime / Mathf.Max(1, system.substeps);
        float halfStep = dt / 2f;
        float gConstant = system.gravitationalConstant;
        float soft = system.softening;
        float cullSqr = cullingRadius * cullingRadius;

        if (stopOnImpact) BuildCollidableList(bodyCount);

        system.ComputeAccelerations(pos, mass, anchored, kind, orbitIndex, softRadii, acc, bodyCount, gConstant, soft);

        drawnCnt = 0;

        points[drawnCnt++] = ToVec3(pos[targetIndex]);

        //仮物理を計算する
        for (int step = 1; step <= lookaheadSteps; step++)
        {
            if (stopOnImpact) Array.Copy(pos, prevPos, bodyCount);

            for (int i = 0; i < bodyCount; i++)
            {
                if (anchored[i]) continue;

                vel[i] += acc[i] * halfStep;
                pos[i] += vel[i] * dt;
            }

            system.ComputeAccelerations(pos, mass, anchored, kind, orbitIndex, softRadii, acc, bodyCount, gConstant, soft);

            for (int i = 0; i < bodyCount; i++)
            {
                if (anchored[i]) continue;
                vel[i] += acc[i] * halfStep;
            }

            if (stopOnImpact && TryFindImpact(targetIndex, out int hitIndex, out float hitT))
            {
                Vector2 impact = Vector2.Lerp(prevPos[targetIndex], pos[targetIndex], hitT);
                Vector2 otherAt = Vector2.Lerp(prevPos[hitIndex], pos[hitIndex], hitT);

                Vector2 delta = impact - otherAt;

                ImpactNormal = delta.sqrMagnitude > 0.0001 ? delta.normalized : Vector2.up;
                ImpactOffset = ImpactNormal * radii[hitIndex];

                HasImpact = true;
                ImpactPoint = impact;
                ImpactTime = (step - 1 + hitT) * dt;
                ImpactBody = hitIndex < bodies.Count ? bodies[hitIndex] : null;

                ImpactOtherPos = otherAt;
                ImpactGap = delta.magnitude - (radii[targetIndex] + radii[hitIndex]);

                if (drawnCnt < points.Length) points[drawnCnt++] = ToVec3(impact);

                break;
            }

            if (step % stepsPerPt != 0) continue;

            Vector2 targetPos = pos[targetIndex];

            if (targetPos.sqrMagnitude > cullSqr) break;

            points[drawnCnt++] = ToVec3(targetPos);
            if (drawnCnt >= points.Length) break;
        }
    }

    /// <summary>
    /// 衝突しうる天体だけを毎フレーム1回だけ絞り込む
    /// </summary>
    private void BuildCollidableList(int inBodyCount)
    {
        collidableCount = 0;
        float selfRadius = radii[targetIndex];

        for (int i = 0; i < inBodyCount; i++)
        {
            if (i == targetIndex || kind[i] != GravSystem.KindNormal || radii[i] < minImpactRadius) continue;

            collidables[collidableCount++] = i;

            // 発射直後など、最初から重なっている相手は離れるまで無視する
            float combined = selfRadius + radii[i];
            ignoreOverlap[i] = (pos[i] - pos[targetIndex]).sqrMagnitude < combined * combined;
        }
    }

    /// <summary>
    /// 直前ステップから現ステップまでの移動区間で、最も早い衝突を探す
    /// </summary>
    private bool TryFindImpact(int self, out int hitIndex, out float hitT)
    {
        hitIndex = -1;
        hitT = float.MaxValue;

        Vector2 selfPrev = prevPos[self];
        Vector2 selfNext = pos[self];
        float selfRadius = radii[self];

        for (int i = 0; i < collidableCount; i++)
        {
            int colIndex = collidables[i];
            float combined = selfRadius + radii[colIndex];

            // 相手基準の相対運動で見る（相手も動くため）
            Vector2 toRel = selfNext - pos[colIndex];

            if (ignoreOverlap[colIndex])
            {
                // 十分離れた時点で判定を有効化
                if (toRel.sqrMagnitude > combined * combined) ignoreOverlap[colIndex] = false;

                continue;
            }

            Vector2 fromRel = selfPrev - prevPos[colIndex];

            if (SegmentHitsCircle(fromRel, toRel, combined, out float t) && t < hitT)
            {
                hitT = t;
                hitIndex = colIndex;
            }
        }

        return hitIndex >= 0;
    }

    /// <summary>
    /// 原点を中心とする半径radiusの円と線分fromRel→toRelの交差判定
    /// </summary>
    /// <param name="t">交差した位置（0〜1）</param>
    /// <param name="fromRel">　開始時点の相対位置</param>
    /// <param name="toRel">終了時点の相対位置</param>
    private static bool SegmentHitsCircle(Vector2 fromRel, Vector2 toRel, float radius, out float t)
    {
        Vector2 d = toRel - fromRel;
        float a = Vector2.Dot(d, d);
        float b = 2f * Vector2.Dot(fromRel, d);
        float c = Vector2.Dot(fromRel, fromRel) - radius * radius;

        t = 0f;

        if (c <= 0f) return true; // ステップ開始時点で既に接触

        if (a <= Mathf.Epsilon) return false; // 相対的に静止

        float disc = b * b - 4f * a * c;

        if (disc < 0f) return false;

        float root = (-b - Mathf.Sqrt(disc)) / (2f * a);

        if (root < 0f || root > 1f) return false;

        t = root;
        return true;
    }

    //-----------------------------------HELPERS

    public void SetTargetParam(Vector2 inPos, Vector2 inVel, Vector2 inAcc)
    {
        isParamSet = true;
        targetAcc = inAcc;
        targetVel = inVel;
        targetPos = inPos;
        Predict();
    }

    private Vector3 ToVec3(Vector2 p)
    {
        return new Vector3(p.x, p.y, transform.position.z);
    }

    private void EnsureBuffers(int inCount)
    {
        if (pos == null || pos.Length < inCount)
        {
            pos = new Vector2[inCount];
            vel = new Vector2[inCount];
            acc = new Vector2[inCount];
            mass = new float[inCount];
            anchored = new bool[inCount];
            radii = new float[inCount];
            prevPos = new Vector2[inCount];
            collidables = new int[inCount];
            ignoreOverlap = new bool[inCount];
            kind = new int[inCount];
            orbitIndex = new int[inCount];
            softRadii = new float[inCount];
        }

        int maxPoints = lookaheadSteps / Mathf.Max(1, stepsPerPt) + 2;
        if (points == null || points.Length < maxPoints) points = new Vector3[maxPoints];
    }

    private void ApplyTextureScroll()
    {
        if (scrollSpeed != 0f)
        {
            scrollOffset += (scrollForward ? -scrollSpeed : scrollSpeed) * Time.deltaTime;
            scrollOffset = Mathf.Repeat(scrollOffset, 1f); // 精度落ちを防ぐ
        }

        line.GetPropertyBlock(mpb);
        mpb.SetVector(BaseMapST, new Vector4(textureTiling, 1f, scrollOffset, 0f));
        line.SetPropertyBlock(mpb);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !HasImpact) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ImpactOtherPos, ImpactBody != null ? ImpactBody.collisionRadius : 0.25f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ImpactPoint, previewRadius);
    }
}