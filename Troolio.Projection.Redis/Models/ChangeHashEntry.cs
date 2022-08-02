namespace Troolio.Projection.Redis.Models
{
    public class ChangeHashEntry
    {
        /// <summary>
        /// The Key of the ChangeHash entry, e.g. the unique ChangeId Guid.
        /// </summary>
        public readonly string ChangeId;

        /// <summary>
        /// The Entity Key of the ChangeHash entry, e.g. PostActor:12300000-0000-0000-0000-000000000000
        /// </summary>
        public readonly string EntityKey;

        public ChangeHashEntry(string changeId, string entityKey)
        {
            ChangeId = changeId;
            EntityKey = entityKey;
        }
    }
}
