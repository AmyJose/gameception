using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputLayer
{
    public class DanceMatInputProvider : MonoBehaviour
    {
        public event Action<int> OnPadPressed;

        [SerializeField, Range(1, 9)] private int maxDigitSelect = 9;
        [SerializeField] private bool zeroClearsSelection = true;
        [SerializeField] private bool shiftToSoloSelect = true;
        [SerializeField] private bool autoSelectFirst = true;
        [SerializeField] private int planetCount = 0;

        [Header("State")]
        [SerializeField] private SelectionState selectionState;

        public void SetPlanetCount(int count)
        {
            planetCount = Mathf.Max(0, count);

            if (selectionState != null)
                selectionState.PruneInvalid(planetCount);
        }

        private void Start()
        {
            if (selectionState == null)
            {
                Debug.LogError("[DanceMatInputProvider] SelectionState is not assigned.");
                return;
            }

            if (autoSelectFirst && planetCount > 0 && selectionState.Selected.Count == 0)
            {
                selectionState.SoloSelect(0);
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || selectionState == null) return;

            bool shiftHeld = shiftToSoloSelect && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);

            if (zeroClearsSelection && kb.digit0Key.wasPressedThisFrame)
            {
                selectionState.Clear();
                return;
            }

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

            OnPadPressed?.Invoke(idx);

            if (shiftHeld)
            {
                selectionState.SoloSelect(idx);
            }
            else
            {
                selectionState.Toggle(idx);
            }
        }

        private bool IsIndexAllowed(int idx)
        {
            if (idx < 0) return false;
            if (planetCount > 0 && idx >= planetCount) return false;
            return true;
        }
    }
}