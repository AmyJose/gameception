using UnityEngine;
//stateless logic/calc. class
public static class HabitabilityLogic
{
    public static float Compute (ElementState elements, float imbalancePenalty = 0.05f)
    {
        float imbalance = Mathf.Abs(elements.air - elements.water) + Mathf.Abs(elements.fire - elements.earth);
        float score = 1f - imbalance * imbalancePenalty;
        return Mathf.Clamp01(score);   
    }
}
