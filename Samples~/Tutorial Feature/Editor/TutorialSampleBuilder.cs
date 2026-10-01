using System;
using System.IO;
using Dreamy.Tutorial.Integration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Dreamy.Tutorial.Sample.Editor
{
    public static class TutorialSampleBuilder
    {
        private const string Output = "Assets/DreamyTutorialDemo";
        [MenuItem("Dreamy/Tutorial/Build UI and 3D Demo")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before building assets.");
            // Never overwrite existing user assets when rebuilding.
            string folder = AssetDatabase.GenerateUniqueAssetPath(Output);
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("TutorialDemo");
                var registry = root.AddComponent<TutorialTargetRegistry>();
                var controller = root.AddComponent<TutorialController>();
                var demo = root.AddComponent<TutorialDemo>();
                var cameraObject = new GameObject("TutorialCamera", typeof(Camera), typeof(PhysicsRaycaster));
                var camera = cameraObject.GetComponent<Camera>();
                camera.transform.position = new Vector3(0, 0, -8); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f, .1f, .16f);
                new GameObject("TutorialEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = "TutorialCube";
                cube.transform.position = new Vector3(0, .7f, 0);
                cube.AddComponent<TutorialWorldTarget>().Configure("demo.cube", registry, camera, cube.GetComponent<Collider>());
                var light = new GameObject("TutorialLight", typeof(Light)).GetComponent<Light>();
                light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(35, -30, 0);
                var canvas = CreateCanvas("DemoUI", 0);
                var startUI = Button(canvas.transform, "Start UI", new Vector2(110, -50));
                var start3D = Button(canvas.transform, "Start 3D", new Vector2(330, -50));
                var reset = Button(canvas.transform, "Reset tutorial save", new Vector2(570, -50));
                var action = Button(canvas.transform, "Host action", new Vector2(110, -130));
                action.gameObject.AddComponent<TutorialUITarget>().Configure("demo.action", registry, action);
                var status = Label(canvas.transform, "Status", new Vector2(0, -210), new Vector2(0, 80));
                var headerButtons = new[] { startUI, start3D, reset };
                for (int i = 0; i < headerButtons.Length; i++)
                {
                    var r = (RectTransform)headerButtons[i].transform;
                    r.anchorMin = new Vector2(.03f + .32f * i, 1); r.anchorMax = new Vector2(.31f + .32f * i, 1);
                    r.sizeDelta = new Vector2(0, 60); r.anchoredPosition = new Vector2(0, -50);
                    headerButtons[i].GetComponentInChildren<Text>().fontSize = 18;
                }
                var statusRect = (RectTransform)status.transform;
                statusRect.anchorMin = new Vector2(.05f, 1); statusRect.anchorMax = new Vector2(.95f, 1);
                var overlayObject = CreateOverlay();
                string prefabPath = folder + "/TutorialOverlay.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(overlayObject, prefabPath);
                UnityEngine.Object.DestroyImmediate(overlayObject);
                var overlay = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<TutorialOverlay>();
                var demoData = new SerializedObject(demo);
                demoData.FindProperty("catalog").objectReferenceValue = FindCatalog();
                demoData.FindProperty("controller").objectReferenceValue = controller;
                demoData.FindProperty("overlay").objectReferenceValue = overlay;
                demoData.FindProperty("registry").objectReferenceValue = registry;
                demoData.FindProperty("startUI").objectReferenceValue = startUI;
                demoData.FindProperty("start3D").objectReferenceValue = start3D;
                demoData.FindProperty("reset").objectReferenceValue = reset;
                demoData.FindProperty("action").objectReferenceValue = action;
                demoData.FindProperty("statusText").objectReferenceValue = status;
                demoData.ApplyModifiedPropertiesWithoutUndo();
                string path = folder + "/TutorialDemo.unity";
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Unable to save tutorial scene.");
                Debug.Log($"Tutorial demo saved to {path}. Open the scene and press Play.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        private static TextAsset FindCatalog()
        {
            foreach (string guid in AssetDatabase.FindAssets("tutorialCatalog t:TextAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Tutorial Feature/Config/")) return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            }
            throw new InvalidOperationException("Import the Tutorial Feature sample with its Config folder first.");
        }
        public static GameObject CreateOverlay()
        {
            var canvas = CreateCanvas("TutorialOverlay", 30000);
            var overlay = canvas.gameObject.AddComponent<TutorialOverlay>();
            var body = Rect("Content", canvas.transform); Stretch(body);
            var shade = Rect("Spotlight", body); Stretch(shade);
            var spotlight = shade.gameObject.AddComponent<TutorialSpotlight>(); spotlight.color = new Color(0, 0, 0, .65f);
            var safe = Rect("SafeArea", body); Stretch(safe);
            var panel = Rect("Tooltip", safe);
            panel.anchorMin = new Vector2(.05f, 0); panel.anchorMax = new Vector2(.95f, 0); panel.pivot = new Vector2(.5f, 0);
            panel.anchoredPosition = new Vector2(0, 24); panel.sizeDelta = new Vector2(0, 210);
            panel.gameObject.AddComponent<Image>().color = new Color(.12f, .16f, .24f);
            var text = Label(panel, "Message", Vector2.zero, new Vector2(600, 100));
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = new Vector2(.05f, .5f); textRect.anchorMax = new Vector2(.95f, .95f); textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var next = Button(panel, "Next", new Vector2(325, -155));
            var skip = Button(panel, "Skip", new Vector2(100, -155));
            var retry = Button(panel, "Retry save", new Vector2(550, -155));
            foreach (var item in new[] { skip, next, retry })
            {
                var rect = (RectTransform)item.transform;
                float x = item == skip ? .18f : item == next ? .5f : .82f;
                rect.anchorMin = rect.anchorMax = new Vector2(x, 0);
                rect.anchoredPosition = new Vector2(0, 50); rect.sizeDelta = new Vector2(150, 60);
            }
            overlay.Configure(body.gameObject, spotlight, safe, text, next, skip, retry);
            return canvas.gameObject;
        }
        private static Canvas CreateCanvas(string name, int sorting)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = sorting;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            return canvas;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static Button Button(Transform parent, string name, Vector2 position)
        {
            var rect = Rect(name, parent); rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = new Vector2(200, 60);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.2f, .4f, .65f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var text = Label(rect, name, Vector2.zero, Vector2.zero); Stretch((RectTransform)text.transform); text.raycastTarget = false;
            return button;
        }
        private static Text Label(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = Rect(name, parent); rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = rect.gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = name; text.fontSize = 24; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return text;
        }
    }
}
