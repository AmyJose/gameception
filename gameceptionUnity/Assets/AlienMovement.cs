using UnityEngine;

public class AlienMovement : MonoBehaviour
{
    public Transform planet;      
    public float walkSpeed = -40f;    

    void Update()
    {
        // If the spawner hasn't given us a planet yet, don't do anything
        if (planet == null) return;

        // 1. POSITIONING: Keep the alien at a fixed distance from the planet center
        // This acts like "fake gravity" to keep him on the surface
        Vector3 directionToPlanet = (transform.position - planet.position).normalized;
        transform.position = planet.position + directionToPlanet * 2.1f; // Adjust 2.1f based on planet size

        // 2. ROTATION: Keep the alien's feet pointing toward the center
        float angle = Mathf.Atan2(directionToPlanet.y, directionToPlanet.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        // 3. MOVEMENT: Manually slide the alien around the circle
        transform.RotateAround(planet.position, Vector3.forward, walkSpeed * Time.deltaTime);
    }
}