using System;
using System.Collections.Generic;
using UnityEngine;

namespace InputLayer
{
    public class SelectionState : MonoBehaviour
    {
        public event Action<IReadOnlyCollection<int>> OnChanged;

        private readonly HashSet<int> _selected = new();

        public IReadOnlyCollection<int> Selected => _selected;

        public bool IsSelected(int index) => _selected.Contains(index);

        public void Clear()
        {
            if (_selected.Count == 0) return;

            _selected.Clear();
            OnChanged?.Invoke(_selected);
        }

        public void Toggle(int index)
        {
            bool added = _selected.Add(index);
            if (!added)
                _selected.Remove(index);
            OnChanged?.Invoke(_selected);
        }

        public void SoloSelect(int index)
        {
            bool changed = _selected.Count != 1 || !_selected.Contains(index);
            if (!changed) return;

            _selected.Clear();
            _selected.Add(index);
            OnChanged?.Invoke(_selected);
        }

        public void SetSelection(IEnumerable<int> indices)
        {
            _selected.Clear();

            foreach (var i in indices)
                _selected.Add(i);

            OnChanged?.Invoke(_selected);
        }

        public void PruneInvalid(int planetCount)
        {
            bool changed = false;
            var copy = new List<int>(_selected);

            foreach (var i in copy)
            {
                if (i < 0 || i >= planetCount)
                {
                    _selected.Remove(i);
                    changed = true;
                }
            }

            if (changed)
                OnChanged?.Invoke(_selected);
        }
    }
}