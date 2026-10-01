using System;
using Dreamy.Core;
using Dreamy.DataConfig;
using Dreamy.Datasave;
namespace Dreamy.Tutorial
{
    public static class TutorialInstaller
    {
        public static void RegisterConfig(IDataConfigService config) =>
            (config ?? throw new ArgumentNullException(nameof(config))).Register<TutorialCatalogConfig>("tutorialCatalog");
        public static ITutorialService Install(string saveKey = "tutorials") => Install(
            ServiceLocator.Get<IDataConfigService>().GetTable<TutorialCatalogConfig>(), ServiceLocator.Get<IDatasaveService>(), saveKey);
        public static ITutorialService Install(TutorialCatalogConfig catalog, IDatasaveService save, string saveKey = "tutorials")
        {
            var service = new TutorialModel(catalog, save, saveKey);
            ServiceLocator.Register<ITutorialService>(service);
            return service;
        }
    }
}
