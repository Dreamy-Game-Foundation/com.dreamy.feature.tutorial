using System;
using Dreamy.Datasave;
using Dreamy.Tutorial.Integration;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
namespace Dreamy.Tutorial.Sample
{
    public sealed class TutorialDemo : MonoBehaviour
    {
        [SerializeField] private TextAsset catalog;
        [SerializeField] private TutorialController controller;
        [SerializeField] private TutorialOverlay overlay;
        [SerializeField] private TutorialTargetRegistry registry;
        [SerializeField] private Button startUI;
        [SerializeField] private Button start3D;
        [SerializeField] private Button reset;
        [SerializeField] private Button action;
        [SerializeField] private Text statusText;
        private DatasaveService datasave;
        private ITutorialService service;
        private int count;
        private void Start()
        {
            datasave = new DatasaveService(new DatasaveOptions { DirectoryName = "DreamyTutorialDemo", CreateFileOnFirstLoad = false });
            Install();
            startUI.onClick.AddListener(StartUI); start3D.onClick.AddListener(Start3D);
            reset.onClick.AddListener(ResetTutorials); action.onClick.AddListener(HostAction);
        }
        private void Install()
        {
            if (service != null) service.StateChanged -= Refresh;
            service = new TutorialModel(JsonConvert.DeserializeObject<TutorialCatalogConfig>(catalog.text), datasave, "tutorial-demo");
            controller.Initialize(service, overlay, registry); service.StateChanged += Refresh; Refresh();
        }
        private void StartUI() => StartFlow("ui-demo");
        private void Start3D() => StartFlow("world-demo");
        private void StartFlow(string id)
        {
            TutorialResult result = service.TryStart(id);
            statusText.text = $"{id}: {result}";
        }
        private void HostAction()
        {
            Guid captured = service.GetState().StepToken;
            count++; // Gameplay owns the action; checkpoint retries never execute this again.
            controller.ReportSignal("demo.action", captured);
            Refresh();
        }
        private void Refresh()
        {
            var state = service.GetState();
            statusText.text = $"Actions: {count} | {state.FlowId} / {state.Step?.Id} / {state.Status}";
        }
        private void ResetTutorials() { datasave.Delete("tutorial-demo"); Install(); }
        private void OnApplicationPause(bool paused) { if (paused) datasave?.SaveAll(); }
        private void OnDestroy()
        {
            if (service != null) service.StateChanged -= Refresh;
            if (startUI != null) startUI.onClick.RemoveListener(StartUI);
            if (start3D != null) start3D.onClick.RemoveListener(Start3D);
            if (reset != null) reset.onClick.RemoveListener(ResetTutorials);
            if (action != null) action.onClick.RemoveListener(HostAction);
        }
    }
}
