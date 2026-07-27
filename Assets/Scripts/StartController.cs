using UnityEngine;
using UnityEngine.InputSystem;

public class StartController : MonoBehaviour
{
    //シングルトン
    public static StartController Instance { get; private set; }
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

    //ロケットマネージャーへの参照
    private RocketManager rocketManager;
    private RocketController rocketController;

    [Header("ロケットの発射角度")]
    [Tooltip("ロケットの発射角度")]
    [Range(180f, -180f)]
    [SerializeField] private float launchAngle = 0f;
    
    [Tooltip("発射角０を基準とした、ロケットの発射角の範囲")]
    [SerializeField] private float launchAngleRange = 45f;

    [Tooltip("ロケットの発射初速度")]
    [SerializeField] private float launchSpeed = 45f;

    //ロケットの発射方法のモードを管理する変数
    enum LaunchMode
    {
        Manual, //手動発射モード
        Automatic //時間経過で自動発射モード
    }

    [Header("ロケットの発射方法のモード")]
    [Tooltip("ロケットの発射方法のモード")]
    [SerializeField] private LaunchMode launchMode = LaunchMode.Manual; // 手動発射モードかどうか

    //ロケットの発射モードが自動発射モードの場合のみ
    [Tooltip("自動発射モードの発射間隔")]
    [SerializeField] private float autoLaunchInterval = 5f; // 自動発射モードの発射間隔
    private float timer = 0f; // タイマー


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //シングルトンの初期化
        rocketManager = RocketManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (launchMode == LaunchMode.Automatic)
        {
            //自動発射モードの場合、一定時間ごとにロケットを発射する
            //if (rocketController == null)
            //{
                if (timer >= autoLaunchInterval)
                {
                    GenerateRocket();
                    timer = 0f;
                }
            //}
        }
    }

    // 発射角のセッターとゲッター
    public float LaunchAngle
    {
        get { return launchAngle; }
        set { launchAngle = value; }
    }

    //発射角を指定の値ずつ変更するメソッド
    public void ChangeLaunchAngle(float delta)
    {
        launchAngle += delta;
        //発射角の範囲を制限する
        launchAngle = Mathf.Clamp(launchAngle, -launchAngleRange, launchAngleRange);
    }

    // 発射初速度のセッターとゲッター
    public float LaunchSpeed
    {
        get { return launchSpeed; }
        set { launchSpeed = value; }
    }

    //ロケットを生成して発射するメソッド
    private void GenerateRocket()
    {
        rocketController = rocketManager.CreateRocket(transform.position, launchAngle, launchSpeed);
    }


}
