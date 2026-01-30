using UnityEngine;

public class AlienSpawner : MonoBehaviour
{
    // These slots will appear in the Inspector once the names match
    public GameObject alienPrefab; 
    public Transform planetTransform; 
    private GameObject currentAlien;

    void Update()
    {
        // Press 'E' to spawn
        if (Input.GetKeyDown(KeyCode.E) && currentAlien == null)
        {
            SpawnAlien();
        }
    }

    void SpawnAlien()
    {
        Vector3 spawnPos = planetTransform.position + new Vector3(0, 1, 0);
        currentAlien = Instantiate(alienPrefab, spawnPos, Quaternion.identity);
        currentAlien.GetComponent<AlienMovement>().planet = planetTransform;
    }
}