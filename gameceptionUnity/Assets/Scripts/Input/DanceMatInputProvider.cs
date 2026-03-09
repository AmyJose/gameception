using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputLayer
{
    public class DanceMatInputProvider : MonoBehaviour
    {
        public event Action<HashSet<int>> OnSelectionChanged;

        [SerializeField, Range(1, 9)] private int maxDigitSelect = 9;

        [SerializeField] private bool zeroClearsSelection = true;

        [SerializeField] private bool shiftToSoloSelect = true;

        [SerializeField] private bool autoSelectFirst = true;

        [SerializeField] private int planetCount = 0;

        private readonly HashSet<int> _selected = new();

        public IReadOnlyCollection<int> Selected => _selected;

        //call if the planet count changes during runtime
        public void SetPlanetCount(int count)
        {
            planetCount = Mathf.Max(0, count);
            PruneInvalidSelections();
        }

        //given a planet index, add to pressed
        public void SetPressed(int planetIndex, bool pressed)
        {
            if (!IsIndexAllowed(planetIndex)) return;

            bool changed = pressed ? _selected.Add(planetIndex) : _selected.Remove(planetIndex);
            if (changed) RaiseChanged();
        }

        public void ClearSelection()
        {
            if (_selected.Count == 0) return;
            _selected.Clear();
            RaiseChanged();
        }

        public void SoloSelect(int planetIndex)
        {
            if (!IsIndexAllowed(planetIndex)) return;

            bool changed = _selected.Count != 1 || !_selected.Contains(planetIndex);
            if (!changed) return;

            _selected.Clear();
            _selected.Add(planetIndex);
            RaiseChanged();
        }

        private void Start()
        {
            if (autoSelectFirst && _selected.Count == 0)
            {
                if (planetCount == 0 || planetCount > 0)
                {
                    _selected.Add(0);
                    RaiseChanged();
                }
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            bool shiftHeld = shiftToSoloSelect && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);

            if (zeroClearsSelection && kb.digit0Key.wasPressedThisFrame)
            {
                ClearSelection();
                return;
            }

            // Digit keys 1..9 map to indices 0..8
            if (kb.digit1Key.wasPressedThisFrame) HandleDigit(1, shiftHeld);
            if (kb.digit2Key.wasPressedThisFrame) HandleDigit(2, shiftHeld);
            if (kb.digit3Key.wasPressedThisFrame) HandleDigit(3, shiftHeld);
            if (kb.digit4Key.wasPressedThisFrame) HandleDigit(4, shiftHeld);
            if (kb.digit5Key.wasPressedThisFrame) HandleDigit(5, shiftHeld);
            if (kb.digit6Key.wasPressedThisFrame) HandleDigit(6, shiftHeld);
            if (kb.digit7Key.wasPressedThisFrame) HandleDigit(7, shiftHeld);
            if (kb.digit8Key.wasPressedThisFrame) HandleDigit(8, shiftHeld);
            if (kb.digit9Key.wasPressedThisFrame) HandleDigit(9, shiftHeld);
        }

        private void HandleDigit(int digit, bool shiftHeld)
        {
            if (digit < 1 || digit > maxDigitSelect) return;

            int idx = digit - 1;
            if (!IsIndexAllowed(idx)) return;

            if (shiftHeld)
            {
                SoloSelect(idx);
                return;
            }

            // Toggle
            bool changed = !_selected.Add(idx);
            if (!changed)
            {
                RaiseChanged();
            }
            else
            {
                _selected.Remove(idx);
                RaiseChanged();
            }
        }

        private bool IsIndexAllowed(int idx)
        {
            if (idx < 0) return false;
            if (planetCount > 0 && idx >= planetCount) return false;
            return true;
        }

        private void PruneInvalidSelections()
        {
            if (planetCount <= 0) return;

            bool changed = false;
            // copy to avoid modifying while iterating
            var tmp = new List<int>(_selected);
            foreach (var idx in tmp)
            {
                if (idx < 0 || idx >= planetCount)
                {
                    _selected.Remove(idx);
                    changed = true;
                }
            }

            if (changed) RaiseChanged();
        }

        private void RaiseChanged()
        {
            // Send a copy so listeners can't mutate our internal set
            OnSelectionChanged?.Invoke(new HashSet<int>(_selected));
        }
    }
}