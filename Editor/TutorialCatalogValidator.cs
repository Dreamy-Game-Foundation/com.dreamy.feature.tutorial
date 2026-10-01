using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
namespace Dreamy.Tutorial.Editor
{
    public static class TutorialCatalogValidator
    {
        [MenuItem("Dreamy/Tutorial/Validate Selected Catalog")]
        public static void ValidateSelected()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select a tutorial catalog JSON asset.");
            ValidateJson(File.ReadAllText(path)); Debug.Log($"Tutorial catalog validated: {path}");
        }
        public static void ValidateJson(string json)
        {
            var catalog = JsonConvert.DeserializeObject<TutorialCatalogConfig>(json);
            if (catalog == null) throw new InvalidOperationException("Tutorial catalog cannot be null.");
            catalog.Initialize("tutorialCatalog");
        }
    }
}
