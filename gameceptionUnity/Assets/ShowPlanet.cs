using UnityEngine;
using UnityEngine.InputSystem;

public class ShowPlanet : MonoBehaviour
{
    public GameObject planet;
    public Transform parent;
    public float spacing = 2.0f;
    public float margin = 0.5f; // extra gap between planets
    private int count;
    private GameObject[] planets = new GameObject[4];
    private Vector3 anchorPos;

    void Start()
    {
        anchorPos = planet.transform.position;
        planet.SetActive(false);
        planets[0] = planet;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame && count < 4)
        {   
            var p = parent != null ? parent : planet.transform.parent;
            GameObject current;
            if (count == 0)
            {
                //First time, just activate the existing planet.
                current = planet;
                current.SetActive(true);
                planets[0] = current;
            }
            else
            {
                //Instantiate a new planet as a copy of the original one.
                current = Instantiate(planet, p);
                current.name = $"{planet.name}_Clone_{count}";
                current.SetActive(true);
                planets[count] = current;
            }
            //Increment exactly once per press
            count++;

            //Reposition and scaling all the planets.
            UpdatePlanetLayout();
        }
    }

    void UpdatePlanetLayout(){
        Vector3 basePos = anchorPos; // keep layouts centered on the original position
        Vector3 baseScale = planet.transform.localScale;
        float scale = 1f;

        if (count == 1){
            //the first planet is at the center of the screen.
            planets[0].transform.position = basePos;
            planets[0].transform.localScale = baseScale;
        } 
        else if (count == 2)
        {
            //two planets side by side
            scale = 0.8f;
            float step = spacing *2 + margin; // symmetric about basePos
            planets[0].transform.position = basePos + Vector3.left * step;
            planets[1].transform.position = basePos + Vector3.right * step;
            for (int i = 0; i < 2; i++) planets[i].transform.localScale = baseScale * scale;
        } 
        else if (count == 3)
        {
            //three planets in a horizontal line, but in a smaller scale
            scale = 0.6f;
            float step = spacing*3 + margin; // keep center at basePos
            planets[0].transform.position = basePos + Vector3.left * step;
            planets[1].transform.position = basePos;
            planets[2].transform.position = basePos + Vector3.right * step;
            for (int i = 0; i < 3; i++) planets[i].transform.localScale = baseScale * scale;
        } 
        else if (count == 4)
        {
            //four planets in a square layout, 2x2 grid.
            scale = 0.5f;
            float offset = spacing * 0.5f + margin;
            planets[0].transform.position = basePos + new Vector3(-offset, offset, 0); //top-left
            planets[1].transform.position = basePos + new Vector3(offset, offset, 0); //top-right
            planets[2].transform.position = basePos + new Vector3(-offset, -offset, 0); //bottom-left
            planets[3].transform.position = basePos + new Vector3(offset, -offset, 0); //bottom-right
            for (int i = 0; i < 4; i++)
                planets[i].transform.localScale = baseScale * scale;
        }
    }
}
