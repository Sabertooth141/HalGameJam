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
    //※必ずシーンのビルドインデックスと同じ順番にすること！
    public enum SceneName
    {
        Title = 0,
        StageSelect,
        Result,
        SampleScene1, //テスト用のサンプルシーン
        SampleScene2, //テスト用のサンプルシーン2

        Stage1, 
        Stage2, 
        Stage3, 
        Stage4, 
        Stage5, 
        Stage6, 
        Stage7, 
        Stage8, 
        Stage9
    }

    //----------------------------
    // パラメータ
    //----------------------------
    [Header("フェードアウト")]
    [Tooltip("フェードアウトする画像")]
    [SerializeField] private Image fadeImg;
    [SerializeField] private Canvas canvas;

    [Tooltip("フェード時間")]
    [SerializeField] private float duration = 1.0f;

    [Header("スライドイン")]
    [Tooltip("スライドインする画像")]
    [SerializeField] private Image slideImg;
    [Tooltip("スライド時間")]
    [SerializeField] private float slideDuration = 1.0f;
    [Tooltip("スライドする画像の止まる位置のオフセット")]
    [SerializeField] private float slideOffset = 1.0f;

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
        canvas.sortingOrder = 100;
        
        if (Instance != null && Instance != this) return;

        Instance = this;

        //初期値はα値を0にして透明にしておく
        Color color = fadeImg.color;
        color.a = 0f;
        fadeImg.color = color;
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
                break;
            case SceneName.StageSelect:
                //各ステージに遷移
                break;
            case SceneName.Result:
                //タイトル画面に遷移
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

    // フェード処理
    //画像をフェードしながら次のシーンに切り替える関数
    private IEnumerator LoadFade(SceneName name)
    {
        Color color = fadeImg.color;
        fadeImg.color = color;
        isSceneTransitioning = true; //シーン遷移中フラグを立てる

        //--------------
        //フェードアウトする
        float time = 0.0f; //初期化
        while (time < duration)
        {
            time += Time.deltaTime; //時間を加算
            //アルファ値の変化用の変数
            float alpha = Mathf.Clamp01(time / duration); //Clamp01は0~1の値に制限して返す

            color.a = alpha; //alpha値を変更
            fadeImg.color = color;

            //1フレーム停止する
            //→停止したのち、またここから開始する
            yield return null;
        }

        //--------------
        //ここでシーン遷移直前に行いたい処理を呼び出す
        //

        //--------------
        //裏でシーン遷移を済ませる
        //非同期でシーンをロード
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync((int)name);

        //ロードが完了しても勝手にシーンを切り替えないように設定
        asyncLoad.allowSceneActivation = false;

        // asyncLoad.progress が 0.9 になると「ロード完了（切り替え準備OK）」を意味する
        while (asyncLoad.progress < 0.9f)
        {
            yield return null; // ロードが終わるまで1フレームずつ待つ
        }

        // ロード完了したのでシーン切り替えを許可する
        asyncLoad.allowSceneActivation = true;

        // シーンが実際に切り替わるまで待機する
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        currentScene = name; //現在のシーン名を更新

        //--------------
        //ここでシーン遷移直後に行いたい処理を呼び出す
        //

        //--------------
        //フェードインする
        time = 0.0f; //初期化
        while (time < duration)
        {
            time += Time.deltaTime; //時間を加算
            //アルファ値の変化用の変数
            float alpha = Mathf.Clamp01(time / duration); //Clamp01は0~1の値に制限して返す

            color.a = 1 - alpha; //alpha値を変更
            fadeImg.color = color;

            //1フレーム停止する
            //→停止したのち、またここから開始する
            yield return null;
        }

        color.a = 0.0f;
        isSceneTransitioning = false; //シーン遷移中フラグを下ろす
    }

    // スライド処理
    //画像をスライドしながら次のシーンに切り替える関数
    IEnumerator LoadSlide(SceneName name)
    {
        isSceneTransitioning = true; //シーン遷移中フラグを立てる

        //--------------
        //画像をスライドインする
        float time = 0.0f; //初期化
        Vector3 tempPos = slideImg.rectTransform.anchoredPosition;
        Vector3 startPos = new Vector3(Screen.width + slideImg.rectTransform.rect.width, tempPos.y, tempPos.z); //右からスタート
        Vector3 endPos = new Vector3(slideOffset, tempPos.y, tempPos.z); //中央で一旦止まる(オフセット付き)
        while (time < slideDuration)
        {
            time += Time.deltaTime; //時間を加算
            float t = Mathf.Clamp01(time / slideDuration); //0~1の値に制限して返す
            slideImg.rectTransform.localPosition = Vector3.Lerp(tempPos, endPos, t); //線形補間で位置を更新
            yield return null; //1フレーム停止する
        }

        //--------------
        //ここでシーン遷移直前に行いたい処理を呼び出す
        //

        //--------------
        //裏でシーン遷移を済ませる
        //非同期でシーンをロード
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync((int)name);

        //ロードが完了しても勝手にシーンを切り替えないように設定
        asyncLoad.allowSceneActivation = false;

        // asyncLoad.progress が 0.9 になると「ロード完了（切り替え準備OK）」を意味する
        while (asyncLoad.progress < 0.9f)
        {
            yield return null; // ロードが終わるまで1フレームずつ待つ
        }

        // ロード完了したのでシーン切り替えを許可する
        asyncLoad.allowSceneActivation = true;

        // シーンが実際に切り替わるまで待機する
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        currentScene = name; //現在のシーン名を更新

        //--------------
        //ここでシーン遷移直後に行いたい処理を呼び出す
        //

        //--------------
        //画像をスライドアウトする
        time = 0.0f; //初期化
        startPos = endPos; //今の位置からスタート
        endPos = new Vector3(-Screen.width - slideImg.rectTransform.rect.width, tempPos.y, tempPos.z); //左へスライドアウト
        while (time < slideDuration)
        {
            time += Time.deltaTime; //時間を加算
            float t = Mathf.Clamp01(time / slideDuration); //0~1の値に制限して返す
            slideImg.rectTransform.localPosition = Vector3.Lerp(startPos, endPos, t); //線形補間で位置を更新
            yield return null; //1フレーム停止する
        }

        currentScene = name;
        Debug.Log($"loaded, フェードイン開始 alpha={fadeImg.color.a} order={canvas.sortingOrder}");
        isSceneTransitioning = false; //シーン遷移中フラグを下ろす
    }

    //フェードしながら次のシーンに切り替える関数を実行する関数
    public void LoadSceneFade(SceneName name)
    {
        if (isSceneTransitioning)
        {
            Debug.LogWarning("シーン遷移中のため、フェード遷移は無効です。");
            return;
        }
        StartCoroutine(LoadFade(name));
        Debug.Log(name + "画面にフェード遷移");
    }

    //スライドしながら次のシーンに切り替える関数を実行する関数
    public void LoadSceneSlide(SceneName name)
    {
        if (isSceneTransitioning)
        {
            Debug.LogWarning("シーン遷移中のため、スライド遷移は無効です。");
            return;
        }
        StartCoroutine(LoadSlide(name));
        Debug.Log(name + "画面にスライド遷移");
    }

    //シーンを特殊効果なしに切り替える関数(LoadSceneの引数をenumに変えるだけの関数って感じ)
    public void LoadSceneInstant(SceneName name)
    {
        if (isSceneTransitioning)
        {
            Debug.LogWarning("シーン遷移中のため、遷移は無効です。");
            return;
        }
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
            case SceneName.StageSelect:
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
