using InputLayer;
using System;
using System.Collections.Generic;
using UnityEngine;
public enum NeedState
{
    Empty,
    Fading,
    Filled
}
[Serializable]
public class NeedSlot
{
    public ElementPose element;
    public NeedState state = NeedState.Empty;
}
public class PlanetNeeds : MonoBehaviour
{
    [Header("Needs config")]
    [SerializeField] private List<NeedSlot> slots = new();
    [SerializeField] private float baseDecayInterval = 5f;
    [SerializeField] private bool startFilled = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private float _decayTimer = 0f;
    public IReadOnlyList<NeedSlot> Slots => slots;
    public event Action OnNeedsChanged;

    public void InitialiseFromDefinition(PlanetDefinition definition)
    {

    }
    public void Tick(float dt, float decayMultiplier = 1f)
    {

    }
    public bool RestoreNeed(ElementPose element)
    {
        return false;
    }
    public void DecayOneStep()
    {

    }
    public float GetStabilityRatio()
    {
        return 0f;
    }
    public int GetMissingNeedCount()
    {
        return 0;
    }
    public int GetFilledNeedCount()
    {
        return 0;
    }
    public int GetFadingNeedCount()
    {
        return 0;
    }
    public bool AreAllNeedsMet()
    {
        return false;
    }
    public bool NeedsElement(ElementPose element)
    {
        return false;
    }
    public void SetAllNeeds(NeedState state)
    {

    }
    public void ClearAllNeeds()
    {

    }
    private void NotifyChanged()
    {
        OnNeedsChanged?.Invoke();
    }
}
