namespace Troolio.Core.Projection
{
    internal class EfJob
    {
        public readonly Func<Task> Action;
        public int TryCount { get; private set; }

        public EfJob(Func<Task> action)
        {
            Action = action;
        }

        public void IncrementTryCount() => TryCount++;
    }
}
