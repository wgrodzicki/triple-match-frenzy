using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TripleMatchFrenzy.Core;
using Random = System.Random;

namespace TripleMatchFrenzy.Generation
{
    /// <summary>
    /// Generates the complete <see cref="TileData"/> population for the game board.
    /// Pure data class with no Unity lifecycle; call once at startup, then pass the
    /// result to <c>OcclusionGraph.Build</c> before spawning any GameObjects.
    /// </summary>
    public class GridGenerator
    {
        // even / odd / even column counts produce the half-tile stagger between layers.
        private static readonly int[] _colCounts = { 6, 5, 6 };

        /// <summary>
        /// Generates a complete list of <see cref="TileData"/> for all three layers.
        /// Each tile has <see cref="TileData.Type"/>, <see cref="TileData.LayerIndex"/>,
        /// and <see cref="TileData.GridPosition"/> set. <see cref="TileData.Blockers"/>
        /// are left empty and must be populated by <c>OcclusionGraph.Build</c> afterwards.
        /// </summary>
        /// <param name="boardCenter">World-space centre of the board.</param>
        /// <param name="tileSize">World-space side length of one tile cell.</param>
        /// <param name="rowCount">Number of rows per layer (max 6).</param>
        /// <param name="rng">
        /// Seeded <see cref="Random"/> instance used for all random choices,
        /// enabling reproducible board layouts from a given seed.
        /// </param>
        /// <returns>All tiles across all layers in layer → row → column order.</returns>
        public List<TileData> Generate(Vector2 boardCenter, float tileSize, int rowCount, Random rng)
        {
            var shapes = new HashSet<Vector2Int>[_colCounts.Length];
            for (int layer = 0; layer < _colCounts.Length; layer++)
            {
                shapes[layer] = GenerateLayerShape(_colCounts[layer], rowCount, rng);
            }

            TrimToMultipleOfThree(shapes);

            int totalCells = shapes.Sum(s => s.Count);
            List<TileType> tokens = BuildShuffledTokens(totalCells, rng);

            var tiles = new List<TileData>(totalCells);
            int tokenIndex = 0;
            for (int layer = 0; layer < _colCounts.Length; layer++)
            {
                List<Vector2Int> ordered = shapes[layer]
                    .OrderBy(c => c.y)
                    .ThenBy(c => c.x)
                    .ToList();

                foreach (Vector2Int cell in ordered)
                {
                    tiles.Add(new TileData
                    {
                        Type = tokens[tokenIndex++],
                        LayerIndex = layer,
                        GridPosition = cell
                    });
                }
            }

            return tiles;
        }

        /// <summary>
        /// Converts a tile's grid address to its world-space centre position.
        /// Layer 1 is offset by half a tile on both axes relative to layers 0 and 2,
        /// creating the staggered occlusion effect.
        /// </summary>
        /// <param name="gridPos">Zero-based column and row of the tile within its layer.</param>
        /// <param name="layerIndex">Layer index of the tile (0–2).</param>
        /// <param name="boardCenter">World-space centre of the board.</param>
        /// <param name="tileSize">World-space side length of one tile cell.</param>
        /// <param name="rowCount">Row count passed to <see cref="Generate"/> for this board.</param>
        /// <returns>World-space position for the tile's visual centre.</returns>
        public static Vector2 ComputeWorldPosition(
            Vector2Int gridPos,
            int layerIndex,
            Vector2 boardCenter,
            float tileSize,
            int rowCount)
        {
            // Use the reference (even-layer) column count for centering all layers so that
            // the stagger is the sole source of X offset between layer 1 and layers 0/2.
            int refColCount = _colCounts[0];
            float stagger = (layerIndex % 2 == 1) ? tileSize * 0.5f : 0f;

            float x = boardCenter.x + (gridPos.x - (refColCount - 1) * 0.5f) * tileSize + stagger;
            float y = boardCenter.y + (gridPos.y - (rowCount - 1) * 0.5f) * tileSize + stagger;
            return new Vector2(x, y);
        }

        // For each row, picks a random number of cells on the left half and mirrors them
        // to the right. Odd-column layers always include the centre column so every row
        // stays symmetric and non-empty.
        private static HashSet<Vector2Int> GenerateLayerShape(int colCount, int rowCount, Random rng)
        {
            var cells = new HashSet<Vector2Int>();
            int halfMax = colCount / 2;
            bool hasCenter = colCount % 2 == 1;
            int centerCol = colCount / 2;

            for (int row = 0; row < rowCount; row++)
            {
                int halfCount = rng.Next(1, halfMax + 1);
                for (int i = 0; i < halfCount; i++)
                {
                    cells.Add(new Vector2Int(i, row));
                    cells.Add(new Vector2Int(colCount - 1 - i, row));
                }
                if (hasCenter)
                {
                    cells.Add(new Vector2Int(centerCol, row));
                }
            }
            return cells;
        }

        // Removes at most 2 cells (total % 3 is always 0, 1, or 2) from the outermost
        // position of the largest layer so the token pool divides evenly into triples.
        private static void TrimToMultipleOfThree(HashSet<Vector2Int>[] shapes)
        {
            int toRemove = shapes.Sum(s => s.Count) % 3;
            for (int i = 0; i < toRemove; i++)
            {
                int targetLayer = GetLargestLayerIndex(shapes);
                Vector2Int? cell = GetOutermostCell(shapes[targetLayer]);
                if (cell.HasValue)
                {
                    shapes[targetLayer].Remove(cell.Value);
                }
            }
        }

        private static int GetLargestLayerIndex(HashSet<Vector2Int>[] shapes)
        {
            int best = 0;
            for (int i = 1; i < shapes.Length; i++)
            {
                bool moreCells = shapes[i].Count > shapes[best].Count;
                bool tiedButWider = shapes[i].Count == shapes[best].Count
                    && _colCounts[i] > _colCounts[best];
                if (moreCells || tiedButWider)
                {
                    best = i;
                }
            }
            return best;
        }

        // Returns the cell with the highest row index, breaking ties by highest column index.
        private static Vector2Int? GetOutermostCell(HashSet<Vector2Int> cells)
        {
            if (cells.Count == 0)
            {
                return null;
            }
            return cells.OrderByDescending(c => c.y).ThenByDescending(c => c.x).First();
        }

        // Distributes totalCells across all TileType values in multiples of 3, spreading
        // any leftover triples across the first N types, then shuffles with Fisher-Yates.
        private static List<TileType> BuildShuffledTokens(int totalCells, Random rng)
        {
            TileType[] types = (TileType[])Enum.GetValues(typeof(TileType));
            var tokens = new List<TileType>(totalCells);

            int totalTriples = totalCells / 3;
            int baseTriples = totalTriples / types.Length;
            int extraTriples = totalTriples % types.Length;

            for (int i = 0; i < types.Length; i++)
            {
                int count = (baseTriples + (i < extraTriples ? 1 : 0)) * 3;
                for (int t = 0; t < count; t++)
                {
                    tokens.Add(types[i]);
                }
            }

            // Fisher-Yates shuffle
            for (int i = tokens.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (tokens[i], tokens[j]) = (tokens[j], tokens[i]);
            }

            return tokens;
        }
    }
}
