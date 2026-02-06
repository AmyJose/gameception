using UnityEngine;

public class AlienMovement : MonoBehaviour
{
    public float orbitSpeed = 30f; // degrees per second

    void Update()
    {
        transform.Rotate(Vector3.forward, orbitSpeed * Time.deltaTime);
    }
}
