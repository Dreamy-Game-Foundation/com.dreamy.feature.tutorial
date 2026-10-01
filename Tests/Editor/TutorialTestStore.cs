using System;
using System.Collections.Generic;
using Dreamy.Datasave;
using Newtonsoft.Json;
namespace Dreamy.Tutorial.Tests
{
    internal sealed class TutorialTestStore : IDatasaveService
    {
        private readonly Dictionary<string, string> values = new();
        public bool Fail;
        public Action OnSave;
        public T Load<T>(string key = null) where T : SaveData, new() =>
            values.TryGetValue(key, out string json) ? JsonConvert.DeserializeObject<T>(json) : new T();
        public void Save<T>(T data, string key = null) where T : SaveData
        {
            OnSave?.Invoke();
            if (Fail) throw new InvalidOperationException("Persistence unavailable");
            values[key] = JsonConvert.SerializeObject(data);
        }
        public void SaveAll() { }
        public bool Exists(string key) => values.ContainsKey(key);
        public void Delete(string key) => values.Remove(key);
        public void DeleteAll() => values.Clear();
    }
}
