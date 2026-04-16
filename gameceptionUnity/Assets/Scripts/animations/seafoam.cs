using UnityEngine;

public partial class SeafoamScroll : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 0.5f;
    private MeshRenderer meshRenderer;
    private Material mat;

    void Start()
    {
        mat = GetComponent<SpriteRenderer>().material;
    }

    void Update()
    {
        // offset based on time
        float offset = Time.time * scrollSpeed;
        
        // apply offset to texture's X axis
        mat.mainTextureOffset = new Vector2(offset, 0);
    }
}