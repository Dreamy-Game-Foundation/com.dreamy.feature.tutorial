using System;
using Dreamy.UI;
using UnityEngine;
namespace Dreamy.Tutorial.Integration
{
    public sealed class TutorialController : MonoBehaviour
    {
        [SerializeField] private TutorialOverlay overlay;
        [SerializeField] private TutorialTargetRegistry registry;
        private PanelPresenterHost host;
        private ITutorialService service;
        public ITutorialService Service => service;
        public void Initialize(ITutorialService tutorialService, TutorialOverlay view, TutorialTargetRegistry targets)
        {
            var factory = new PanelPresenterFactory();
            factory.Register<TutorialOverlay>(v => new TutorialPresenter(tutorialService, v, targets));
            Initialize(factory, tutorialService, view, targets);
        }
        public void Initialize(PanelPresenterFactory factory, ITutorialService tutorialService,
            TutorialOverlay view, TutorialTargetRegistry targets)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            host?.Dispose();
            service = tutorialService ?? throw new ArgumentNullException(nameof(tutorialService));
            overlay = view; registry = targets;
            host = new PanelPresenterHost(factory, view);
            if (isActiveAndEnabled) { service.Resume(); host.Show(); }
        }
        private void OnEnable() { if (service != null) { service.Resume(); host?.Show(); } }
        private void LateUpdate() => host?.Tick();
        public TutorialResult ReportSignal(string key, Guid token) => host?.Presenter is TutorialPresenter presenter
            ? presenter.ReportSignal(key, token) : TutorialResult.NotActive;
        private void OnDisable() { host?.Dispose(); service?.Suspend(); }
        private void OnDestroy() { host?.Dispose(); host = null; }
    }
}
