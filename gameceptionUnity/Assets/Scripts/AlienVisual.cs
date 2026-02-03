using UnityEngine;
public class AlienVisual : MonoBehaviour
{
    public AlienManager manager;

    //this is general exploring how to use mood for happy or angry aliens 
    void Update()
    {
        gameObject.SetActive(manager.Population > 0);
    }
}
