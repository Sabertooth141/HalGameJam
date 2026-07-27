using UnityEngine;

public class GoalController : MonoBehaviour
{
    //シングルトン
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

    [Header("発射されていて、進行中かどうか")]
    [SerializeField] private bool isMoving = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
