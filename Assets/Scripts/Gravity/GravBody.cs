using System;
using UnityEngine;

public class GravBody : MonoBehaviour
{
    [Tooltip("質量")]
    public float mass = 1;

    [Tooltip("天体の速度")]
    public Vector2 velocity;

    [Tooltip("天体を固定")]
    public bool isAnchored;

    [Tooltip("この天体専用の最小距離。0ならグローバル設定を使う")]
    public float softeningRadius = 0f;

    private GravBody orbitingBody;

    [HideInInspector] public Vector2 position;
    [HideInInspector] public Vector2 acceleration;

    private void OnEnable()
    {
        position = transform.position;
        GravSystem.Register(this);
    }

    private void OnDisable()
    {
        GravSystem.Unregister(this);
    }

    /// <summary>
    /// 直接object.transform.positionをセットするよりこれを使う
    /// </summary>
    /// <param name="worldPos">　ワールド座標 </param>
    public void Teleport(Vector2 worldPos)
    {
        position = worldPos;

        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    /// <summary>
    /// 運動量を加える
    /// </summary>
    /// <param name="deltaV">速度変動</param>
    public void AddImpulse(Vector2 deltaV)
    {
        velocity += deltaV;
    }

    public void SetOrbitingBody(GravBody inOrbitingBody)
    {
        orbitingBody = inOrbitingBody;
    }

    public GravBody GetOrbitingBody()
    {
        return orbitingBody;
    }
}