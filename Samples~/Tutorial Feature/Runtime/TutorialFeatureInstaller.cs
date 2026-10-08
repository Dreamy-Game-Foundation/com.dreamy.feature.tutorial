using System;
using Dreamy.DataConfig;
using Dreamy.Datasave;
using Dreamy.Tutorial.Integration;
using Dreamy.Tutorial;
using Dreamy.UI;

namespace Dreamy.Feature.Tutorial.Integration
{
    public static class TutorialFeatureInstaller
    {
        public static void RegisterConfig(IDataConfigService config) => TutorialInstaller.RegisterConfig(config);
        public static ITutorialService Install(PanelPresenterFactory factory, TutorialCatalogConfig config,
            IDatasaveService save, Func<TutorialOverlay, ITutorialTargetResolver> resolver, string saveKey = "tutorials")
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            return Install(factory, TutorialInstaller.Install(config, save, saveKey), resolver);
        }
        public static ITutorialService Install(PanelPresenterFactory factory, ITutorialService service,
            Func<TutorialOverlay, ITutorialTargetResolver> resolver)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            factory.Register<TutorialOverlay>(view => new TutorialPresenter(service, view,
                resolver(view) ?? throw new InvalidOperationException("Tutorial view requires a target resolver.")));
            return service;
        }
    }
}
