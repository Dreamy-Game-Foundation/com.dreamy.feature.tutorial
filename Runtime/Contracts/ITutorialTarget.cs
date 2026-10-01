using System;
namespace Dreamy.Tutorial
{
    public interface ITutorialTarget
    {
        string Id { get; }
        bool IsAvailable { get; }
        event Action Clicked;
    }
}
