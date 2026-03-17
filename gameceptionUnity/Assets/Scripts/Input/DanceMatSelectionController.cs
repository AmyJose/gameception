using UnityEngine;
using UnityEngine.InputSystem;

namespace InputLayer
{
    //turns pad presses into selection changes in the global state
    public class DanceMatSelectionController : MonoBehaviour
    {
        [SerializeField] private DanceMatInputProvider inputProvider;
        [SerializeField] private SelectionState selectionState;

        [Header("Selection Rules")]
        [SerializeField] private bool zeroClearsSelection = true;
        [SerializeField] private bool shiftToSoloSelect = true;
        [SerializeField] private bool autoSelectFirst = true;
        [SerializeField] private int planetCount = 0;

        [Header("State")]
        [SerializeField] private bool selectionEnabled = true;

        public bool SelectionEnabled
        {
            get => selectionEnabled;
            set => selectionEnabled = value;
        }

        public void SetSelectionEnabled(bool enabled)
        {
            selectionEnabled = enabled;
        }

        public void SetPlanetCount(int count)
        {
            planetCount = Mathf.Max(0, count);

            if (selectionState != null)
                selectionState.PruneInvalid(planetCount);

            if (autoSelectFirst && planetCount > 0 && selectionState != null && selectionState.Selected.Count == 0)
            {
                selectionState.SoloSelect(0);
            }
        }

        private void Start()
        {
            if (selectionState == null)
            {
                Debug.LogError("[DanceMatSelectionController] SelectionState is not assigned.");
                return;
            }

            if (inputProvider == null)
            {
                Debug.LogError("[DanceMatSelectionController] DanceMatInputProvider is not assigned.");
                return;
            }

            if (autoSelectFirst && planetCount > 0 && selectionState.Selected.Count == 0)
            {
                selectionState.SoloSelect(0);
            }
        }

        private void OnEnable()
        {
            if (inputProvider != null)
                inputProvider.OnPadPressed += HandlePadPressed;
        }

        private void OnDisable()
        {
            if (inputProvider != null)
                inputProvider.OnPadPressed -= HandlePadPressed;
        }

        private void Update()
        {
            if (!selectionEnabled || selectionState == null) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (zeroClearsSelection && kb.digit0Key.wasPressedThisFrame)
            {
                selectionState.Clear();
            }
        }

        private void HandlePadPressed(int idx)
        {
            if (!selectionEnabled || selectionState == null) return;
            if (!IsIndexAllowed(idx)) return;

            var kb = Keyboard.current;
            bool shiftHeld = shiftToSoloSelect &&
                             kb != null &&
                             (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);

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
            if (idx >= planetCount) return false;
            return true;
        }
    }
}