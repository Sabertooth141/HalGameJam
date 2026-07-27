using UnityEngine;
using UnityEngine.InputSystem;

public class StartController : MonoBehaviour
{
    //----------------------------
    //シングルトン
    //----------------------------
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
    // 参照
    //----------------------------
    private RocketManager rocketManager;
    private RocketController rocketController;
    private GravBody rocketGravBody;

    //----------------------------
    // パラメータ
    //----------------------------
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

    //----------------------------
    //関数
    //----------------------------

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //シングルトンの初期化
        rocketManager = RocketManager.Instance;
        rocketController = rocketManager.Current;
        rocketGravBody = rocketController?.GetComponent<GravBody>();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (launchMode == LaunchMode.Manual)
        {
            //まだなにもない
        }
        else if (launchMode == LaunchMode.Automatic)
        {
            //自動発射モードの場合、一定時間ごとにロケットを発射する
            if (timer >= autoLaunchInterval)
            {
                if (rocketController != null)
                {
                    rocketController.DestroyRocket(); // 既存のロケットを破棄
                }

                GenerateRocket();
                Launch();
                timer = 0f;

            }
        }
    }

    //参照を更新
    private void SetRocketControllerRef()
    {
        rocketController = rocketManager.Current;
        rocketGravBody = rocketController?.GetComponent<GravBody>();
    }

    //発射角を指定の値ずつ変更するメソッド
    public void ChangeLaunchAngle(float delta)
    {
        launchAngle += delta;
        //発射角の範囲を制限する
        launchAngle = Mathf.Clamp(launchAngle, -launchAngleRange, launchAngleRange);
    }

    //ロケットを生成して発射するメソッド
    private void GenerateRocket()
    {
        rocketController = rocketManager.CreateRocket(transform.position, launchAngle, launchSpeed);
        rocketGravBody = rocketController?.GetComponent<GravBody>();
    }

    //能動的にロケットを発射するためのメソッド
    private void OnFire(InputValue value)
    {
        if (value.isPressed)
        {
            if (rocketController != null)
            {
                rocketController.DestroyRocket(); // 既存のロケットを破棄
            }
            GenerateRocket();
        }
    }

    //ロケットに初速インパルスを与えるためのメソッド
    private void Launch()
    {
        Vector2 velocity = rocketController.CalculateVelocity();
        rocketGravBody.AddInpulse(velocity);
    }

}
