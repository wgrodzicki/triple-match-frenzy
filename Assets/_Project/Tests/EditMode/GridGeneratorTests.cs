using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using TripleMatchFrenzy.Core;
using TripleMatchFrenzy.Generation;

namespace TripleMatchFrenzy.Tests
{
    public class GridGeneratorTests
    {
        private GridGenerator _generator;

        private const float TileSize = 1f;
        private const int RowCount = 6;
        private static readonly Vector2 BoardCenter = Vector2.zero;

        [SetUp]
        public void SetUp()
        {
            _generator = new GridGenerator();
        }

        /// <summary>The total number of tiles across all layers must be divisible by 3.</summary>
        [Test]
        public void Generate_TotalTileCount_IsDivisibleByThree()
        {
            List<TileData> tiles = _generator.Generate(BoardCenter, TileSize, RowCount, new System.Random(42));

            Assert.That(tiles.Count % 3, Is.EqualTo(0),
                $"Total tile count {tiles.Count} must be divisible by 3.");
        }

        /// <summary>Every TileType that appears on the board must appear a multiple of 3 times.</summary>
        [Test]
        public void Generate_EachTileType_AppearsMultipleOfThreeTimes()
        {
            List<TileData> tiles = _generator.Generate(BoardCenter, TileSize, RowCount, new System.Random(42));

            var countsByType = tiles
                .GroupBy(t => t.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (TileType type in System.Enum.GetValues(typeof(TileType)))
            {
                if (!countsByType.TryGetValue(type, out int count))
                {
                    continue;
                }
                Assert.That(count % 3, Is.EqualTo(0),
                    $"TileType.{type} appears {count} times — must be a multiple of 3.");
            }
        }

        /// <summary>
        /// For the same grid position, a layer 1 tile's world position must be exactly
        /// (+tileSize * 0.5, +tileSize * 0.5) relative to a layer 0 tile at that position.
        /// </summary>
        [Test]
        public void ComputeWorldPosition_Layer1_IsOffsetByHalfTileOnBothAxes()
        {
            List<TileData> tiles = _generator.Generate(BoardCenter, TileSize, RowCount, new System.Random(42));

            var layer1Tiles = tiles.Where(t => t.LayerIndex == 1).ToList();
            Assert.That(layer1Tiles, Is.Not.Empty, "Layer 1 must contain at least one tile.");

            float expectedOffset = TileSize * 0.5f;

            foreach (TileData tile in layer1Tiles)
            {
                Vector2 layer1Pos = GridGenerator.ComputeWorldPosition(
                    tile.GridPosition, 1, BoardCenter, TileSize, RowCount);
                Vector2 layer0Pos = GridGenerator.ComputeWorldPosition(
                    tile.GridPosition, 0, BoardCenter, TileSize, RowCount);

                Assert.That(layer1Pos.x - layer0Pos.x, Is.EqualTo(expectedOffset).Within(0.001f),
                    $"At grid {tile.GridPosition}: layer 1 X offset from layer 0 must be {expectedOffset}.");
                Assert.That(layer1Pos.y - layer0Pos.y, Is.EqualTo(expectedOffset).Within(0.001f),
                    $"At grid {tile.GridPosition}: layer 1 Y offset from layer 0 must be {expectedOffset}.");
            }
        }
    }
}
