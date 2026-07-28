using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

//--------------------------------------------
//ゲームのシーンを遷移させる。
//関数を他のスクリプトから呼び出して使う。
//ゲームルール管理用オブジェクトにアタッチして使う。
//--------------------------------------------

public class SceneTransitioner : MonoBehaviour
{
    public static SceneTransitioner Instance { get; private set; } //シーンをまたぐ窓口

    //----------------------------
    // 列挙型
    //----------------------------
    //シーン名列挙型
    //Unityのビルド設定でのシーンの順番と合わせること
    //public enum SceneName
    //{
    //    Title = 0,
    //    Playing = 1,
    //    Result = 2,

    //    SampleScene1 = 3, //テスト用のサンプルシーン
    //    SampleScene2 = 4, //テスト用のサンプルシーン2
    //}
    //テストのためにタイトルとかのシーン番号を変えている。あとで戻す。
    public enum SceneName
    {
        Title = 10,
        Playing = 11,
        Result = 12,

        SampleScene1 = 0, //テスト用のサンプルシーン
        SampleScene2 = 1, //テスト用のサンプルシーン2
    }

    //----------------------------
    // パラメータ
    //----------------------------
    [Header("フェードアウトする画像")]
    [SerializeField] private Image FadeImg;
    [Header("フェード時間")]
    [SerializeField] private float Duration = 1.0f;

    [Header("シーン管理")]
    [Tooltip("現在のシーン")]
    [SerializeField] private SceneName currentScene = SceneName.Title; //現在のシーン名

    //----------------------------
    // 変数
    //----------------------------
    //シーン遷移中かどうかのフラグ
    private bool isSceneTransitioning = false;

    //----------------------------
    // 関数
    //----------------------------
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // シーンをまたいでも破棄されないようにしたい場合
        // DontDestroyOnLoad(gameObject);

        //初期値はα値を0にして透明にしておく
        Color color = FadeImg.color;
        color.a = 0f;
        FadeImg.color = color;
    }

    private void Start()
    {
        currentScene = (SceneName)SceneManager.GetActiveScene().buildIndex;
    }

    private void Update()
    {
        //インスタンスがないなら何もしない
        if (Instance == null)
        {
            return;
        }

        //シーン遷移中のシーン遷移は無効、何もしない
        if (isSceneTransitioning)
        {
            return;
        }

        //--------------------------------
        //シーン遷移条件管理
        switch (currentScene)
        {
            case SceneName.Title:
                //プレイ画面に遷移
                //（タイトル画面であり、ESCキー以外の何かキーが押された場合に遷移）
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    break; //ESCキーなら遷移なし
                }
                else if (Keyboard.current.anyKey.wasPressedThisFrame)
                {
                    Debug.Log("タイトル画面で何かキーが押された");
                    LoadSceneFade(SceneName.Playing); //プレイ画面に遷移
                }
                break;
            case SceneName.Playing:
                //リザルト画面に遷移
                //if (GameManager.Instance.gameStageSteps == GameManager.GameStageSteps.gameEnd && (GameManager.Instance.isGameCleard || GameManager.Instance.isGameOver))
                //{
                //    LoadSceneFade(SceneName.Result); //リザルト画面に遷移
                //}

                //イベントで実行するように変更
                break;
            case SceneName.Result:
                //タイトル画面に遷移
                if (Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    LoadSceneFade(SceneName.Title); //タイトル画面に遷移
                }
                break;
            default:
                break;
        }
    }

    //現在のシーン名を取得する関数
    //引数なし          : 現在のシーンをSceneName型で返す
    //引数にSceneName型 : 指定のシーンに遷移しているかどうかをboolで返す
    public SceneName GetCurrentScene()
    {
        return currentScene;
    }
    public bool GetCurrentScene(SceneName sceneName)
    {
        return currentScene == sceneName;
    }

    //フェードしながら次のシーンに切り替える関数
    private IEnumerator LoadFade(SceneName name)
    {
        Color color = FadeImg.color;
        FadeImg.color = color;
        isSceneTransitioning = true; //シーン遷移中フラグを立てる

        //フェードアウトする
        float time = 0.0f; //初期化
        while (time < Duration)
        {
            time += Time.deltaTime; //時間を加算
            //アルファ値の変化用の変数
            float alpha = Mathf.Clamp01(time / Duration); //Clamp01は0~1の値に制限して返す

            color.a = alpha; //alpha値を変更
            FadeImg.color = color;

            //1フレーム停止する
            //→停止したのち、またここから開始する
            yield return null;
        }

        //ここでシーン遷移直前に行いたい処理を呼び出す
        //

        //裏でシーン遷移を済ませる
        color.a = 1.0f;
        FadeImg.color = color;
        SceneManager.LoadScene((int)name);
        currentScene = name; //現在のシーン名を更新

        //ここでシーン遷移直後に行いたい処理を呼び出す
        //

        //フェードインする
        time = 0.0f; //初期化
        while (time < Duration)
        {
            time += Time.deltaTime; //時間を加算
            //アルファ値の変化用の変数
            float alpha = Mathf.Clamp01(time / Duration); //Clamp01は0~1の値に制限して返す

            color.a = 1 - alpha; //alpha値を変更
            FadeImg.color = color;

            //1フレーム停止する
            //→停止したのち、またここから開始する
            yield return null;
        }

        color.a = 0.0f;
        isSceneTransitioning = false; //シーン遷移中フラグを下ろす
    }

    //フェードしながら次のシーンに切り替える関数を実行する関数
    public void LoadSceneFade(SceneName name)
    {
        StartCoroutine(LoadFade(name));
        Debug.Log(name + "画面にフェード遷移");
    }

    //シーンを切り替える関数(LoadSceneの引数をenumに変えるイメージ)
    public void LoadSceneInstant(SceneName name)
    {
        SceneManager.LoadScene((int)name);
        currentScene = name; //現在のシーン名を更新
        Debug.Log(name + "画面に遷移");
    }

    //リザルト画面に遷移する専用の関数
    public void LoadResult()
    {
        LoadSceneFade(SceneName.Result);
    }

    //タイトル画面に遷移する専用の関数
    public void LoadTitle()
    {
        LoadSceneFade(SceneName.Title);
    }

    //--------------------------------
    //デバッグ用関数群
    //--------------------------------
    public void DebugShowCurrentScene()
    {
        switch (currentScene)
        {
            case SceneName.Title:
                Debug.Log("現在のシーンはタイトル");
                break;
            case SceneName.Playing:
                Debug.Log("現在のシーンはプレイ中");
                break;
            case SceneName.Result:
                Debug.Log("現在のシーンはリザルト");
                break;
            case SceneName.SampleScene1:
                Debug.Log("現在のシーンはサンプルシーン１");
                break;
            case SceneName.SampleScene2:
                Debug.Log("現在のシーンはサンプルシーン２");
                break;
        }
    }

}
