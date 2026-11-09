using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Dreamy.Tutorial.Integration
{
    public sealed class TutorialOverlay : MonoBehaviour, ITutorialFocusView, ITutorialWaitingView
    {
        [SerializeField] private GameObject content;
        [SerializeField] private TutorialSpotlight spotlight;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Text message;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button recoveryButton;
        private bool hasError;
        public string WaitingMessageKey { get; set; } = "tutorial.waiting";
        public string WaitingTimeoutMessageKey { get; set; } = "tutorial.waiting.timeout";
        public event Action RecoveryRequested;
        public void ConfigureRecovery(Button button)
        {
            if (recoveryButton != null) recoveryButton.onClick.RemoveListener(OnRecovery);
            recoveryButton = button;
            if (isActiveAndEnabled && recoveryButton != null) recoveryButton.onClick.AddListener(OnRecovery);
        }
        private void OnRecovery() => RecoveryRequested?.Invoke();
        public void RenderWaiting(TutorialState state, bool timedOut)
        {
            if (!hasError && message != null)
            {
                string key = timedOut ? WaitingTimeoutMessageKey : WaitingMessageKey;
                message.text = ResolveMessage?.Invoke(key) ?? key;
            }
            if (recoveryButton != null) recoveryButton.gameObject.SetActive(timedOut && !hasError);
        }
        public void RenderTargets(TutorialState state, ITutorialTarget target,
            IReadOnlyList<ITutorialTarget> focusTargets, IReadOnlyList<ITutorialTarget> inputTargets)
        {
            RenderContent(state);
            if (spotlight != null)
            {
                bool active = state.Status == TutorialStatus.ActiveStep;
                spotlight.SetTargets(active ? focusTargets : null, active ? inputTargets : null,
                    active && state.Step.BlockOutsideTarget);
            }
        }
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
            if (recoveryButton != null) recoveryButton.onClick.AddListener(OnRecovery);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkip);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        }
        private void Unbind()
        {
            if (recoveryButton != null) recoveryButton.onClick.RemoveListener(OnRecovery);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
            if (skipButton != null) skipButton.onClick.RemoveListener(OnSkip);
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
        }
        private void OnNext() => NextRequested?.Invoke();
        private void OnSkip() => SkipRequested?.Invoke();
        private void OnRetry() => RetryRequested?.Invoke();
        public void Render(TutorialState state, ITutorialTarget target)
        {
            RenderContent(state);
            bool active = state.Status == TutorialStatus.ActiveStep;
            if (spotlight != null) spotlight.SetTarget(active ? target as ITutorialScreenTarget : null, active && state.Step.BlockOutsideTarget);
        }
        private void RenderContent(TutorialState state)
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
            if (recoveryButton != null && state.Status != TutorialStatus.WaitingForTarget) recoveryButton.gameObject.SetActive(false);
            if (string.IsNullOrEmpty(state.Error)) hasError = false;
            if (!hasError && message != null)
                message.text = active ? (ResolveMessage?.Invoke(state.Step.MessageKey) ?? state.Step.MessageKey) : (ResolveMessage?.Invoke(WaitingMessageKey) ?? WaitingMessageKey);
            if (nextButton != null) nextButton.gameObject.SetActive(active && state.Step.CompletionMode == TutorialCompletionMode.Next && !hasError);
            if (skipButton != null) skipButton.gameObject.SetActive(state.AllowSkip);
            if (retryButton != null) retryButton.gameObject.SetActive(hasError);
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
            if (recoveryButton != null) recoveryButton.gameObject.SetActive(false);
            hasError = false;
        }
    }
}
