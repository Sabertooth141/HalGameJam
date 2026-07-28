using UnityEngine;

public class StageSelect : MonoBehaviour
{
    //----------------------------
    // 列挙型
    //----------------------------
    //シーン遷移のモード
    public enum TransitionMode
    {
        NoEffect = 0, //特殊効果なしの即時切り替わり
        FadeOut = 1, //フェードアウト
        SlideIn = 2, //スライドイン
    }

    //----------------------------
    // パラメータ
    //----------------------------
    [Header("ステージ選択星の半径")]
    [Tooltip("ステージ選択星の半径を設定する")]
    [Min(0f)]
    [SerializeField] private float radius = 1f; // 半径

    [Header("シーン遷移のモード")]
    [Tooltip("モード選択（特殊効果無・フェード・スライド）")]
    [SerializeField] private TransitionMode transitionMode = TransitionMode.SlideIn;

    [Header("次のシーン")]
    [Tooltip("ロケットが当たったときに遷移するシーンを設定")]
    [SerializeField] private SceneTransitioner.SceneName nextScene;

    //----------------------------
    // 参照
    //----------------------------
    //現在のロケットコントローラーへの参照
    private RocketController rocketController;

    //----------------------------
    // 変数
    //----------------------------

    //----------------------------
    // 関数
    //----------------------------
    void Start()
    {
        //ロケットが生成されたときに、現在のロケットコントローラーを取得するようにイベントに登録
        RocketManager.Instance.OnCreateRocket += () => this.rocketController = RocketManager.Instance.Current;
        rocketController = RocketManager.Instance.Current;
    }

    void FixedUpdate()
    {
        //ロケットの位置を取得して接触判定を行う
        if (rocketController == null) return;
        Vector3 pos = rocketController.transform.position;
        Vector2 rocketPos = new Vector2(pos.x, pos.y);
        CheckSelected(rocketPos);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    //ロケットの位置とステージ選択星の位置を比較して近かったらゴールにする
    private void CheckSelected(Vector2 rocketPos)
    {
        Vector2 goalPos = new Vector2(transform.position.x, transform.position.y);
        float distance = Vector2.Distance(rocketPos, goalPos);
        if (distance < radius) // 半径を使用
        {
            Debug.Log("Goal!");
            switch (transitionMode)
            {
                case TransitionMode.NoEffect:
                    SceneTransitioner.Instance.LoadSceneInstant(nextScene);
                    break;
                case TransitionMode.FadeOut:
                    SceneTransitioner.Instance.LoadSceneFade(nextScene);
                    break;
                case TransitionMode.SlideIn:
                    SceneTransitioner.Instance.LoadSceneSlide(nextScene);
                    break;
                default:
                    break;
            }
        }
    }
}
