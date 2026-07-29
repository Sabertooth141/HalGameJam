using UnityEngine;
using UnityEngine.SceneManagement;

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
    [Min(0f)]
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
        Instance = this;
    }

    void Start()
    {
        //ロケットが生成されたときに、現在のロケットコントローラーを取得するようにイベントに登録
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, goalRadius);
    }

    //ロケットの位置とゴールの位置を比較して近かったらゴールにする
    private void CheckGoal(Vector2 rocketPos)
    {
        Vector2 goalPos = new Vector2(transform.position.x, transform.position.y);
        float distance = Vector2.Distance(rocketPos, goalPos);
        if (distance < goalRadius) // ゴールの半径を使用
        {
            isGoal = true;
            rocketController.GetComponent<GravBody>().velocity = Vector2.zero; // ロケットの速度をゼロにする
            
            SoundManager.Instance.Play("Clear");

            if (IsLastScene())
            {
                SceneTransitioner.Instance.LoadSceneSlide(SceneTransitioner.SceneName.Result);
            }
            else
            {
                SceneTransitioner.Instance.LoadSceneSlide((SceneTransitioner.SceneName)SceneManager.GetActiveScene().buildIndex + 1);
            }
            
        }
    }

    //今のシーンが最後かどうか
    private bool IsLastScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        return current == SceneManager.sceneCountInBuildSettings - 1;
    }
}
