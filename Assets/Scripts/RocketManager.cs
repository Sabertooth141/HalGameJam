using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RocketManager : MonoBehaviour
{
    //----------------------------
    //シングルトン
    public static RocketManager Instance { get; private set; }
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
    //----------------------------

    //ロケットのプレハブを格納する変数
    [Header("ロケットのプレハブ")]
    [SerializeField] private GameObject rocketPrefab; // ロケットのプレハブ

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
        Current = rocketController; // 新しいロケットを現在のロケットとして設定
        return rocketController;
    }
    
}
