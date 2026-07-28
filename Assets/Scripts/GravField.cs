using System;
using UnityEngine;

public class GravField : MonoBehaviour
{
    [Tooltip("重力の向きと強さ")]
    [SerializeField] private Vector2 direction;

    private void OnTriggerStay2D(Collider2D other)
    {
        GravBody body = other.GetComponent<GravBody>();
        if (body != null)
        {
            body.AddImpulse(direction);
        }
    }
}
