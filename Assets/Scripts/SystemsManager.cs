using UnityEngine;

public class SystemsManager : MonoBehaviour
{
    private static SystemsManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   //シーンに置かれた重複分を丸ごと破棄
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
