using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms.Impl;

public class StartController : MonoBehaviour
{

    //ロケットの発射方法のモードを管理する変数
    private enum LaunchMode
    {
        Manual, //手動発射モード
        Automatic, //時間経過で自動発射モード
        Debug
    }

    //----------------------------
    //シングルトン
    //----------------------------
    public static StartController Instance { get; private set; }

    //----------------------------
    // 参照
    //----------------------------
    private RocketManager rocketManager;
    private RocketController rocketController;
    private GravBody rocketGravBody;
    private Camera cam;

    //----------------------------
    // パラメータ
    //----------------------------
    [Header("ロケットの発射角度")]
    [Tooltip("ロケットの発射角度")]
    [Range(-180f, 180f)] [SerializeField] private float launchAngle = 0f;

    [Tooltip("発射角の中心")]
    [SerializeField] private float baseLaunchAngle = 0f;

    private Vector2 launchVector; // ロケット発射のベクトル

    [Tooltip("発射角０を基準とした、ロケットの発射角の範囲")]
    [SerializeField] private float launchAngleRange = 45f;

    [Tooltip("ロケットの発射初速度")]
    [SerializeField] private float launchSpeed = 45f;

    [Tooltip("ロケットの発射最大初速度")]
    [SerializeField] private float maxLaunchSpeed = 80f;

    [Tooltip("ロケットの発射最小初速度")]
    [SerializeField] private float minLaunchSpeed = 5f;

    [Tooltip("マニュアルモードでの発射初速度の変換速度")]
    [SerializeField] private float launchSpeedChangeMagnitude = 3f;

    [Tooltip("軌道予測の参照")]
    [SerializeField] private TrajectoryPredictor trajPredict;

    private bool isLaunched = false;

    private int launchSpeedChangeSign = 1;

    [Header("ロケットの発射方法のモード")]
    [Tooltip("ロケットの発射方法のモード\n"　+ "Manual: ポインターで発射角度を操作\n" + "Auto: デバッグ用、ゲーム内発射角度操作不能")]
    [SerializeField] private LaunchMode launchMode = LaunchMode.Manual; // 手動発射モードかどうか

    //ロケットの発射モードが自動発射モードの場合のみ
    [Tooltip("自動発射モードの発射間隔")]
    [SerializeField] private float autoLaunchInterval = 5f; // 自動発射モードの発射間隔

    private float timer = 0f; // タイマー

    //----------------------------
    //関数
    //----------------------------

    private void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        //シングルトンの初期化
        rocketManager = RocketManager.Instance;
        rocketController = rocketManager.Current;
        rocketGravBody = rocketController?.GetComponent<GravBody>();

        cam = Camera.main;

        if (trajPredict == null)
        {
            trajPredict = GetComponentInChildren<TrajectoryPredictor>();
        }

        if (launchMode == LaunchMode.Manual)
        {
            launchSpeed = minLaunchSpeed;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        timer += Time.deltaTime;

        UpdateLauncherRotation();

        if (launchMode == LaunchMode.Automatic)
        {
            //自動発射モードの場合、一定時間ごとにロケットを発射する
            if (timer >= autoLaunchInterval)
            {
                if (rocketController != null) rocketController.DestroyRocket(); // 既存のロケットを破棄

                Launch();
                timer = 0f;
            }
        }

        HandleInput();
    }

    private void FixedUpdate()
    {
        if (trajPredict != null)
        {
            var launchRad = launchAngle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(launchRad), Mathf.Sin(launchRad));
            trajPredict.SetTargetParam(new Vector2(transform.position.x, transform.position.y), dir * launchSpeed,
                new Vector2(0, 0));
        }
    }

    private void HandleInput()
    {
        if (launchMode is LaunchMode.Manual or LaunchMode.Debug)
        {
            AimAtCursor();
            HandleFiring();
        }
    }

    private void HandleFiring()
    {
        Keyboard kb = Keyboard.current;

        if (kb == null)
        {
            return;
        }

        if (kb.spaceKey.isPressed)
        {
            if (launchMode == LaunchMode.Manual && isLaunched)
            {
                return;
            }

            launchSpeed += launchSpeedChangeSign * launchSpeedChangeMagnitude * Time.deltaTime;
            if (launchSpeed >= maxLaunchSpeed)
            {
                launchSpeedChangeSign = -1;
            }
            else if (launchSpeed <= minLaunchSpeed)
            {
                launchSpeedChangeSign = 1;
            }
        }

        if (kb.spaceKey.wasReleasedThisFrame)
        {
            if (launchMode == LaunchMode.Debug)
            {
                isLaunched = false;
            }

            if (isLaunched)
            {
                return;
            }

            isLaunched = true;

            Launch();
            launchSpeed = minLaunchSpeed;
        }
    }

    private void AimAtCursor()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || cam == null)
        {
            return;
        }

        Vector2 screenPos = mouse.position.ReadValue();

        float depth = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));

        Vector2 dir = (Vector2)worldPos - (Vector2)transform.position;

        if (dir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        SetLaunchAngle(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    //参照を更新
    private void SetRocketControllerRef()
    {
        rocketController = rocketManager.Current;
        rocketGravBody = rocketController?.GetComponent<GravBody>();
    }

    //発射角を絶対値で設定するメソッド
    public void SetLaunchAngle(float angle)
    {
        float deltaAngle = Mathf.DeltaAngle(baseLaunchAngle, angle);

        //発射角の範囲を制限する
        launchAngle = baseLaunchAngle + Mathf.Clamp(deltaAngle, -launchAngleRange, launchAngleRange);
    }

    //発射角を指定の値ずつ変更するメソッド
    public void ChangeLaunchAngle(float delta)
    {
        SetLaunchAngle(launchAngle + delta);
    }

    //ロケットを生成して発射するメソッド
    private void GenerateRocket()
    {
        rocketController = rocketManager.CreateRocket(transform.position, launchAngle, launchSpeed);
        rocketController.gameObject.SetActive(true);
        rocketGravBody = rocketController?.GetComponent<GravBody>();
    }

    //ロケットに初速インパルスを与えるためのメソッド
    private void Launch()
    {
        if (rocketController != null) rocketController.DestroyRocket();
        GenerateRocket();
        var velocity = rocketController.CalculateVelocity();
        SoundManager.Instance.Play("Launch");
        rocketGravBody.AddImpulse(velocity);
    }

    //発射台の回転を更新するメソッド
    private void UpdateLauncherRotation()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, launchAngle);
    }

    //ロケットの発射速度を取得するメソッド
    public float LaunchSpeed { get { return launchSpeed; } }
    //ロケットの発射最大速度を取得するメソッド
    public float MaxLaunchSpeed { get { return maxLaunchSpeed; } }
    //ロケットの発射最小速度を取得するメソッド
    public float MinLaunchSpeed { get { return minLaunchSpeed; } }

    public bool IsLaunched { get { return isLaunched; } }
}