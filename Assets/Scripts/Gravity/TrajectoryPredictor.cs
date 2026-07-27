using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class TrajectoryPredictor : MonoBehaviour
{
    public GravBody target;

    [Range(20, 3000)] public int lookaheadSteps = 600;

    [Range(1, 20)] public int stepsPerPt = 20;

    public float cullingRadius = 600f;

    private LineRenderer line;

    private Vector2[] pos, vel, acc;
    private float[] mass;
    private bool[] anchored;
    private Vector3[] points;
    private int targetIndex;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;

        if (target == null)
        {
            target = GetComponentInParent<GravBody>();
        }
    }

    private void LateUpdate()
    {
        IReadOnlyList<GravBody> bodies = GravSystem.GravBodies;
        GravSystem system = GravSystem.Instance;

        if (target == null || system == null || bodies == null)
        {
            line.positionCount = 0;
            return;
        }

        // バッファサイズを確保
        EnsureBuffers(bodies.Count);

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
            line.positionCount = 0;
            return;
        }

        float dt = Time.fixedDeltaTime / Mathf.Max(1, system.substeps);
        float halfStep = dt / 2f;
        float gConstant = system.gravitationalConstant;
        float soft = system.softening;
        float cullSqr = cullingRadius * cullingRadius;

        GravSystem.ComputeAccelerations(pos, mass, anchored, acc, bodies.Count, gConstant, soft);

        int drawn = 0;

        points[drawn++] = ToVec3(pos[targetIndex]);

        //仮物理を計算する
        for (int step = 1; step <= lookaheadSteps; step++)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                if (anchored[i])
                {
                    continue;
                }

                vel[i] += acc[i] * halfStep;
                pos[i] += vel[i] * dt;
            }

            GravSystem.ComputeAccelerations(pos, mass, anchored, acc, bodies.Count, gConstant, soft);

            for (int i = 0; i < bodies.Count; i++)
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
}
