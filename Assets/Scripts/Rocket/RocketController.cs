using System;
using UnityEngine;

public class RocketController : MonoBehaviour
{
    //----------------------------
    // パラメータ
    //----------------------------

    [Header("ロケットのパラメータ")]
    [Tooltip("ロケットの現在角度")]
    [SerializeField] private float angle; // 角度
    [Tooltip("ロケットの現在速度")]
    [SerializeField] private float speed; // 速度
    [Tooltip("ロケットの最大速度")]
    [SerializeField] private float maxSpeed;

    // 破棄通知イベント
    public event Action OnDestroyed;
    
    //----------------------------
    // 変数
    //----------------------------
    private bool isDestroyed;

    //----------------------------
    // 関数
    //----------------------------
    public void DestroyRocket()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        OnDestroyed?.Invoke();   // 登録先(Manager)に通知
        Destroy(gameObject);     // 実際にオブジェクト破棄
    }

    private void OnDestroy()
    {
        // 直接Destroyされた場合にも通知したいならここでも可
        if (!isDestroyed) OnDestroyed?.Invoke();
    }

    //パラメータをセットするメソッド
    public void SetParameters(float angle, float speed)
    {
        this.angle = angle;
        this.speed = speed;
    }

    //角度と速さから速度ベクトルを計算するメソッド
    public Vector2 CalculateVelocity()
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
    }

    //角度と速さから速度ベクトルを計算するメソッド
    public Vector2 CalculateVelocity()
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
    }
}
