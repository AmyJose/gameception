using UnityEngine;

public class PlanetExplosionTimer : MonoBehaviour
{
    public Animator anim1; 
    public Animator anim2;
    
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
            if(anim1 != null) anim1.SetTrigger("playexplosion");
            if(anim2 != null) anim2.SetTrigger("playexplosion");
            
            timer = interval;
        }
    }
}