using UnityEngine;
using System;
// Stores the state/values of the elements used in the game eg. (Water, Fire, Earth)
[Serializable]
public class ElementState
{
    [Header("Current Levels")]
    public float air;
    public float earth;
    public float fire;
    public float water;
}
