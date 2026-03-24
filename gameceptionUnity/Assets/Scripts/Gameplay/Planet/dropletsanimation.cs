using UnityEngine;

public class PlanetDroplets : MonoBehaviour
{
    public Animator anim1; 
    
    public float interval = 3.0f;
    private float timer;

    void Start()
    {
        timer = interval;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            if(anim1 != null) anim1.SetTrigger("playanimation");
            timer = interval;
        }
    }
}