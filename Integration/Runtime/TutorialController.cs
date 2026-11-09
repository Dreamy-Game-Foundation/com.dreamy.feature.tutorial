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
        private sealed class ViewBinding { public ITutorialView View; }
        public void Initialize(ITutorialService tutorialService, TutorialOverlay view, TutorialTargetRegistry targets)
            => Initialize(tutorialService, (ITutorialView)view, targets);
        public void Initialize(ITutorialService tutorialService, ITutorialView view, ITutorialTargetResolver targets,
            TutorialWaitingOptions waiting = null)
        {
            Validate(tutorialService, view, targets);
            var factory = new PanelPresenterFactory();
            factory.Register<ViewBinding>(v => new TutorialPresenter(tutorialService, v.View, targets, waiting));
            Bind(factory, tutorialService, new ViewBinding { View = view });
        }
        public void Initialize(PanelPresenterFactory factory, ITutorialService tutorialService,
            TutorialOverlay view, TutorialTargetRegistry targets)
            => Initialize(factory, tutorialService, (ITutorialView)view, targets);
        // Register the concrete view type in the supplied factory at the composition root.
        public void Initialize(PanelPresenterFactory factory, ITutorialService tutorialService,
            ITutorialView view, ITutorialTargetResolver targets)
        {
            Validate(tutorialService, view, targets);
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            Bind(factory, tutorialService, view);
        }
        private static void Validate(ITutorialService tutorialService, ITutorialView view, ITutorialTargetResolver targets)
        {
            if (tutorialService == null) throw new ArgumentNullException(nameof(tutorialService));
            if (view == null || view is UnityEngine.Object obj && obj == null) throw new ArgumentNullException(nameof(view));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
        }
        private void Bind(PanelPresenterFactory factory, ITutorialService tutorialService, object view)
        {
            host?.Dispose();
            if (service != null && !ReferenceEquals(service, tutorialService)) service.Suspend();
            service = tutorialService;
            host = new PanelPresenterHost(factory, view);
            if (isActiveAndEnabled) Open();
        }
        private void Open()
        {
            host?.Show();
            if (host?.Presenter is not TutorialPresenter)
            {
                host?.Dispose();
                throw new InvalidOperationException("Register the concrete tutorial view type with a TutorialPresenter in the factory.");
            }
            service.Resume();
            host.Tick();
        }
        private void OnEnable() { if (service != null) Open(); }
        private void LateUpdate() => host?.Tick();
        public TutorialResult ReportSignal(string key, Guid token) => host?.Presenter is TutorialPresenter presenter
            ? presenter.ReportSignal(key, token) : TutorialResult.NotActive;
        private void OnDisable() { host?.Dispose(); service?.Suspend(); }
        private void OnApplicationPause(bool paused)
        {
            if (service == null || !isActiveAndEnabled) return;
            if (paused) { host?.Dispose(); service.Suspend(); }
            else Open();
        }
        private void OnDestroy() { host?.Dispose(); host = null; }
    }
}
