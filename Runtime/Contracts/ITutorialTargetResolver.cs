namespace Dreamy.Tutorial
{
    public interface ITutorialTargetResolver
    {
        bool TryResolve(string targetId, out ITutorialTarget target);
    }
}
