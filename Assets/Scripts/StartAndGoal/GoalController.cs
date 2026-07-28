using UnityEngine;

public class GoalController : MonoBehaviour
{
    //----------------------------
    //シングルトン
    //----------------------------
    public static GoalController Instance { get; private set; }

    //----------------------------
    // パラメータ
    //----------------------------
    [Header("ゴールの半径")]
    [Tooltip("ゴールの半径を設定する")]
    [SerializeField] private float goalRadius = 1f; // ゴールの半径

    //----------------------------
    // 参照
    //----------------------------
    //現在のロケットコントローラーへの参照
    private RocketController rocketController;


    //----------------------------
    // 変数
    //----------------------------
    //ゴールしているかどうか
    private bool isGoal = false;

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

    void Start()
    {
        RocketManager.Instance.OnCreateRocket += () => this.rocketController = RocketManager.Instance.Current;
        rocketController = RocketManager.Instance.Current;
    }

    void FixedUpdate()
    {
        //ロケットの位置を取得してゴール判定を行う
        if (rocketController == null) return;
        Vector3 pos = rocketController.transform.position;
        Vector2 rocketPos = new Vector2(pos.x, pos.y);
        CheckGoal(rocketPos);
    }

    //ロケットの位置とゴールの位置を比較して近かったらゴールにする
    private void CheckGoal(Vector2 rocketPos)
    {
        Vector2 goalPos = new Vector2(transform.position.x, transform.position.y);
        float distance = Vector2.Distance(rocketPos, goalPos);
        if (distance < goalRadius) // ゴールの半径を使用
        {
            isGoal = true;
            Debug.Log("Goal!");
            SceneTransitioner.Instance.LoadSceneInstant(SceneTransitioner.SceneName.SampleScene1);
        }
    }
}
