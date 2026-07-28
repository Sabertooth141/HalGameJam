using UnityEngine;
using UnityEngine.UI;

public class GaugeController : MonoBehaviour
{
    //----------------------------
    // パラメータ
    //----------------------------
    [Header("ゲージのスプライト")]
    [Tooltip("ロケットの速度ゲージの画像")]
    [SerializeField] private Image gaugeImg;

    [Header("アローのスプライト")]
    [Tooltip("ロケットの速度ゲージを指すアローの画像")]
    [SerializeField] private Image arrowImg;

    [Header("速度ゲージの上下の限界値のオフセット")]
    [Tooltip("速度ゲージの上下の限界値のオフセット（速度ゲージの縦幅を基準にアローがどこまで動くか）")]
    [SerializeField] private float gaugeOffset = 0.1f;

    //----------------------------
    // 変数
    //----------------------------
    // アローの初期位置を保持する変数
    private Vector3 arrowInitialPosition;
    // ゲージの高さを保持する変数
    private float gaugeHeight;

    //----------------------------
    // 関数
    //----------------------------

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // アローの初期位置を保存
        arrowInitialPosition = arrowImg.rectTransform.localPosition;
        // ゲージの高さを保存
        gaugeHeight = gaugeImg.rectTransform.rect.height;
    }

    // Update is called once per frame
    void Update()
    {
        // StartControllerから速度を取得してアローの位置を更新
        StartController startCon = StartController.Instance;
        UpdateArrowPosition(startCon.LaunchSpeed, startCon.MaxLaunchSpeed);
    }

    //アローの位置を更新する関数
    private void UpdateArrowPosition(float speed, float maxSpeed)
    {
        // アローの位置を計算
        float arrowMoveY = Mathf.Clamp((speed / maxSpeed) * gaugeHeight, 0f + gaugeOffset, gaugeHeight - gaugeOffset);

        // アローの位置を更新
        Vector3 arrowPos = arrowInitialPosition;
        arrowPos.y += arrowMoveY;
        arrowImg.rectTransform.localPosition = arrowPos;
    }
}
