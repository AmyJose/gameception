using UnityEngine;
using UnityEngine.UI;

public class ResourceBarTracker: MonoBehaviour
{
    [Header("settings")]
    [SerializeField] private Image bar;
    [SerializeField] private float currentResource =100;
    [SerializeField] private float maxResource =100;


    private void UpdateBar()
    {

        float fillAmount = (float) currentResource/maxResource;
        bar.fillAmount = fillAmount;
    }

    public void SetValue(float current)
    {
        currentResource = current;
        UpdateBar();
        
    }


    
}
