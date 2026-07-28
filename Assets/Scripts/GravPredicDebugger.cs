using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 予測モデルが実際のシミュレーションとどれだけズレるかを計測する。
/// GravSystemの後に実行される必要があるのでExecutionOrderを大きめに設定。
/// </summary>
[DefaultExecutionOrder(200)]
public class GravPredictionDebugger : MonoBehaviour
{
    [Header("検証設定")]
    [Tooltip("何ティック先を検証するか。1にするとモデルの忠実度、100前後にすると累積誤差を見る")]
    public int horizonTicks = 100;

    [Tooltip("この誤差(ワールド単位)を超えた天体を警告として出す")]
    public float errorThreshold = 0.1f;

    [Tooltip("検証を繰り返す間隔(ティック)。0なら前回の検証終了後すぐ再開")]
    public int repeatDelayTicks = 0;

    [Header("モデル比較")]
    [Tooltip("ONでpairSoft＋タグ除外を再現した忠実モデル、OFFでTrajectoryPredictorと同じ簡易モデル")]
    public bool useFaithfulModel = false;

    [Header("プレビュー天体の注入")]
    [Tooltip("仮想天体を混ぜて、それが惑星を引っ張るかどうかを検証する")]
    public bool injectPreviewBody = false;

    [Tooltip("仮想天体の質量。0ならテスト粒子として振る舞う")]
    public float previewMass = 1f;

    public Vector2 previewPos;
    public Vector2 previewVel;

    [Header("起動時の天体一覧")]
    [Tooltip("最初のティックで全天体のパラメータをログに出す")]
    public bool logBodyTableOnStart = true;

    // 予測用バッファ
    private Vector2[] pos, vel, acc;
    private float[] mass, softRadii;
    private bool[] anchored;
    private int[] kind, orbitIndex;

    // 検証中の状態
    private GravBody[] tracked;
    private Vector2[] predictedPos;
    private int trackedCount;
    private int ticksRemaining = -1;
    private int delayRemaining;
    private bool tableLogged;

    private const int KindNormal = 0;
    private const int KindPlayer = 1;
    private const int KindSatellite = 2;

    private void FixedUpdate()
    {
        var bodies = GravSystem.GravBodies;
        var system = GravSystem.Instance;

        if (system == null || bodies == null || bodies.Count == 0) return;

        if (logBodyTableOnStart && !tableLogged)
        {
            tableLogged = true;
            LogBodyTable(bodies, system);
        }

        if (ticksRemaining > 0)
        {
            ticksRemaining--;
            if (ticksRemaining == 0)
            {
                CompareAndLog(system);
                ticksRemaining = -1;
                delayRemaining = repeatDelayTicks;
            }

            return;
        }

        if (delayRemaining > 0)
        {
            delayRemaining--;
            return;
        }

        StartPrediction(bodies, system);
    }

    /// <summary>
    /// 現在の状態からhorizonTicks先を予測して保存する
    /// </summary>
    private void StartPrediction(IReadOnlyList<GravBody> bodies, GravSystem system)
    {
        var realCount = bodies.Count;
        var total = realCount + (injectPreviewBody ? 1 : 0);

        EnsureBuffers(total);

        for (var i = 0; i < realCount; i++)
        {
            var body = bodies[i];

            pos[i] = body.position;
            vel[i] = body.velocity;
            acc[i] = body.acceleration;
            mass[i] = body.mass;
            anchored[i] = body.isAnchored;
            softRadii[i] = body.softeningRadius;

            kind[i] = body.CompareTag("Player") ? KindPlayer
                : body.CompareTag("Satellite") ? KindSatellite
                : KindNormal;

            tracked[i] = body;
        }

        // 衛星の親をインデックスに変換
        for (var i = 0; i < realCount; i++)
        {
            orbitIndex[i] = -1;

            if (kind[i] != KindSatellite) continue;

            var parent = bodies[i].GetOrbitingBody();

            for (var j = 0; j < realCount; j++)
                if (bodies[j] == parent)
                {
                    orbitIndex[i] = j;
                    break;
                }
        }

        if (injectPreviewBody)
        {
            pos[realCount] = previewPos;
            vel[realCount] = previewVel;
            acc[realCount] = Vector2.zero;
            mass[realCount] = previewMass;
            anchored[realCount] = false;
            softRadii[realCount] = 0f;
            kind[realCount] = KindPlayer;
            orbitIndex[realCount] = -1;
        }

        trackedCount = realCount;

        // GravSystemと同じ積分手順で回す
        var substeps = Mathf.Max(1, system.substeps);
        var dt = Time.fixedDeltaTime / substeps;
        var halfStep = dt / 2f;

        ComputeAcc(system, total);

        for (var tick = 0; tick < horizonTicks; tick++)
        for (var s = 0; s < substeps; s++)
        {
            for (var i = 0; i < total; i++)
            {
                if (anchored[i]) continue;
                vel[i] += acc[i] * halfStep;
                pos[i] += vel[i] * dt;
            }

            ComputeAcc(system, total);

            for (var i = 0; i < total; i++)
            {
                if (anchored[i]) continue;
                vel[i] += acc[i] * halfStep;
            }
        }

        for (var i = 0; i < realCount; i++) predictedPos[i] = pos[i];

        ticksRemaining = horizonTicks;
    }

    /// <summary>
    /// horizonTicks経過後、予測位置と実位置を比較する
    /// </summary>
    private void CompareAndLog(GravSystem system)
    {
        var sb = new StringBuilder();
        var horizonSeconds = horizonTicks * Time.fixedDeltaTime;

        sb.AppendLine($"=== 予測誤差 : {horizonTicks}ティック ({horizonSeconds:F2}秒) 先 ===");
        sb.AppendLine($"モデル: {(useFaithfulModel ? "忠実(pairSoft+タグ除外)" : "簡易(TrajectoryPredictorと同じ)")}"
                      + $" / プレビュー天体: {(injectPreviewBody ? $"あり 質量={previewMass}" : "なし")}");
        sb.AppendLine($"substeps={system.substeps}  G={system.gravitationalConstant}  softening={system.softening}");

        var worstError = 0f;
        var worstName = "-";

        for (var i = 0; i < trackedCount; i++)
        {
            var body = tracked[i];

            if (body == null)
            {
                sb.AppendLine($"  [{i}] (破棄済み)");
                continue;
            }

            var actual = body.position;
            var error = Vector2.Distance(actual, predictedPos[i]);

            // その天体自身がどれだけ動いたかに対する相対誤差
            var travelled = actual.magnitude > 0.001f ? actual.magnitude : 1f;
            var flag = error > errorThreshold ? "  <<< 誤差大" : "";

            sb.AppendLine($"  [{i}] {body.name,-16} tag={body.tag,-10} anchored={body.isAnchored,-5}"
                          + $" pred={predictedPos[i]} actual={actual} 誤差={error:F4}{flag}");

            if (error > worstError)
            {
                worstError = error;
                worstName = body.name;
            }
        }

        sb.AppendLine($"最大誤差: {worstError:F4} ({worstName})");

        if (worstError > errorThreshold)
            Debug.LogWarning(sb.ToString());
        else
            Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 起動時に全天体のパラメータを一覧で出す
    /// </summary>
    private void LogBodyTable(IReadOnlyList<GravBody> bodies, GravSystem system)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"=== 天体一覧 ({bodies.Count}体) ===");
        sb.AppendLine($"G={system.gravitationalConstant} softening={system.softening} "
                      + $"substeps={system.substeps} externalField={system.externalField} "
                      + $"fixedDeltaTime={Time.fixedDeltaTime}");

        for (var i = 0; i < bodies.Count; i++)
        {
            var body = bodies[i];
            var parent = body.GetOrbitingBody();

            sb.AppendLine($"  [{i}] {body.name,-16} tag={body.tag,-10}"
                          + $" mass={body.mass,-8:F3} collR={body.collisionRadius,-6:F3}"
                          + $" softR={body.softeningRadius,-6:F3} anchored={body.isAnchored,-5}"
                          + $" 親={(parent != null ? parent.name : "なし")}"
                          + $" pos={body.position} vel={body.velocity}");
        }

        Debug.Log(sb.ToString());
    }

    private void ComputeAcc(GravSystem system, int count)
    {
        if (useFaithfulModel)
            ComputeAccFaithful(system, count);
        else
            system.ComputeAccelerations(pos, mass, anchored, kind, orbitIndex, softRadii, acc, count,
                system.gravitationalConstant, system.softening);
    }

    /// <summary>
    /// ライブ版のComputeAccelerationsを配列で再現したもの
    /// </summary>
    private void ComputeAccFaithful(GravSystem system, int count)
    {
        for (var i = 0; i < count; i++) acc[i] = Vector2.zero;

        var gravConst = system.gravitationalConstant;
        var globalSoft = system.softening;

        for (var i = 0; i < count; i++)
        for (var j = i + 1; j < count; j++)
        {
            if (kind[i] == KindPlayer && kind[j] == KindPlayer) continue;
            if (kind[i] == KindSatellite && orbitIndex[i] != j) continue;
            if (kind[j] == KindSatellite && orbitIndex[j] != i) continue;

            var pairSoft = Mathf.Max(globalSoft, Mathf.Max(softRadii[i], softRadii[j]));
            var softSqr = pairSoft * pairSoft;

            var displacement = pos[j] - pos[i];
            var rSqr = displacement.sqrMagnitude + softSqr;
            var invRCubed = 1f / (rSqr * Mathf.Sqrt(rSqr));
            var unitForce = gravConst * invRCubed * displacement;

            if (!anchored[i]) acc[i] += unitForce * mass[j];
            if (!anchored[j]) acc[j] -= unitForce * mass[i];
        }

        if (system.externalField != Vector2.zero)
            for (var i = 0; i < count; i++)
                if (!anchored[i])
                    acc[i] += system.externalField;
    }

    private void EnsureBuffers(int inCount)
    {
        if (pos != null && pos.Length >= inCount) return;

        pos = new Vector2[inCount];
        vel = new Vector2[inCount];
        acc = new Vector2[inCount];
        mass = new float[inCount];
        anchored = new bool[inCount];
        softRadii = new float[inCount];
        kind = new int[inCount];
        orbitIndex = new int[inCount];
        tracked = new GravBody[inCount];
        predictedPos = new Vector2[inCount];
    }
}