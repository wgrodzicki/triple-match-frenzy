using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TripleMatchFrenzy.Tray;
using UnityEngine;

namespace TripleMatchFrenzy.Tweening
{
    /// <summary>
    /// Owns all DOTween calls in the project.
    /// Exposes async UniTask methods so callers can await animations without coupling to tween internals.
    /// Has no knowledge of game logic, tile data, or the occlusion graph.
    /// </summary>
    public class TweenController : MonoBehaviour
    {
        [SerializeField]
        private float _tileToTrayDuration = 0.25f;

        [SerializeField]
        private float _trayCollapseDuration = 0.15f;

        [SerializeField]
        private float _matchRemoveDuration = 0.2f;

        [SerializeField]
        private Ease _tileToTrayEase = Ease.OutCubic;

        [SerializeField]
        private Ease _trayCollapseEase = Ease.OutCubic;

        [SerializeField]
        private Ease _matchRemoveEase = Ease.InCubic;

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        /// <summary>
        /// Tweens a UI clone from its current position to the target tray slot position.
        /// Awaitable — completes when the tween finishes.
        /// </summary>
        public async UniTask AnimateTileToTray(RectTransform clone, Vector2 targetPosition)
        {
            await clone
                .DOMove(new Vector3(targetPosition.x, targetPosition.y, 0f), _tileToTrayDuration)
                .SetEase(_tileToTrayEase)
                .ToUniTask();
        }

        /// <summary>
        /// Tweens all remaining tray clones to their new slot positions after a match removal.
        /// All moves run in parallel. Awaitable — completes when every tween finishes.
        /// </summary>
        public async UniTask AnimateTrayCollapse(List<(RectTransform clone, Vector2 targetPosition)> moves)
        {
            UniTask[] tasks = new UniTask[moves.Count];
            for (int i = 0; i < moves.Count; i++)
            {
                (RectTransform clone, Vector2 target) = moves[i];
                tasks[i] = clone
                    .DOMove(new Vector3(target.x, target.y, 0f), _trayCollapseDuration)
                    .SetEase(_trayCollapseEase)
                    .ToUniTask();
            }
            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// Scales matched clones down to zero, then returns them to the pool.
        /// All scale tweens run in parallel. Pool return happens after all tweens complete.
        /// Awaitable — completes when every tween finishes and clones are back in the pool.
        /// </summary>
        public async UniTask AnimateMatchRemove(List<RectTransform> clones, TrayClonePool pool)
        {
            UniTask[] tasks = new UniTask[clones.Count];
            for (int i = 0; i < clones.Count; i++)
            {
                tasks[i] = clones[i]
                    .DOScale(Vector3.zero, _matchRemoveDuration)
                    .SetEase(_matchRemoveEase)
                    .ToUniTask();
            }
            await UniTask.WhenAll(tasks);

            foreach (RectTransform clone in clones)
            {
                clone.localScale = Vector3.one;
                pool.Return(clone.gameObject);
            }
        }
    }
}
