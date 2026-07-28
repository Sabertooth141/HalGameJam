using System;
using Unity.VectorGraphics;
using UnityEngine;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [SerializeField]
    private Bounds playArea = new(Vector3.zero, new Vector3(100, 60, 100));

    private Vector2 rocketPos;
    private bool restarting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        rocketPos = Vector2.zero;
    }

    private void LateUpdate()
    {
        OutOfBoundsCheck();
    }

    public void SetRocketPos(Vector2 inPos)
    {
        rocketPos = inPos;
    }

    private void OutOfBoundsCheck()
    {
        if (restarting || playArea.Contains(rocketPos))
        {
            return;
        }

        restarting = true;
        GameController.Instance.RestartScene();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(playArea.center, playArea.size);
    }
}