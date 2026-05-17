using System.Collections.Generic;
using UnityEngine;

namespace TripleMatchFrenzy.Tray
{
    /// <summary>
    /// Object pool for the UI Image clones used by <see cref="Tray"/>.
    /// Pre-allocates 21 instances (three full matches worth) and expands on demand.
    /// Must be initialised via <see cref="Setup"/> before any <see cref="Get"/> calls.
    /// </summary>
    public class TrayClonePool : MonoBehaviour
    {
        private const int InitialSize = 21;

        private GameObject _prefab;
        private readonly List<GameObject> _pool = new();

        /// <summary>
        /// Stores the clone prefab and fills the pool to <see cref="InitialSize"/>.
        /// Called by <see cref="Tray"/> in Awake before any tile interactions occur.
        /// </summary>
        public void Setup(GameObject prefab)
        {
            _prefab = prefab;
            for (int i = 0; i < InitialSize; i++)
            {
                CreateInstance();
            }
        }

        /// <summary>Returns an active clone from the pool, expanding if needed.</summary>
        public GameObject Get(Transform parent)
        {
            GameObject clone = _pool.Find(go => !go.activeInHierarchy);
            if (clone == null)
            {
                clone = CreateInstance();
            }

            clone.transform.SetParent(parent, false);
            clone.SetActive(true);
            return clone;
        }

        /// <summary>Returns a clone to the pool (deactivates and reparents to this transform).</summary>
        public void Return(GameObject clone)
        {
            clone.SetActive(false);
            clone.transform.SetParent(transform, false);
        }

        private GameObject CreateInstance()
        {
            GameObject go = Instantiate(_prefab, transform);
            go.SetActive(false);
            _pool.Add(go);
            return go;
        }
    }
}
