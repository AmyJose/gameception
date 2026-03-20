// using System.Collections.Generic;
// using UnityEngine;
// using InputLayer;

// namespace Gameplay
// {
//     [System.Serializable]
//     public class ComboRecipe
//     {
//         public string name;
//         public List<ElementPose> sequence; // e.g. [Water, Air]
//     }

//     public class ComboSystem : MonoBehaviour
//     {
//         [SerializeField] private List<ComboRecipe> recipes;

//         // Rolling window of last inputs
//         private readonly List<ElementPose> _buffer = new();
//         [SerializeField] private int maxBuffer = 4;

//         public void RegisterHit(ElementPose element, List<int> targets, int beatIndex)
//         {
//             Push(element);
//             TryResolve(targets);
//         }

//         public void RegisterMiss(int beatIndex)
//         {
//             //clear buffer on miss
//             _buffer.Clear();
//             Debug.Log("ComboSystem: miss");
//         }

//         private void Push(ElementPose element)
//         {
//             _buffer.Add(element);
//             Debug.Log($"ComboSystem: added element {element} to buffer");
//             if (_buffer.Count > maxBuffer)
//                 _buffer.RemoveAt(0);
//         }

//         private void TryResolve(List<int> targets)
//         {
//             foreach (var r in recipes)
//             {
//                 if (EndsWith(_buffer, r.sequence))
//                 {
//                     TriggerCombo(r, targets);
//                     _buffer.Clear();
//                     break;
//                 }
//             }
//         }

//         private bool EndsWith(List<ElementPose> buffer, List<ElementPose> seq)
//         {
//             if (seq.Count == 0 || buffer.Count < seq.Count) return false;

//             int start = buffer.Count - seq.Count;
//             for (int i = 0; i < seq.Count; i++)
//                 if (buffer[start + i] != seq[i]) return false;

//             return true;
//         }

//         private void TriggerCombo(ComboRecipe recipe, List<int> targets)
//         {
//             Debug.Log($"COMBO! {recipe.name} with {recipe.sequence.Count} elements on {targets.Count} planet(s)");
//             // TODO: apply special effect
//         }
//     }
// }


using System.Collections.Generic;
using UnityEngine;
using InputLayer;

namespace Gameplay
{
    [System.Serializable]
    public class ComboRecipe
    {
        public string name;
        public List<ElementPose> sequence;
    }

    public class ComboSystem : MonoBehaviour
    {
        [SerializeField] private List<ComboRecipe> recipes;
        [SerializeField] private PlanetManager planetManager;

        private readonly List<ElementPose> _buffer = new();
        [SerializeField] private int maxBuffer = 4;

//how long the buffer is
        private float _bufferTimer;
        [SerializeField] private float bufferTimeout = 4f;

//how long after a hit to wait for more inputs
        [SerializeField] private float comboResolveDelay = 1f;
        private float _resolveTimer;
        private List<int> _pendingTargets;
        private bool _waitingToResolve;

// //cooldown to prevent same hits on multiple beat
//         private float _hitCooldown;
//         [SerializeField] private float hitCooldownDuration = 0.5f;

        private void Update()
        {
            // buffer timeout
            if (_buffer.Count > 0)
            {
                _bufferTimer += Time.deltaTime;
                if (_bufferTimer >= bufferTimeout)
                {
                    _buffer.Clear();
                    Debug.Log("ComboSystem: buffer timed out");
                }
            }

            // resolve delay
            if (_waitingToResolve)
            {
                _resolveTimer += Time.deltaTime;
                if (_resolveTimer >= comboResolveDelay)
                {
                    _waitingToResolve = false;
                    TryResolve(_pendingTargets);
                }
            }
        }

        public void RegisterHit(ElementPose element, List<int> targets, int beatIndex)
        {
            Push(element);
            _pendingTargets = targets;
            _waitingToResolve = true;
            _resolveTimer = 0f;
        }

        public void RegisterMiss(int beatIndex)
        {
            ResetCombo();
            Debug.Log("ComboSystem: miss");
        }

        public void ResetCombo()
        {
            _buffer.Clear();
            _bufferTimer = 0f;
            _resolveTimer = 0f;
            _waitingToResolve = false;
            _pendingTargets = null;
        }

        private void Push(ElementPose element)
        {
            _bufferTimer = 0f;
            _buffer.Add(element);
            Debug.Log($"ComboSystem: added element {element} to buffer");
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
                    _bufferTimer = 0f;
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
            Debug.Log($"COMBO! {recipe.name} with {recipe.sequence.Count} elements on {targets.Count} planet(s)");
            // TODO: apply special effect
            foreach (var planetIndex in targets)
            {
                Planet p = planetManager.GetPlanet(planetIndex);
                if (p == null) continue;
                //p.ApplyComboEffect(recipe.name);
            }
        }
    }
}