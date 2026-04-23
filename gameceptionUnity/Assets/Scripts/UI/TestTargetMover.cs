using UnityEngine;

public class TestTargetMover : MonoBehaviour
{
    public Transform target;
    public float speed = 2f;
    public float amplitude = 0.5f;

    private Vector3 startPos;

    void Start()
    {
        startPos = target.position;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * speed) * amplitude;
        float x = Mathf.Sin(Time.time * speed) * amplitude;
        target.position = startPos + new Vector3(x, y, 0);
    }
}