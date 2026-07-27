using UnityEngine;

public class GoalController : MonoBehaviour
{
    //----------------------------
    //シングルトン
    //----------------------------
    public static GoalController Instance { get; private set; }
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
    //現在のロケットコントローラーへの参照
    private RocketController rocketController;


    //----------------------------
    // 関数
    //----------------------------

    void Start()
    {
        rocketController = RocketManager.Instance.Current;
    }

    // Update is called once per frame
    void Update()
    {
        rocketController = RocketManager.Instance.Current;
    }
}
