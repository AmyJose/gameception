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
        slots.Clear();
        _decayTimer = 0f;
        if (definition == null)
        {
            Debug.LogWarning("[PlanetNeeds] InitialiseFromDefinition called with null definition");
            NotifyChanged();
            return;
        }

        if(definition.requiredNeeds == null || definition.requiredNeeds.Count == 0)
        {
            Debug.LogWarning($"[PlanetNeeds] PlanetDefinition '{definition.name}' has no requirements");
            NotifyChanged();
            return;
        }

        NeedState initialState = startFilled ? NeedState.Filled : NeedState.Empty;

        foreach (ElementPose element in definition.requiredNeeds)
        {
            slots.Add(new NeedSlot { element = element, state = initialState });
        }

        if (showDebugLogs)
        {
            Debug.Log($"[PlanetNeeds] Initialised {slots.Count} need slots for '{definition.name}'.");
        }

        NotifyChanged();
    }
    public void Tick(float dt, float decayMultiplier = 1f)
    {
        if (slots.Count == 0) return;

        float safeMultipler = Mathf.Max(0.01f, decayMultiplier);
        float currentDecayInterval = baseDecayInterval / safeMultipler;

        _decayTimer += dt;

        if(_decayTimer >= currentDecayInterval)
        {
            _decayTimer = 0f;
            DecayOneStep();
        }
    }

    //restores one slot matching the given element.
    public bool RestoreNeed(ElementPose element)
    {
        //two seperate for loops for this prioritisation
        for (int i = 0; i <slots.Count; i++)
        {
            if (slots[i].element == element && slots[i].state == NeedState.Fading)
            {
                slots[i].state = NeedState.Filled;

                if (showDebugLogs) Debug.Log($"[PlanetNeeds] restored FADING {element} slot to FILLED");
                NotifyChanged();
                return true;
            }
        }
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].element == element && slots[i].state == NeedState.Empty)
            {
                slots[i].state = NeedState.Filled;

                if (showDebugLogs) Debug.Log($"[PlanetNeeds] restored EMPTY {element} slot to FILLED");
                NotifyChanged();
                return true;
            }
        }
        if (showDebugLogs) Debug.Log($"[PlanetNeeds] No restorable slot found for {element}");
        return false;
    }
    //decays a slot by one step
    public void DecayOneStep()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].state == NeedState.Filled)
            {
                slots[i].state = NeedState.Fading;

                if (showDebugLogs) Debug.Log($"[PlanetNeeds] Decayed {slots[i].element} from FILLED to FADING");
                NotifyChanged();
                return;
            }
        }
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].state == NeedState.Fading)
            {
                slots[i].state = NeedState.Empty;

                if (showDebugLogs) Debug.Log($"[PlanetNeeds] Decayed {slots[i].element} from FADING to EMPTY");
                NotifyChanged();
                return;
            }
        }
        if (showDebugLogs) Debug.Log($"[PlanetNeeds] All slots already empty. No decay");
    }
    public float GetStabilityRatio()
    {
        if (slots.Count == 0) return 0f;

        float score = 0f;

        for (int i = 0; i < slots.Count; i++)
        {
            switch (slots[i].state)
            {
                case NeedState.Filled:
                    score += 1f;
                    break;

                case NeedState.Fading:
                    score += 0.5f;
                    break;

                case NeedState.Empty:
                    score += 0f;
                    break;
            }
        }

        return score / slots.Count;
    }
    public int GetMissingNeedCount()
    {
        int count = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].state == NeedState.Empty)
            {
                count++;
            }
        }

        return count;
    }
    public int GetFilledNeedCount()
    {
        int count = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].state == NeedState.Filled)
            {
                count++;
            }
        }

        return count;
    }
    public int GetFadingNeedCount()
    {
        int count = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].state == NeedState.Fading)
            {
                count++;
            }
        }

        return count;
    }
    public bool AreAllNeedsMet()
    {
        if (slots.Count == 0) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].state != NeedState.Filled)
            {
                return false;
            }
        }

        return true;
    }
    public bool NeedsElement(ElementPose element)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].element == element)
            {
                return true;
            }
        }

        return false;
    }
    public void SetAllNeeds(NeedState state)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].state = state;
        }

        NotifyChanged();
    }
    public void ClearAllNeeds()
    {
        SetAllNeeds(NeedState.Empty);
    }
    private void NotifyChanged()
    {
        OnNeedsChanged?.Invoke();
    }
}
