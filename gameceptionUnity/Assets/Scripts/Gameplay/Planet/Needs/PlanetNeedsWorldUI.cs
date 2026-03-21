using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class PlanetNeedsWorldUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Planet targetPlanet;
        [SerializeField] private PlanetNeeds targetNeeds;
        [SerializeField] private NeedSlotSprite slotPrefab;

        [Header("Layout")]
        [SerializeField] private float spacing = 2.2f;
        [SerializeField] private Vector3 offset = new Vector3(0f, -5f, 0f);

        private readonly List<NeedSlotSprite> _slotInstances = new();

        private void Awake()
        {
            if (targetPlanet == null)
                targetPlanet = GetComponentInParent<Planet>();

            if (targetNeeds == null && targetPlanet != null)
                targetNeeds = targetPlanet.Needs;
        }

        private void OnEnable()
        {
            Subscribe();
            Rebuild();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (targetNeeds != null)
            {
                targetNeeds.OnNeedsChanged += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (targetNeeds != null)
            {
                targetNeeds.OnNeedsChanged -= Refresh;
            }
        }

        public void Rebuild()
        {
            ClearExisting();

            if (targetNeeds == null || slotPrefab == null)
                return;

            var slots = targetNeeds.Slots;

            for (int i = 0; i < slots.Count; i++)
            {
                NeedSlotSprite instance = Instantiate(slotPrefab, transform);
                _slotInstances.Add(instance);
            }

            LayoutSlots();
            Refresh();
        }

        public void Refresh()
        {
            if (targetNeeds == null)
                return;

            var slots = targetNeeds.Slots;

            if (_slotInstances.Count != slots.Count)
            {
                Rebuild();
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                _slotInstances[i].SetSlot(slots[i].element, slots[i].state);
            }
        }

        private void LayoutSlots()
        {
            int count = _slotInstances.Count;
            float totalWidth = (count - 1) * spacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector3 localPos = offset + new Vector3(startX + i * spacing, 0f, 0f);
                _slotInstances[i].transform.localPosition = localPos;
            }
        }

        private void ClearExisting()
        {
            for (int i = 0; i < _slotInstances.Count; i++)
            {
                if (_slotInstances[i] != null)
                {
                    Destroy(_slotInstances[i].gameObject);
                }
            }

            _slotInstances.Clear();
        }
    }
}