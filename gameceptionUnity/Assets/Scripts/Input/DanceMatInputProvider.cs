using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputLayer
{
    //raw pad input only. no state updates
    public class DanceMatInputProvider : MonoBehaviour
    {
        public event Action<int> OnPadPressed;
        public event Action<int> OnPadReleased;

        [SerializeField, Range(1, 9)] private int maxDigitSelect = 9;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            CheckDigit(kb.digit1Key, 1);
            CheckDigit(kb.digit2Key, 2);
            CheckDigit(kb.digit3Key, 3);
            CheckDigit(kb.digit4Key, 4);
            CheckDigit(kb.digit5Key, 5);
            CheckDigit(kb.digit6Key, 6);
            CheckDigit(kb.digit7Key, 7);
            CheckDigit(kb.digit8Key, 8);
            CheckDigit(kb.digit9Key, 9);
        }

        private void CheckDigit(UnityEngine.InputSystem.Controls.KeyControl key, int digit)
        {
            if (digit < 1 || digit > maxDigitSelect) return;
            int idx = digit - 1;
            
            if (key.wasPressedThisFrame) OnPadPressed?.Invoke(idx);
            if (key.wasReleasedThisFrame) OnPadReleased?.Invoke(idx);
        }
    }
}