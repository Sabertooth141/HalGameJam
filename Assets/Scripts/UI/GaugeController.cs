using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    // ゲージの上下限界位置を保持する変数
    private float gaugeMaxY;
    private float gaugeMinY; 

    //----------------------------
    // 関数
    //----------------------------

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // アローの初期位置を保存
        arrowInitialPosition = arrowImg.rectTransform.localPosition;
        // ゲージの上下限界位置を計算
        gaugeMaxY = gaugeImg.rectTransform.localPosition.y + gaugeImg.rectTransform.rect.height / 2;
        gaugeMinY = gaugeImg.rectTransform.localPosition.y - gaugeImg.rectTransform.rect.height / 2;
    }

    // Update is called once per frame
    void Update()
    {
        // StartControllerから速度を取得してアローの位置を更新
        StartController startCon = StartController.Instance;
        UpdateArrowPosition(startCon.LaunchSpeed, startCon.MaxLaunchSpeed, startCon.MinLaunchSpeed);
    }

    //アローの位置を更新する関数
    private void UpdateArrowPosition(float speed, float maxSpeed, float minSpeed)
    {
        // アローの最低位置を計算
        float arrowMinY = gaugeMinY + gaugeOffset;
        // ゲージのうち、アローの動く範囲を計算
        float movableRange = gaugeMaxY - gaugeOffset - arrowMinY;
        // アローの位置を計算
        float arrowPosY = arrowMinY + Mathf.Clamp(((speed - minSpeed) / (maxSpeed - minSpeed)) * movableRange, 0f, movableRange);

        // アローの位置を更新
        Vector3 arrowPos = arrowImg.rectTransform.localPosition;
        arrowPos.y = arrowPosY;
        arrowImg.rectTransform.localPosition = arrowPos;
    }
}
