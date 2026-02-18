public class Alien
{
    public float mood;

    public float lifespan;

    //might not be a good idea to have need here
    //a planetary need that influences mood might be better
    public float need;

    public bool IsAlive => lifespan > 0;

    public void UpdateState(float habitability, float dt)
    {
        // Example rules (tweak later)
        mood += (habitability - 0.5f) * dt;
        lifespan -= (1f - habitability) * dt;
        //need to think of some way to update need
    }
}
