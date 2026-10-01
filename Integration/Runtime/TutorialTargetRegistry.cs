using System;
using System.Collections.Generic;
using UnityEngine;
namespace Dreamy.Tutorial.Integration
{
    public sealed class TutorialTargetRegistry : MonoBehaviour, ITutorialTargetResolver
    {
        private readonly Dictionary<string, ITutorialTarget> targets = new(StringComparer.Ordinal);
        public bool Register(ITutorialTarget target)
        {
            if (target == null || string.IsNullOrWhiteSpace(target.Id)) return false;
            if (targets.TryGetValue(target.Id, out var existing) && !ReferenceEquals(existing, target))
            {
                Debug.LogError($"Duplicate tutorial target '{target.Id}' in registry '{name}'.", this);
                return false;
            }
            targets[target.Id] = target; return true;
        }
        public void Unregister(ITutorialTarget target)
        {
            if (target != null && targets.TryGetValue(target.Id, out var existing) && ReferenceEquals(existing, target))
                targets.Remove(target.Id);
        }
        public bool TryResolve(string targetId, out ITutorialTarget target)
        {
            target = null;
            return !string.IsNullOrWhiteSpace(targetId) && targets.TryGetValue(targetId, out target);
        }
    }
}
