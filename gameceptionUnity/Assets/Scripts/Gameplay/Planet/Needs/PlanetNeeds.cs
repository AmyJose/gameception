using InputLayer;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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

    [Header("Decay Weights")]
    [SerializeField] private float filledDecayWeight = 1f;
    [SerializeField] private float fadingDecayWeight = 0.7f;
    [SerializeField] private float lastDecayedPenaltyMultiplier = 0.25f;
    [SerializeField] private bool avoidRepeatingLastDecay = true;
    [SerializeField] private float averageDecayInterval = 4.5f;
    [SerializeField] private float decayJitter = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private int _lastDecayedIndex = -1;
    private float _timeUntilNextDecay = 0f;
    public IReadOnlyList<NeedSlot> Slots => slots;
    public event Action OnNeedsChanged;

    private float GetRandomDecayInterval()
    {
        return Mathf.Max(0.75f, averageDecayInterval + UnityEngine.Random.Range(-decayJitter, decayJitter));
    }

    public void InitialiseFromDefinition(PlanetDefinition definition)
    {
        slots.Clear();
        _timeUntilNextDecay = GetRandomDecayInterval();
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

        _timeUntilNextDecay -= dt * safeMultipler;

        if(_timeUntilNextDecay <=0f)
        {
            DecayOneStep();
            _timeUntilNextDecay = GetRandomDecayInterval();
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
                _timeUntilNextDecay += 0.2f;
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
                _timeUntilNextDecay += 0.3f;
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
        int index = ChooseWeightedDecaySlot();

        if (index < 0 || index >= slots.Count)
        {
            if (showDebugLogs) Debug.Log("[PlanetNeeds] No valid slot available for decay");
            return;
        }

        switch (slots[index].state)
        {
            case NeedState.Filled:
                slots[index].state = NeedState.Fading;
                break;

            case NeedState.Fading:
                slots[index].state = NeedState.Empty;
                break;

            case NeedState.Empty:
                return;
        }

        _lastDecayedIndex = index;

        if (showDebugLogs) Debug.Log($"[PlanetNeeds] Decayed slot {index} ({slots[index].element}). New state = {slots[index].state}");

        NotifyChanged();
    }
    private int ChooseWeightedDecaySlot()
    {
        if (slots.Count == 0) return -1;

        float totalWeight = 0f;
        float[] weights = new float[slots.Count];

        for (int i = 0; i < slots.Count; i++)
        {
            float weight = 0f;

            switch (slots[i].state)
            {
                case NeedState.Filled:
                    weight = filledDecayWeight;
                    break;
                case NeedState.Fading:
                    weight = fadingDecayWeight;
                    break;
                case NeedState.Empty:
                    weight = 0f;
                    break;
            }

            if (avoidRepeatingLastDecay && i == _lastDecayedIndex)
            {
                weight *= lastDecayedPenaltyMultiplier;
            }

            //tiny bit of random jsut sprinkled in there for fun.
            weight *= UnityEngine.Random.Range(0.9f, 1.1f);

            weights[i] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f) return -1;

        float pick = UnityEngine.Random.Range(0, totalWeight);
        float running = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            running += weights[i];
            if (pick <= running) return i;
        }
        return weights.Length -1;
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
