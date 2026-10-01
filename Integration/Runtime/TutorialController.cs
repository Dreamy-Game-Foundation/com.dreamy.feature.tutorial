using System;
using UnityEngine;
namespace Dreamy.Tutorial.Integration
{
    public sealed class TutorialController : MonoBehaviour
    {
        [SerializeField] private TutorialOverlay overlay;
        [SerializeField] private TutorialTargetRegistry registry;
        private TutorialPresenter presenter;
        private ITutorialService service;
        public ITutorialService Service => service;
        public void Initialize(ITutorialService tutorialService, TutorialOverlay view, TutorialTargetRegistry targets)
        {
            presenter?.Dispose(); service = tutorialService ?? throw new ArgumentNullException(nameof(tutorialService));
            overlay = view; registry = targets;
            if (isActiveAndEnabled) Bind();
        }
        private void OnEnable() { if (service != null) { service.Resume(); Bind(); } }
        private void Bind()
        {
            presenter?.Dispose(); presenter = new TutorialPresenter(service, overlay, registry); presenter.Show();
        }
        private void LateUpdate() => presenter?.Tick();
        public TutorialResult ReportSignal(string signalKey, Guid capturedStepToken) => presenter != null
            ? presenter.ReportSignal(signalKey, capturedStepToken) : TutorialResult.NotActive;
        private void OnDisable() { presenter?.Dispose(); presenter = null; service?.Suspend(); }
        private void OnDestroy() { presenter?.Dispose(); presenter = null; }
    }
}
