using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        vPlayer = GetComponent<VideoPlayer>();
        vPlayer.isLooping = false; // これがtrueだとloopPointReachedが呼ばれない
        vPlayer.playOnAwake = false;
        vPlayer.loopPointReached += OnTransitionComplete;
        vPlayer.errorReceived += OnError;
        vPlayer.prepareCompleted += OnPrepared;

        ClearTargetTexture();
        if (videoOverlay != null)
        {
            videoOverlay.SetActive(false);
        }
    }

    private void Start()
    {
        if (kb == null)
        {
            kb = Keyboard.current;
        }
    }

    private void OnPrepared(VideoPlayer p) => StartPlayback();

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
        if (videoOverlay != null)
        {
            videoOverlay.SetActive(true);
        }
        vPlayer.frame = 0;
        vPlayer.Play();
        StartCoroutine(Failsafe());
    }

    private void Update()
    {
        if (kb.rKey.wasPressedThisFrame)
        {
            RestartScene();
        }
        if (isTransitioning && kb.rKey.wasPressedThisFrame)
        {
            Go();
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
        Time.timeScale = 1f;
        if (SceneTransitioner.Instance.GetCurrentScene(SceneTransitioner.SceneName.StageSelect))
        {
            //シーンがStageSelectの場合は、シーンをリロードせずに遷移する
            isTransitioning = false;
            hasLoaded = false;
            videoOverlay.SetActive(false);
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
        yield return new WaitForSecondsRealtime(maxWaitTime);
        Go();
    }

    private void OnDestroy()
    {
        vPlayer.loopPointReached -= OnTransitionComplete;
        vPlayer.errorReceived -= OnError;
        vPlayer.prepareCompleted -= OnPrepared;
    }
}