using UnityEngine;

public class PlantSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [SerializeField] private float maxRotationAngle = 10f; 
    [SerializeField] private float cycleTime = 1.0f; // 1 full cycle (s)
    
    private float offset;
    private Quaternion initialRotation; 

    void Start()
    {
        // current rotation from the Unity Editor
        initialRotation = transform.localRotation;
        
        // randomize start orientation so plants aren't in sync
        // offset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // converting seconds into math frequency - 2 * PI / cycleTime 
        float frequency = (2f * Mathf.PI) / cycleTime;
        
        // sway offset
        float swayZ = Mathf.Sin((Time.time) * frequency) * maxRotationAngle; // time.time + offset here if want random)

        // apply sway on top of current rotation
        transform.localRotation = initialRotation * Quaternion.Euler(0, 0, swayZ);
    }
}