using System;
using UnityEngine;
using UnityEngine.UI;
namespace Dreamy.Tutorial.Integration
{
    public sealed class TutorialOverlay : MonoBehaviour, ITutorialView
    {
        [SerializeField] private GameObject content;
        [SerializeField] private TutorialSpotlight spotlight;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Text message;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button retryButton;
        private bool hasError;
        public Func<string, string> ResolveMessage { get; set; }
        public event Action NextRequested;
        public event Action SkipRequested;
        public event Action RetryRequested;
        public void Configure(GameObject body, TutorialSpotlight shade, RectTransform safe, Text label, Button next, Button skip, Button retry)
        {
            Unbind(); content = body; spotlight = shade; safeArea = safe; message = label;
            nextButton = next; skipButton = skip; retryButton = retry; if (isActiveAndEnabled) Bind();
        }
        private void OnEnable() => Bind();
        private void OnDisable() { Unbind(); Hide(); }
        private void Bind()
        {
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkip);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        }
        private void Unbind()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
            if (skipButton != null) skipButton.onClick.RemoveListener(OnSkip);
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
        }
        private void OnNext() => NextRequested?.Invoke();
        private void OnSkip() => SkipRequested?.Invoke();
        private void OnRetry() => RetryRequested?.Invoke();
        public void Render(TutorialState state, ITutorialTarget target)
        {
            if (content == null) return;
            content.SetActive(true);
            Rect safe = Screen.safeArea;
            if (safeArea != null && Screen.width > 0 && Screen.height > 0)
            {
                safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
                safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            }
            bool active = state.Status == TutorialStatus.ActiveStep;
            if (string.IsNullOrEmpty(state.Error)) hasError = false;
            if (!hasError && message != null)
                message.text = active ? (ResolveMessage?.Invoke(state.Step.MessageKey) ?? state.Step.MessageKey) : "Waiting for tutorial target…";
            if (nextButton != null) nextButton.gameObject.SetActive(active && state.Step.CompletionMode == TutorialCompletionMode.Next && !hasError);
            if (skipButton != null) skipButton.gameObject.SetActive(state.AllowSkip);
            if (retryButton != null) retryButton.gameObject.SetActive(hasError);
            if (spotlight != null) spotlight.SetTarget(active ? target as ITutorialScreenTarget : null, active && state.Step.BlockOutsideTarget);
        }
        public void ShowError(string value)
        {
            hasError = true;
            if (message != null) message.text = value;
            if (retryButton != null) retryButton.gameObject.SetActive(true);
        }
        public void Hide()
        {
            if (spotlight != null) spotlight.SetTarget(null, false);
            if (content != null) content.SetActive(false);
            hasError = false;
        }
    }
}
