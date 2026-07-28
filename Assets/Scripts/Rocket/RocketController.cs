using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(GravBody))]
public class RocketController : MonoBehaviour
{
    private static readonly int Explode = Animator.StringToHash("Explode");
    //----------------------------
    // パラメータ
    //----------------------------

    [Header("ロケットのパラメータ")]
    [Tooltip("ロケットの現在角度")]
    [SerializeField] private float angle; // 角度
    [Tooltip("ロケットの現在速度")]
    [SerializeField] private float speed; // 速度
    [Tooltip("ロケットの最大速度")]
    [SerializeField] private float maxSpeed;

    [SerializeField] private Animator anim;
    [SerializeField] private float explodeLen = 0.6f;
    [SerializeField] private GameObject rocketSprite;

    private GravBody gravBody;

    // 破棄通知イベント
    public event Action OnDestroyed;
    
    //----------------------------
    // 変数
    //----------------------------
    private bool isDestroyed;

    //----------------------------
    // 関数
    //----------------------------
    private void Awake()
    {
        
    }

    private void Update()
    {
        SceneController.Instance.SetRocketPos(this.transform.position);

        gravBody = GetComponent<GravBody>();

        if (gravBody == null)
        {
            Debug.LogWarning($"{name}: gravbody not found", this);
        }

        if (anim == null)
        {
            Debug.LogWarning($"{name}: animator not found", this);
        }

        if (rocketSprite == null)
        {
            Debug.LogWarning($"{name}: rocketSprite not found", this);
        }

    }

    private void LateUpdate()
    {
        ChangeSpriteRotation();
    }

    public void DestroyRocket()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        OnDestroyed?.Invoke();   // 登録先(Manager)に通知
        Destroy(gameObject);     // 実際にオブジェクト破棄
    }

    private void OnDestroy()
    {
        // 直接Destroyされた場合にも通知したいならここでも可
        if (!isDestroyed) OnDestroyed?.Invoke();
    }

    //パラメータをセットするメソッド
    public void SetParameters(float inAngle, float inSpeed)
    {
        this.angle = inAngle;
        this.speed = inSpeed;
    }

    //角度と速さから速度ベクトルを計算するメソッド
    public Vector2 CalculateVelocity()
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
    }

    private void ChangeSpriteRotation()
    {
        if (gravBody == null)
        {
            return;
        }

        if (gravBody.velocity.magnitude < 0.001)
        {
            return;
        }

        Vector2 currDir = gravBody.velocity.normalized;

        float facingAngle = Mathf.Atan2(currDir.y, currDir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, facingAngle);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 150 * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!this.CompareTag("Player"))
        {
            return;
        }

        if (isDestroyed)
        {
            return;
        }

        if (other.CompareTag("GravField") || other.CompareTag("Player"))
        {
            return;
        }

        gravBody.isAnchored = true;
        gravBody.velocity = Vector2.zero;
        GetComponent<CircleCollider2D>().enabled = false;
        
        anim.SetTrigger(Explode);
        SoundManager.Instance.Play("Explosion");
        rocketSprite.SetActive(false);
        StartCoroutine(AfterExplosion());
    }

    IEnumerator AfterExplosion()
    {
        yield return new WaitForSecondsRealtime(explodeLen);
        GameController.Instance.RestartScene();
    }
}
