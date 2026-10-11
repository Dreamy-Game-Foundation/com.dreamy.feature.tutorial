using System.Collections.Generic;
using Dreamy.Datasave;

namespace Dreamy.Tutorial.Sample
{
    /// <summary>Isolated session storage for repeatable walkthrough demos. Never touches player files.</summary>
    public sealed class TutorialDemoMemorySave : IDatasaveService
    {
        private readonly Dictionary<string, SaveData> saves = new();
        public T Load<T>(string key = null) where T : SaveData, new()
        {
            key ??= typeof(T).FullName;
            if (!saves.TryGetValue(key, out var data)) saves[key] = data = new T();
            return (T)data;
        }
        public void Save<T>(T data, string key = null) where T : SaveData => saves[key ?? typeof(T).FullName] = data;
        public void SaveAll() { }
        public bool Exists(string key) => saves.ContainsKey(key);
        public void Delete(string key) => saves.Remove(key);
        public void DeleteAll() => saves.Clear();
    }
}
