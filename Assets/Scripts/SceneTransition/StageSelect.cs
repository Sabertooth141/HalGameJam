using UnityEngine;

public class StageSelect : MonoBehaviour
{
    //----------------------------
    // パラメータ
    //----------------------------
    [Header("ステージ選択星の半径")]
    [Tooltip("ステージ選択星の半径を設定する")]
    [Min(0f)]
    [SerializeField] private float radius = 1f; // 半径

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
        //ロケットの位置を取得してゴール判定を行う
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
        if (distance < radius) // ゴールの半径を使用
        {
            Debug.Log("Goal!");
            SceneTransitioner.Instance.LoadSceneInstant(SceneTransitioner.SceneName.SampleScene1);
        }
    }
}
