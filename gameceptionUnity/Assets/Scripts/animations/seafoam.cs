using UnityEngine;

public class SeafoamScroll : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 0.06f;
    private Material mat;

    void Start()
    {
        // This gets a unique instance of the material, which is correct for scrolling
        mat = GetComponent<SpriteRenderer>().material;
    }

    void Update()
    {
        // Calculate the offset
        float offset = Time.time * scrollSpeed;
        
        // Use mainTextureOffset to work correctly with SpriteRenderers
        mat.mainTextureOffset = new Vector2(offset, 0);
    }
}