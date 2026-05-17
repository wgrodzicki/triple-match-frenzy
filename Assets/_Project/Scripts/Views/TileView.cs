using System;
using System.Collections.Generic;
using TripleMatchFrenzy.Data;
using UnityEngine;

namespace TripleMatchFrenzy.Views
{
    /// <summary>
    /// MonoBehaviour attached to each tile GameObject.
    /// Pure view and data holder: owns the tile's data model, drives its visual state,
    /// and exposes <see cref="OnSelected"/> for GameManager to invoke.
    /// Has no knowledge of the tray, the occlusion graph, grid generation, or input.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TileView : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer _backgroundRenderer;

        [SerializeField]
        private SpriteRenderer _foregroundRenderer;

        [SerializeField]
        private SpriteRenderer _iconRenderer;

        [SerializeField]
        private BoxCollider2D _collider;

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        /// <summary>The data model backing this tile.</summary>
        public TileData Data { get; private set; }

        /// <summary>The renderer displaying this tile's background layer.</summary>
        public SpriteRenderer BackgroundRenderer => _backgroundRenderer;

        /// <summary>The renderer displaying this tile's foreground border or frame.</summary>
        public SpriteRenderer ForegroundRenderer => _foregroundRenderer;

        /// <summary>The renderer displaying this tile's type icon.</summary>
        public SpriteRenderer IconRenderer => _iconRenderer;

        /// <summary>The physics collider used for tap detection.</summary>
        public BoxCollider2D Collider => _collider;

        /// <summary>
        /// Raised by GameManager when a valid tap on this tile is confirmed.
        /// GameManager subscribes immediately after spawning the tile.
        /// </summary>
        public Action<TileView> OnSelected;

        private Dictionary<SpriteRenderer, Color> _defaultColors = new();

        /// <summary>
        /// Assigns the tile's data model, looks up its icon from the sprite library,
        /// and sets the sorting orders of all three renderers based on the tile's layer index.
        /// Call once after instantiating the prefab, before the tile enters the scene.
        /// </summary>
        /// <param name="data">The data model for this tile.</param>
        /// <param name="library">Sprite library used to resolve the tile type to an icon.</param>
        public void Initialize(TileData data, TileTypeLibrary library)
        {
            Data = data;
            _iconRenderer.sprite = library.GetSprite(data.Type);

            int baseOrder = data.LayerIndex * 3;
            _backgroundRenderer.sortingOrder = baseOrder;
            _foregroundRenderer.sortingOrder = baseOrder + 1;
            _iconRenderer.sortingOrder = baseOrder + 2;

            _defaultColors[_backgroundRenderer] = _backgroundRenderer.color;
            _defaultColors[_foregroundRenderer] = _foregroundRenderer.color;
            _defaultColors[_iconRenderer] = _iconRenderer.color;

            UpdateOcclusionVisual();
        }

        public void UpdateOcclusionVisual()
        {
            bool isSelectable = Data.Blockers.Count == 0;
            ApplyOcclusionColor(_backgroundRenderer, isSelectable);
            ApplyOcclusionColor(_foregroundRenderer, isSelectable);
            ApplyOcclusionColor(_iconRenderer, isSelectable);
        }

        private void ApplyOcclusionColor(SpriteRenderer renderer, bool isSelectable)
        {
            const float DarkeningGrade = 0.05f;
            Color def = _defaultColors[renderer];
            renderer.color = isSelectable
                ? def
                : new Color(def.r - DarkeningGrade, def.g - DarkeningGrade, def.b - DarkeningGrade, def.a);
        }

        /// <summary>
        /// Visually hides the tile without destroying the GameObject.
        /// Called by GameManager when the tile is moved into the tray, where the tray
        /// owns its own visual representation.
        /// </summary>
        public void Hide()
        {
            _backgroundRenderer.enabled = false;
            _foregroundRenderer.enabled = false;
            _iconRenderer.enabled = false;
            _collider.enabled = false;
        }

        /// <summary>
        /// Destroys the tile's GameObject. Called by GameManager after a successful match
        /// removes the tile from the board permanently.
        /// </summary>
        public void Dispose()
        {
            Destroy(gameObject);
        }

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (!_collider)
            {
                _collider = GetComponent<BoxCollider2D>();
            }
        }
    }
}
