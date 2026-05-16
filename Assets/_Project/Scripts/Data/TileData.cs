using System.Collections.Generic;
using UnityEngine;

namespace TripleMatchFrenzy.Data
{
    /// <summary>Pure data representation of a single tile on the board.</summary>
    public class TileData
    {
        /// <summary>The tile's visual and logical type.</summary>
        public TileType Type { get; set; }

        /// <summary>Zero-based layer index (0 = bottom layer, increasing upward).</summary>
        public int LayerIndex { get; set; }

        /// <summary>Column and row address within this layer's grid.</summary>
        public Vector2Int GridPosition { get; set; }

        /// <summary>
        /// Tiles on the layer directly above that fully or partially occlude this tile.
        /// This list is maintained by <see cref="OcclusionGraph"/>.
        /// </summary>
        public List<TileData> Blockers { get; } = new List<TileData>();

        /// <summary>
        /// True when no tiles are occluding this tile, meaning the player can select it.
        /// </summary>
        public bool IsSelectable => Blockers.Count == 0;

        /// <summary>Removes a specific tile from the <see cref="Blockers"/> list.</summary>
        /// <param name="tile">The tile to remove.</param>
        public void RemoveBlocker(TileData tile) => Blockers.Remove(tile);
    }
}
