using System.Collections.Generic;
using UnityEngine;
using InputLayer;

namespace Gameplay
{
    [System.Serializable]
    public class ComboRecipe
    {
        public string name;
        public List<ElementPose> sequence; // e.g. [Water, Air]
    }

    public class ComboSystem : MonoBehaviour
    {
        [SerializeField] private List<ComboRecipe> recipes;

        // Rolling window of last inputs
        private readonly List<ElementPose> _buffer = new();
        [SerializeField] private int maxBuffer = 4;

        public void RegisterHit(ElementPose element, List<int> targets, int beatIndex)
        {
            Push(element);
            TryResolve(targets);
        }

        public void RegisterMiss(int beatIndex)
        {
            //clear buffer on miss
            _buffer.Clear();
            Debug.Log("ComboSystem: miss");
        }

        private void Push(ElementPose element)
        {
            _buffer.Add(element);
            Debug.Log("ComboSystem: added element to buffer");
            if (_buffer.Count > maxBuffer)
                _buffer.RemoveAt(0);
        }

        private void TryResolve(List<int> targets)
        {
            foreach (var r in recipes)
            {
                if (EndsWith(_buffer, r.sequence))
                {
                    TriggerCombo(r, targets);
                    _buffer.Clear();
                    break;
                }
            }
        }

        private bool EndsWith(List<ElementPose> buffer, List<ElementPose> seq)
        {
            if (seq.Count == 0 || buffer.Count < seq.Count) return false;

            int start = buffer.Count - seq.Count;
            for (int i = 0; i < seq.Count; i++)
                if (buffer[start + i] != seq[i]) return false;

            return true;
        }

        private void TriggerCombo(ComboRecipe recipe, List<int> targets)
        {
            Debug.Log($"COMBO! {recipe.name} on {targets.Count} planet(s)");
            // TODO: apply special effect
        }
    }
}