using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

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

    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    private MaterialPropertyBlock mpb;
    private float scrollOffset;

    private Vector2[] pos, vel, acc;
    private float[] mass;
    private bool[] anchored;
    private Vector3[] points;
    private int targetIndex;

    private Vector2 targetPos, targetVel, targetAcc;
    private bool isParamSet = false;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;

        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.textureMode = LineTextureMode.Tile;   // 軌道が伸びてもダッシュのサイズが一定
        mpb = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        IReadOnlyList<GravBody> bodies = GravSystem.GravBodies;
        GravSystem system = GravSystem.Instance;

        if (system == null || bodies == null)
        {
            line.positionCount = 0;
            return;
        }

        // バッファサイズを確保
        EnsureBuffers(bodies.Count + 1);

        targetIndex = -1;

        for (int i = 0;  i < bodies.Count; i++)
        {
            pos[i] = bodies[i].position;
            vel[i] = bodies[i].velocity;
            acc[i] = bodies[i].acceleration;
            mass[i] = bodies[i].mass;

            anchored[i] = bodies[i].isAnchored;
            if (bodies[i] == target)
            {
                targetIndex = i;
            } 
        }

        if (targetIndex < 0)
        {
            if (!isParamSet)
            {
                line.positionCount = 0;
                return;
            }

            pos[bodies.Count] = targetPos;
            acc[bodies.Count] = targetAcc;
            vel[bodies.Count] = targetVel;
            mass[bodies.Count] = 1;
            anchored[bodies.Count] = false;
            targetIndex = bodies.Count;
        }

        float dt = Time.fixedDeltaTime / Mathf.Max(1, system.substeps);
        float halfStep = dt / 2f;
        float gConstant = system.gravitationalConstant;
        float soft = system.softening;
        float cullSqr = cullingRadius * cullingRadius;

        GravSystem.Instance.ComputeAccelerations(pos, mass, anchored, acc, bodies.Count + 1, gConstant, soft);

        int drawn = 0;

        points[drawn++] = ToVec3(pos[targetIndex]);

        //仮物理を計算する
        for (int step = 1; step <= lookaheadSteps; step++)
        {
            for (int i = 0; i < bodies.Count + 1; i++)
            {
                if (anchored[i])
                {
                    continue;
                }

                vel[i] += acc[i] * halfStep;
                pos[i] += vel[i] * dt;
            }

            GravSystem.Instance.ComputeAccelerations(pos, mass, anchored, acc, bodies.Count + 1, gConstant, soft);

            for (int i = 0; i < bodies.Count + 1; i++)
            {
                if (anchored[i]) continue;
                vel[i] += acc[i] * halfStep;
            }

            if (step % stepsPerPt != 0)
            {
                continue;
            }

            Vector2 targetPos = pos[targetIndex];

            if (targetPos.sqrMagnitude > cullSqr)
            {
                break;
            }

            points[drawn++] = ToVec3(targetPos);
            if (drawn >= points.Length)
            {
                break;
            }
        }

        line.positionCount = drawn;
        line.SetPositions(points);

        ApplyTextureScroll();
    }

    public void SetTargetParam(Vector2 inPos, Vector2 inVel, Vector2 inAcc)
    {
        isParamSet = true;
        targetAcc = inAcc;
        targetVel = inVel;
        targetPos = inPos;
    }

    Vector3 ToVec3(Vector2 p)
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
        }

        int maxPoints = lookaheadSteps / Mathf.Max(1, stepsPerPt) + 2;
        if (points == null || points.Length < maxPoints)
        {
            points = new Vector3[maxPoints];
        }
    }

    private void ApplyTextureScroll()
    {
        if (scrollSpeed != 0f)
        {
            scrollOffset += (scrollForward ? -scrollSpeed : scrollSpeed) * Time.deltaTime;
            scrollOffset = Mathf.Repeat(scrollOffset, 1f);   // 精度落ちを防ぐ
        }

        line.GetPropertyBlock(mpb);
        mpb.SetVector(BaseMapST, new Vector4(textureTiling, 1f, scrollOffset, 0f));
        line.SetPropertyBlock(mpb);
    }
}
