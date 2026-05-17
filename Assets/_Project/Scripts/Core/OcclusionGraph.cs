using System.Collections.Generic;
using TripleMatchFrenzy.Data;
using UnityEngine;

namespace TripleMatchFrenzy.Core
{
    /// <summary>
    /// Builds and maintains blocker relationships between tiles on adjacent layers.
    /// A tile on layer N is blocked by any tile on layer N+1 whose footprint overlaps it,
    /// accounting for the half-tile positional stagger between even and odd layers.
    /// </summary>
    public class OcclusionGraph
    {
        // Reference to the live tile collection; updated externally as tiles are removed.
        private List<TileData> _allTiles;

        /// <summary>
        /// Populates <see cref="TileData.Blockers"/> for every tile, then retains a reference
        /// to <paramref name="allTiles"/> so that <see cref="OnTileRemoved"/> can update
        /// selectability as tiles leave the board.
        /// </summary>
        /// <param name="allTiles">
        /// The authoritative list of all tiles across all layers.
        /// The caller is responsible for removing tiles from this list when they are taken.
        /// </param>
        /// <param name="tileSize">World-space side length of a single tile cell.</param>
        public void Build(List<TileData> allTiles, float tileSize)
        {
            _allTiles = allTiles;

            // Group tiles by layer for O(n) blocker assignment instead of O(n²).
            var byLayer = new Dictionary<int, List<TileData>>();
            foreach (TileData tile in allTiles)
            {
                if (!byLayer.TryGetValue(tile.LayerIndex, out List<TileData> bucket))
                {
                    bucket = new List<TileData>();
                    byLayer[tile.LayerIndex] = bucket;
                }
                bucket.Add(tile);
            }

            foreach (TileData tile in allTiles)
            {
                if (!byLayer.TryGetValue(tile.LayerIndex + 1, out List<TileData> above))
                {
                    continue;
                }

                foreach (TileData upper in above)
                {
                    if (Overlaps(tile, upper, tileSize))
                    {
                        tile.Blockers.Add(upper);
                    }
                }
            }
        }

        /// <summary>
        /// Removes <paramref name="tile"/> from the <see cref="TileData.Blockers"/> list of
        /// every tile on the layer directly below it, potentially making those tiles selectable.
        /// Call this immediately after a tile is taken from the board.
        /// </summary>
        /// <param name="tile">The tile that was removed from the board.</param>
        public void OnTileRemoved(TileData tile)
        {
            int lowerLayer = tile.LayerIndex - 1;
            foreach (TileData other in _allTiles)
            {
                if (other.LayerIndex == lowerLayer)
                {
                    other.RemoveBlocker(tile);
                }
            }
            _allTiles.Remove(tile);
        }

        // Converts a grid address to a world-space centre, applying the half-tile stagger
        // that offsets odd-indexed layers relative to even-indexed layers.
        private static Vector2 GridToWorld(Vector2Int grid, int layer, float tileSize)
        {
            float offset = (layer % 2 == 1) ? tileSize * 0.5f : 0f;
            return new Vector2(grid.x * tileSize + offset, grid.y * tileSize + offset);
        }

        // Two tiles overlap when their world-space centres are closer than one full tile
        // on both axes (each tile extends tileSize/2 from its centre, so the overlap
        // threshold is tileSize/2 + tileSize/2 = tileSize).
        private static bool Overlaps(TileData lower, TileData upper, float tileSize)
        {
            Vector2 lowerWorld = GridToWorld(lower.GridPosition, lower.LayerIndex, tileSize);
            Vector2 upperWorld = GridToWorld(upper.GridPosition, upper.LayerIndex, tileSize);
            return Mathf.Abs(lowerWorld.x - upperWorld.x) < tileSize
                && Mathf.Abs(lowerWorld.y - upperWorld.y) < tileSize;
        }
    }
}
