using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using static SceneTransitioner;

[RequireComponent(typeof(VideoPlayer))]
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Tooltip("動画タイムアウト時間（秒）")]
    [SerializeField] private float maxWaitTime = 3f;

    [SerializeField] private GameObject videoOverlay;

    private Keyboard kb;
    private VideoPlayer vPlayer;
    private bool isTransitioning = false;
    private bool hasLoaded;

    private Coroutine failsafeRoutine;

    private void Awake()
    {
        vPlayer = GetComponent<VideoPlayer>();   //先に取っておく

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        vPlayer.isLooping = false;
        vPlayer.playOnAwake = false;
        vPlayer.loopPointReached += OnTransitionComplete;
        vPlayer.errorReceived += OnError;
        vPlayer.prepareCompleted += OnPrepared;

        ClearTargetTexture();
    }

    private void Start()
    {
        if (kb == null)
        {
            kb = Keyboard.current;
        }

        
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnPrepared(VideoPlayer p) => StartPlayback();

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (videoOverlay != null)
        {
            videoOverlay.SetActive(false);
        }

        isTransitioning = false;
        hasLoaded = false;
    }

    private void ClearTargetTexture()
    {
        var rt = vPlayer.targetTexture;
        if (rt == null) return;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = prev;
    }

    public void RestartScene()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        if (vPlayer.isPrepared)
        {
            StartPlayback();
        }
        else
        {
            vPlayer.Prepare();
        }

        SoundManager.Instance.Play("SignalLoss");
    }

    private void StartPlayback()
    {
        if (videoOverlay != null) videoOverlay.SetActive(true);
        vPlayer.frame = 0;
        vPlayer.Play();

        if (failsafeRoutine != null) StopCoroutine(failsafeRoutine);
        failsafeRoutine = StartCoroutine(Failsafe());
    }

    private void Update()
    {
        if (kb == null) kb = Keyboard.current;
        if (kb == null) return;

        HandleRetry();
        HandleReturn();
    }

    private void HandleReturn()
    {
        if (kb.escapeKey.wasPressedThisFrame)
        {
            SoundManager.Instance.Play("Confirm");

            if (SceneTransitioner.Instance.GetCurrentScene(SceneTransitioner.SceneName.StageSelect))
            {
                SceneTransitioner.Instance.LoadSceneSlide(SceneTransitioner.SceneName.Title);
                return;
            }

            SceneTransitioner.Instance.LoadSceneSlide(SceneTransitioner.SceneName.StageSelect);
        }
    }

    private void HandleRetry()
    {
        if (!kb.rKey.wasPressedThisFrame) return;

        if (isTransitioning)
        {
            Go();            //再生中ならスキップ
        }
        else
        {
            RestartScene();  //そうでなければ開始
        }
    }

    //リセットで使う
    public void ResetScene()
    {
        if (isTransitioning)
        {
            Go();            //再生中ならスキップ
        }
        else
        {
            RestartScene();  //そうでなければ開始
        }
    }

    private void OnTransitionComplete(VideoPlayer player)
    {
        if (!isTransitioning)
        {
            return;
        }
        Go();
    }

    private void Go()
    {
        if (hasLoaded) return;
        hasLoaded = true;

        if (failsafeRoutine != null)
        {
            StopCoroutine(failsafeRoutine);
            failsafeRoutine = null;
        }

        vPlayer.Stop();
        Time.timeScale = 1f;

        if (SceneTransitioner.Instance != null &&
            SceneTransitioner.Instance.GetCurrentScene(SceneTransitioner.SceneName.StageSelect))
        {
            isTransitioning = false;
            hasLoaded = false;
            if (videoOverlay != null) videoOverlay.SetActive(false);
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnError(VideoPlayer p, string msg)
    {
        Debug.LogError(msg);
    }

    IEnumerator Failsafe()
    {
        //動画の長さ + 余裕。lengthはPrepare後でないと0なので保険を入れる
        float wait = vPlayer.length > 0.01f
            ? (float)vPlayer.length + maxWaitTime
            : maxWaitTime;

        yield return new WaitForSecondsRealtime(wait);
        Debug.LogWarning("Failsafeが発動しました");
        failsafeRoutine = null;
        Go();
    }

    private void OnDestroy()
    {
        if (vPlayer == null) return;   //購読していないインスタンスは何もしない

        vPlayer.loopPointReached -= OnTransitionComplete;
        vPlayer.errorReceived -= OnError;
        vPlayer.prepareCompleted -= OnPrepared;

        if (Instance == this) Instance = null;
    }
}