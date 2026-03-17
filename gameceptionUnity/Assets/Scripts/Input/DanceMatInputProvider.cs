using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputLayer
{
    //raw pad input only. no state updates
    public class DanceMatInputProvider : MonoBehaviour
    {
        public event Action<int> OnPadPressed;

        [SerializeField, Range(1, 9)] private int maxDigitSelect = 9;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) RaisePadPressed(1);
            if (kb.digit2Key.wasPressedThisFrame) RaisePadPressed(2);
            if (kb.digit3Key.wasPressedThisFrame) RaisePadPressed(3);
            if (kb.digit4Key.wasPressedThisFrame) RaisePadPressed(4);
            if (kb.digit5Key.wasPressedThisFrame) RaisePadPressed(5);
            if (kb.digit6Key.wasPressedThisFrame) RaisePadPressed(6);
            if (kb.digit7Key.wasPressedThisFrame) RaisePadPressed(7);
            if (kb.digit8Key.wasPressedThisFrame) RaisePadPressed(8);
            if (kb.digit9Key.wasPressedThisFrame) RaisePadPressed(9);
        }

        private void RaisePadPressed(int digit)
        {
            if (digit < 1 || digit > maxDigitSelect) return;
            int idx = digit - 1;
            OnPadPressed?.Invoke(idx);
        }
    }
}