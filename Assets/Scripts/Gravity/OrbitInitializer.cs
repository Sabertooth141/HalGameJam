using System;
using UnityEngine;


[RequireComponent(typeof(GravBody))]
[DefaultExecutionOrder(-50)]
public class OrbitInitializer : MonoBehaviour
{
    [Tooltip("回る天体")]
    [SerializeField] private GravBody parent;

    [Tooltip("時計回り:true／反時計回り:false")]
    [SerializeField] private bool isClockwise;

    [Tooltip("ディバッグ：速度を表示するか")]
    [SerializeField] private bool isDrawGizmo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (parent == null)
        {
            Debug.LogWarning($"{name}: OrbitalInitializer has no parent assigned", this);
            return;
        }

        GravBody body = GetComponent<GravBody>();
        GravSystem system = GravSystem.Instance;

        body.SetOrbitingBody(parent);

        Vector2 offset = body.position - parent.position;
        float orbitR = offset.magnitude;

        if (orbitR < 0.01f)
        {
            return;
        }

        float initialSpeed = system.CircularOrbitSpeed(parent.mass, orbitR);

        //　parentへ方向の垂直方向
        Vector2 tangentDir = new Vector2(-offset.y, offset.x).normalized;
        if (!isClockwise)
        {
            tangentDir = -tangentDir;
        }

        body.velocity = parent.velocity + tangentDir * initialSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        if (!isDrawGizmo || parent == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, parent.transform.position);

        // 軌道予測
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
        float r = Vector2.Distance(transform.position, parent.transform.position);
        Vector3 prev = parent.transform.position + new Vector3(r, 0, 0);
        for (int i = 1; i <= 48; i++)
        {
            float a = i / 48f * Mathf.PI * 2f;
            Vector3 next = parent.transform.position + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}