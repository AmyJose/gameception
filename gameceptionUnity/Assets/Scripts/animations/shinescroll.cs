using UnityEngine;

public class ShineScroll : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float interval = 5.0f;      // play every n seconds
    [SerializeField] private float speed = 1.0f;
    
    private Material mat;
    private float timer;
    private bool isScrolling = false;

    void Start()
    {
        mat = GetComponent<SpriteRenderer>().material;
        timer = 0;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // check if it's time to start a new run
        if (timer >= interval)
        {
            timer = 0;
            isScrolling = true;
        }

        if (isScrolling)
        {
            // calculate how far through the single scroll we are (0 to 1)
            float progress = timer / speed;

            if (progress <= 1.0f)
            {
                //moving right to left
                mat.mainTextureOffset = new Vector2(progress, 0);
            }
            else
            {
                // reset movement offset, wait for next interval
                mat.mainTextureOffset = Vector2.zero;
                isScrolling = false;
            }
        }
    }
}