using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StageSelectManager : MonoBehaviour
{
    //----------------------------
    // パラメータ
    //----------------------------
    [Header("ステージ選択の半径と回転角")]
    [Tooltip("ステージ選択が並ぶ円の半径")]
    [Min(0f)]
    [SerializeField] private float radius = 5f;
    [Tooltip("ステージ選択の一回の操作での回転角")]
    [Range(0f, 360f)]
    [SerializeField] private float angle = 45f;

    [Header("滑らかに回転")]
    [Tooltip("trueの場合、回転が滑らかになります。falseの場合、瞬時に回転します。")]
    [SerializeField] private bool smoothRotate = true;

    [Tooltip("滑らかに回転する場合の回転にかかる時間（秒）")]
    [Min(0f)]
    [SerializeField] private float rotateDuration = 0.5f;

    //----------------------------
    // 変数
    //----------------------------
    //子オブジェクトの数を取得する
    private int childCount;

    //子オブジェクトへの参照を格納する配列
    private List<Transform> stageSelects = new List<Transform>();

    //----------------------------
    // イベント
    //----------------------------
    // ステージ選択の回転イベント 引数は回転角度
    public event Action<float> OnStageLotate;

    //----------------------------
    // 関数
    //----------------------------

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 子オブジェクトの数を取得
        childCount = transform.childCount;
        // 子オブジェクトへの参照を格納する配列を初期化して新規に格納
        stageSelects.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (child != null)
            {
                stageSelects.Add(child);
            }
        }

        // 子オブジェクトを円形に配置
        ArrangeChildrenInCircle(radius);

        // 回転イベントにメソッドを登録
        OnStageLotate += RotateCircle;
    }

    // Update is called once per frame
    void Update()
    {
        // 回転の入力を取得
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (smoothRotate)
            {
                RotateCircleSmoothly(angle, rotateDuration);
            }
            else
            {
                RotateCircle(angle);
            }
        }
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (smoothRotate)
            {
                RotateCircleSmoothly(-angle, rotateDuration);
            }
            else
            {
                RotateCircle(angle);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        // 円の描画
        Gizmos.DrawWireSphere(transform.position, radius);
        // 子オブジェクトが配置される位置に円を描画
        for (int i = 0; i < childCount; i++)
        {
            float angle = i * Mathf.PI * 2f / childCount;
            Vector3 newPos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawSphere(transform.position + newPos, 0.1f);
        }
    }


    //子オブジェクトを円形に再配置するメソッド
    private void ArrangeChildrenInCircle(float radius)
    {
        // 子オブジェクトを円形に配置
        for (int i = 0; i < childCount; i++)
        {
            Transform child = stageSelects[i];
            float angle = i * Mathf.PI * 2f / childCount;
            Vector3 newPos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            child.localPosition = newPos;
        }
    }

    // 子オブジェクトを丸ごと回転させるメソッド
    public void RotateCircle(float angle)
    {
        // 回転角度をラジアンに変換
        float rad = angle * Mathf.Deg2Rad;
        // 子オブジェクトを回転させる
        for (int i = 0; i < childCount; i++)
        {
            Transform child = stageSelects[i];
            Vector3 pos = child.localPosition;
            float newX = pos.x * Mathf.Cos(rad) - pos.y * Mathf.Sin(rad);
            float newY = pos.x * Mathf.Sin(rad) + pos.y * Mathf.Cos(rad);
            child.localPosition = new Vector3(newX, newY, pos.z);
        }
    }

    // 子オブジェクトを丸ごと回転させるメソッド（スムーズに回転）
    public void RotateCircleSmoothly(float angle, float duration)
    {
        StartCoroutine(RotateCircle(angle, duration));
    }
    // コルーチン
    IEnumerator RotateCircle(float angle, float duration)
    {
        float elapsed = 0f;
        float startAngle = 0f;

        while (elapsed < duration)
        {
            float currentAngle = Mathf.Lerp(startAngle, angle, elapsed / duration);
            RotateCircle(currentAngle - startAngle);
            startAngle = currentAngle;
            elapsed += Time.deltaTime;
            yield return null;
        }

        RotateCircle(angle - startAngle);
    }
}
