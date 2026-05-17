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
        public RectTransform Clone;
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

        /// <summary>The clone pool owned by this tray, exposed so GameManager can pass it to TweenController.</summary>
        public TrayClonePool Pool => _pool;

        /// <summary>
        /// Returns true if the tray currently contains 3 or more tiles of the same type,
        /// meaning a match is still achievable without adding new tiles.
        /// </summary>
        public bool HasPotentialMatch()
        {
            return _entries
                .GroupBy(e => e.Type)
                .Any(g => g.Count() >= 3);
        }

        /// <summary>
        /// Called by GameManager when a tile is selected.
        /// Spawns a UI clone at the tile's screen position and returns it together with the
        /// world-space target position of the next free slot so TweenController can animate the move.
        /// </summary>
        public (RectTransform clone, Vector2 targetPosition) TryAddTile(TileView tileView)
        {
            if (IsFull)
            {
                Debug.LogWarning("[Tray] TryAddTile called on a full tray.");
                return (null, Vector2.zero);
            }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(tileView.transform.position);

            GameObject clone = _pool.Get(transform);
            RectTransform cloneRect = clone.GetComponent<RectTransform>();
            cloneRect.sizeDelta = new Vector2(_cloneSize, _cloneSize);

            // Opt out of HorizontalLayoutGroup so the clone overlays the slot image.
            LayoutElement layoutElement = clone.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = clone.AddComponent<LayoutElement>();
            }
            layoutElement.ignoreLayout = true;

            // Start at the tile's screen position — TweenController animates from here.
            cloneRect.position = new Vector3(screenPos.x, screenPos.y, 0f);

            Image cloneImage = clone.GetComponent<Image>();
            cloneImage.sprite = tileView.IconRenderer.sprite;

            int slotIndex = _entries.Count;
            Vector2 targetPosition = _slots[slotIndex].position;

            _entries.Add(new TrayEntry { Type = tileView.Data.Type, Clone = cloneRect });

            if (IsFull)
            {
                OnTrayFull?.Invoke();
            }

            return (cloneRect, targetPosition);
        }

        /// <summary>
        /// Scans the tray for 3 tiles of the same type and removes them from the entry list.
        /// Returns the matched clones' RectTransforms so GameManager can pass them to
        /// TweenController for the removal animation. Returns null if no match is found.
        /// Pool return is handled by TweenController after the animation completes.
        /// </summary>
        public List<RectTransform> TryMatch()
        {
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                List<TrayEntry> matches = _entries.Where(e => e.Type == type).Take(3).ToList();
                if (matches.Count < 3)
                {
                    continue;
                }

                List<RectTransform> clones = new();
                foreach (TrayEntry entry in matches)
                {
                    clones.Add(entry.Clone);
                    _entries.Remove(entry);
                }

                OnMatchFound?.Invoke();
                return clones;
            }

            return null;
        }

        /// <summary>
        /// Builds the list of moves needed to collapse remaining clones into consecutive slots.
        /// Returns (clone, targetWorldPosition) pairs without moving anything — GameManager
        /// passes this to TweenController to animate.
        /// </summary>
        public List<(RectTransform clone, Vector2 targetPosition)> CollapseSlots()
        {
            var moves = new List<(RectTransform, Vector2)>();
            for (int i = 0; i < _entries.Count; i++)
            {
                moves.Add((_entries[i].Clone, _slots[i].position));
            }
            return moves;
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
