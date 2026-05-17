using System;
using System.Collections.Generic;
using System.Linq;
using TripleMatchFrenzy.Data;
using TripleMatchFrenzy.Views;
using UnityEngine;
using UnityEngine.UI;

namespace TripleMatchFrenzy.Tray
{
    /// <summary>A single occupied slot entry in the tray.</summary>
    public class TrayEntry
    {
        public TileType Type;
        public GameObject CloneObject;
    }

    /// <summary>
    /// MonoBehaviour managing the 7-slot tray at the bottom of the screen.
    /// Handles tile insertion, triple-match detection, and gap closing after removal.
    /// Has no knowledge of OcclusionGraph, GridGenerator, or the board state.
    /// </summary>
    public class Tray : MonoBehaviour
    {
        [SerializeField]
        private RectTransform[] _slots;

        [SerializeField]
        private GameObject _trayClonePrefab;

        [SerializeField]
        private Canvas _trayCanvas;

        [SerializeField]
        private float _cloneSize = 80f;

        private TrayClonePool _pool;
        private readonly List<TrayEntry> _entries = new();

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        /// <summary>Fired immediately after 3 matched tiles are removed from the tray.</summary>
        public Action OnMatchFound;

        /// <summary>Fired when all 7 slots are occupied after a tile is added.</summary>
        public Action OnTrayFull;

        /// <summary>True if all 7 slots are occupied.</summary>
        public bool IsFull => _entries.Count >= _slots.Length;

        /// <summary>
        /// Called by GameManager when a tile is selected.
        /// Spawns a UI clone at the tile's canvas-space position then snaps it to the next free slot.
        /// The initial canvas position is preserved so TweenController can animate from it next prompt.
        /// Returns false if the tray is already full.
        /// </summary>
        public bool TryAddTile(TileView tileView)
        {
            if (IsFull)
            {
                return false;
            }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(tileView.transform.position);

            GameObject clone = _pool.Get(transform);
            RectTransform cloneRect = clone.GetComponent<RectTransform>();
            cloneRect.sizeDelta = new Vector2(_cloneSize, _cloneSize);

            // Opt out of the HorizontalLayoutGroup so the clone overlays the slot rather than
            // becoming a new row item.
            LayoutElement layoutElement = clone.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = clone.AddComponent<LayoutElement>();
            }
            layoutElement.ignoreLayout = true;

            // Place at tile's screen position (start point for TweenController next prompt).
            cloneRect.position = new Vector3(screenPos.x, screenPos.y, 0f);

            Image cloneImage = clone.GetComponent<Image>();
            cloneImage.sprite = tileView.IconRenderer.sprite;

            // Snap to slot via world-space position so anchor/pivot differences don't matter.
            int slotIndex = _entries.Count;
            cloneRect.position = _slots[slotIndex].position;

            _entries.Add(new TrayEntry { Type = tileView.Data.Type, CloneObject = clone });

            if (IsFull)
            {
                OnTrayFull?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// Scans the tray for 3 tiles of the same type and removes them.
        /// Calls <see cref="CollapseSlots"/> and fires <see cref="OnMatchFound"/> if a match is found.
        /// </summary>
        /// <returns>True if a match was found and removed.</returns>
        public bool TryMatch()
        {
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                List<TrayEntry> matches = _entries.Where(e => e.Type == type).Take(3).ToList();
                if (matches.Count < 3)
                {
                    continue;
                }

                foreach (TrayEntry entry in matches)
                {
                    _pool.Return(entry.CloneObject);
                    _entries.Remove(entry);
                }

                CollapseSlots();
                OnMatchFound?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>Repositions remaining clones to fill any gaps left to right.</summary>
        public void CollapseSlots()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                RectTransform cloneRect = _entries[i].CloneObject.GetComponent<RectTransform>();
                cloneRect.position = _slots[i].position;
            }
        }

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            _pool = GetComponent<TrayClonePool>();
            _pool.Setup(_trayClonePrefab);
        }
    }
}
