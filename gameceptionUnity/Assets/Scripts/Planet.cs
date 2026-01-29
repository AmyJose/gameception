using UnityEngine;

public class Planet : MonoBehaviour
{
    public ElementState elements = new ElementState();
    public float habitability;
    public float population;

    public void AddAir(float amount)
    {
        elements.air += amount;
    }
    public void AddWater(float amount)
    {
        elements.water += amount;
    }
    public void AddFire(float amount)
    {
        elements.fire += amount;
    }
    public void AddEarth(float amount)
    {
        elements.earth += amount;
    }
}
