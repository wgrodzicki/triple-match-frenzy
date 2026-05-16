using System;
using UnityEngine;

namespace TripleMatchFrenzy.Data
{
    /// <summary>Pairs a tile type with the sprite used to render it.</summary>
    [Serializable]
    public class TileTypeEntry
    {
        /// <summary>The tile type this entry represents.</summary>
        public TileType Type;

        /// <summary>The icon sprite displayed for this tile type.</summary>
        public Sprite Icon;
    }

    /// <summary>
    /// Project-wide ScriptableObject that maps every <see cref="TileType"/> to its icon sprite.
    /// Create an instance via <c>Assets → Create → Triple Match Frenzy → Tile Type Library</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "TileTypeLibrary", menuName = "Triple Match Frenzy/Tile Type Library")]
    public class TileTypeLibrary : ScriptableObject
    {
        [SerializeField]
        private TileTypeEntry[] _entries = Array.Empty<TileTypeEntry>();

        /// <summary>Returns the icon sprite for the given <paramref name="type"/>.</summary>
        /// <param name="type">The tile type to look up.</param>
        /// <returns>The matching <see cref="Sprite"/>, or <c>null</c> if the type is not registered.</returns>
        public Sprite GetSprite(TileType type)
        {
            foreach (TileTypeEntry entry in _entries)
            {
                if (entry.Type == type)
                {
                    return entry.Icon;
                }
            }
            return null;
        }
    }
}
