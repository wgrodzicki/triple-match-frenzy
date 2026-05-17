using Cysharp.Threading.Tasks;
using TripleMatchFrenzy.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TripleMatchFrenzy.Utility
{
    /// <summary>
    /// Loads game assets via Addressables on startup and exposes them for use by GameManager.
    /// Releases all handles when the owning GameObject is destroyed.
    /// </summary>
    public class AddressablesLoader : MonoBehaviour
    {
        private const string TilePrefabAddress = "Tile";
        private const string TileLibraryAddress = "TileTypeLibrary";

        private AsyncOperationHandle<GameObject> _tilePrefabHandle;
        private AsyncOperationHandle<TileTypeLibrary> _tileLibraryHandle;

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        /// <summary>Loaded tile prefab. Only valid after <see cref="LoadAsync"/> completes successfully.</summary>
        public GameObject TilePrefab { get; private set; }

        /// <summary>Loaded TileTypeLibrary SO. Only valid after <see cref="LoadAsync"/> completes successfully.</summary>
        public TileTypeLibrary TileLibrary { get; private set; }

        /// <summary>
        /// Async loads the tile prefab and TileTypeLibrary in parallel via Addressables.
        /// Awaitable — completes when both assets are ready.
        /// Logs an error if either asset fails to load.
        /// </summary>
        public async UniTask LoadAsync()
        {
            _tilePrefabHandle = Addressables.LoadAssetAsync<GameObject>(TilePrefabAddress);
            _tileLibraryHandle = Addressables.LoadAssetAsync<TileTypeLibrary>(TileLibraryAddress);

            await UniTask.WhenAll(
                _tilePrefabHandle.ToUniTask(),
                _tileLibraryHandle.ToUniTask());

            if (_tilePrefabHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressablesLoader] Failed to load asset at address '{TilePrefabAddress}'.");
                return;
            }

            if (_tileLibraryHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressablesLoader] Failed to load asset at address '{TileLibraryAddress}'.");
                return;
            }

            TilePrefab = _tilePrefabHandle.Result;
            TileLibrary = _tileLibraryHandle.Result;
        }

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------

        private void OnDestroy()
        {
            if (_tilePrefabHandle.IsValid())
            {
                Addressables.Release(_tilePrefabHandle);
            }

            if (_tileLibraryHandle.IsValid())
            {
                Addressables.Release(_tileLibraryHandle);
            }
        }
    }
}
