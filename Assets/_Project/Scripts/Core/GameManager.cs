using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using TripleMatchFrenzy.Data;
using TripleMatchFrenzy.Generation;
using TripleMatchFrenzy.Tweening;
using TripleMatchFrenzy.UI;
using TripleMatchFrenzy.Utility;
using TripleMatchFrenzy.Views;

namespace TripleMatchFrenzy.Core
{
    /// <summary>
    /// Singleton MonoBehaviour that owns and coordinates all game systems.
    /// Responsible for board spawning, tile lifecycle, and (in later prompts)
    /// input routing, tray management, and win/lose evaluation.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField]
        private AddressablesLoader _loader;

        [SerializeField]
        private TripleMatchFrenzy.Tray.Tray _tray;

        private GameObject _tilePrefab;
        private TileTypeLibrary _tileLibrary;

        [SerializeField]
        private TweenController _tweenController;

        [SerializeField]
        private EndGameUI _endGameUI;

        [SerializeField]
        private float _tileSize = 1f;

        [SerializeField]
        private int _rowCount = 6;

        [SerializeField]
        private Vector2 _boardCenter = Vector2.zero;

        private GridGenerator _gridGenerator;
        private OcclusionGraph _occlusionGraph;
        private List<TileView> _allTiles;

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        /// <summary>The single active instance of <see cref="GameManager"/> in the scene.</summary>
        public static GameManager Instance { get; private set; }

        /// <summary>
        /// The game's current high-level state.
        /// Starts as <see cref="GameState.Loading"/> until <c>SpawnBoard</c> completes,
        /// at which point it transitions to <see cref="GameState.Idle"/>.
        /// </summary>
        public GameState CurrentState { get; private set; }

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _gridGenerator = new GridGenerator();
            _occlusionGraph = new OcclusionGraph();
            _allTiles = new List<TileView>();
        }

        private async UniTaskVoid Start()
        {
            CurrentState = GameState.Loading;
            await _loader.LoadAsync();
            _tilePrefab = _loader.TilePrefab;
            _tileLibrary = _loader.TileLibrary;
            SpawnBoard();
            CurrentState = GameState.Idle;
        }

        private void Update()
        {
            HandleInput();
        }

        // -----------------------------------------------------------------------
        // Board spawning
        // -----------------------------------------------------------------------

        private void SpawnBoard()
        {
            List<TileData> allTileData = _gridGenerator.Generate(
                _boardCenter, _tileSize, _rowCount, new System.Random());

            _occlusionGraph.Build(allTileData, _tileSize);

            foreach (TileData tileData in allTileData)
            {
                Vector2 worldPos2D = GridGenerator.ComputeWorldPosition(
                    tileData.GridPosition,
                    tileData.LayerIndex,
                    _boardCenter,
                    _tileSize,
                    _rowCount);

                GameObject go = Instantiate(
                    _tilePrefab,
                    new Vector3(worldPos2D.x, worldPos2D.y, 0f),
                    Quaternion.identity);

                TileView view = go.GetComponent<TileView>();
                view.Initialize(tileData, _tileLibrary);
                view.OnSelected += tile => OnTileSelected(tile).Forget();
                _allTiles.Add(view);
            }
        }

        // -----------------------------------------------------------------------
        // Input
        // -----------------------------------------------------------------------

        private void HandleInput()
        {
            if (CurrentState != GameState.Idle)
            {
                return;
            }

            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }

            Vector2 screenPos = pointer.position.ReadValue();
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, 0f));

            // OverlapPointAll is used in place of RaycastAll with Vector2.zero direction,
            // which returns no results in Unity's Physics2D (zero-direction rays don't cast).
            TileView topTile = Physics2D.OverlapPointAll(worldPos)
                .Select(hit => hit.GetComponent<TileView>())
                .Where(view => view != null && view.Data != null)
                .OrderByDescending(view => view.Data.LayerIndex)
                .FirstOrDefault();

            if (topTile != null && topTile.Data.IsSelectable && !_tray.IsFull)
            {
                topTile.OnSelected?.Invoke(topTile);
            }
        }

        // -----------------------------------------------------------------------
        // Tile callbacks
        // -----------------------------------------------------------------------

        private async UniTaskVoid OnTileSelected(TileView tileView)
        {
            CurrentState = GameState.Animating;
            tileView.Hide();

            _occlusionGraph.OnTileRemoved(tileView.Data);
            _allTiles.Remove(tileView);
            UpdateAllSelectableVisuals();

            (RectTransform clone, Vector2 targetPos) = _tray.TryAddTile(tileView);
            await _tweenController.AnimateTileToTray(clone, targetPos);

            List<RectTransform> matchedClones = _tray.TryMatch();
            if (matchedClones != null && matchedClones.Count == 3)
            {
                await _tweenController.AnimateMatchRemove(matchedClones, _tray.Pool);

                List<(RectTransform clone, Vector2 targetPosition)> moves = _tray.CollapseSlots();
                await _tweenController.AnimateTrayCollapse(moves);
            }

            if (CheckWin())
            {
                CurrentState = GameState.Won;
                _endGameUI.ShowWin();
                return;
            }

            if (CheckLose())
            {
                CurrentState = GameState.Lost;
                _endGameUI.ShowLose();
                return;
            }

            CurrentState = GameState.Idle;
        }

        private void UpdateAllSelectableVisuals()
        {
            foreach (TileView view in _allTiles)
            {
                view.UpdateOcclusionVisual();
            }
        }

        private bool CheckWin()
        {
            return _allTiles.Count == 0;
        }

        private bool CheckLose()
        {
            if (!_tray.IsFull)
            {
                return false;
            }
            return !_tray.HasPotentialMatch();
        }
    }
}
