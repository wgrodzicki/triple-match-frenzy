using DG.Tweening;
using TMPro;
using UnityEngine;

namespace TripleMatchFrenzy.UI
{
    /// <summary>
    /// Displays the end-game overlay for win and lose states.
    /// Attach to a GameObject that acts as the overlay root's controller.
    /// Has no knowledge of game logic — purely presentation.
    /// </summary>
    public class EndGameUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _messageText;

        [SerializeField]
        private GameObject _overlayRoot;

        private CanvasGroup _canvasGroup;

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        /// <summary>Shows the win message overlay.</summary>
        public void ShowWin()
        {
            _messageText.text = "You Win!";
            Show();
        }

        /// <summary>Shows the lose message overlay.</summary>
        public void ShowLose()
        {
            _messageText.text = "You Lose!";
            Show();
        }

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (_overlayRoot == null)
            {
                Debug.LogError("[EndGameUI] _overlayRoot is not assigned.", this);
                return;
            }

            _canvasGroup = _overlayRoot.GetComponent<CanvasGroup>();
            _overlayRoot.SetActive(false);
        }

        // -----------------------------------------------------------------------
        // Internals
        // -----------------------------------------------------------------------

        private void Show()
        {
            _overlayRoot.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 1f, 0.4f)
                    .SetEase(Ease.OutCubic);
            }
        }
    }
}
