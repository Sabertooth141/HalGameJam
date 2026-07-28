using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//構造体
namespace RocketNamespace
{
    struct RocketData
    {
        public Vector2 position; // 位置
        public float angle; // 角度
        public float speed; // 速度
    }
}


public class RocketManager : MonoBehaviour
{
    //----------------------------
    //シングルトン
    //----------------------------
    public static RocketManager Instance { get; private set; }

    //----------------------------
    // パラメータ
    //----------------------------
    //ロケットのプレハブを格納する変数
    [Header("ロケットのプレハブ")]
    [Tooltip("生成するロケットのプレハブを指定する")]
    [SerializeField] private GameObject rocketPrefab; // ロケットのプレハブ

    //----------------------------
    // 変数
    //----------------------------
    //動いているロケットのコントローラーのリスト
    private readonly HashSet<RocketNamespace.RocketData> history = new HashSet<RocketNamespace.RocketData>();
    //動いているロケットのコントローラーのリスト
    private readonly HashSet<RocketController> active = new HashSet<RocketController>();
    //現在動作中のロケットのコントローラー
    public RocketController Current { get; private set; }

    // ロケットの発射状態を管理する列挙型
    enum LaunchState
    {
        Ready, //発射準備完了
        Launched, //発射済み
        Landed //着地済み
    }
    // 現在のロケットの発射状態
    private LaunchState currentLaunchState = LaunchState.Ready;

    //イベントの宣言
    public event Action OnCreateRocket;

    //----------------------------
    // 関数
    //----------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // シーンをまたいでも破棄されないようにしたい場合
        // DontDestroyOnLoad(gameObject);
    }

    //登録
    public void Register(RocketController rocket)
    {
        if (!active.Add(rocket)) return;
        if (Current == null) Current = rocket;
        rocket.OnDestroyed += () => Unregister(rocket); // 破棄時に自動解除
    }

    //解除
    public void Unregister(RocketController rocket)
    {
        if (!active.Remove(rocket)) return;
        if (Current == rocket) Current = active.FirstOrDefault();
    }

    //新しいロケットを生成して登録する
    public RocketController CreateRocket(Vector2 position, float angle, float speed)
    {
        var rocketObj = Instantiate(rocketPrefab, position, Quaternion.Euler(0, 0, angle));
        var rocketController = rocketObj.GetComponent<RocketController>();
        Register(rocketController);
        rocketController.SetParameters(angle, speed);
        currentLaunchState = LaunchState.Launched; // 発射状態を更新
        Current = rocketController; // 新しいロケットを現在のロケットとして設定
        OnCreateRocket?.Invoke(); // イベントを発火
        return rocketController;
    }
}
