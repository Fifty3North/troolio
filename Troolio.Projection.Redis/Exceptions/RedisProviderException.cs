using System.Runtime.Serialization;

namespace Troolio.Projection.Redis.Exceptions
{
    [Serializable]
    public class RedisProviderException : Exception
    {
        public RedisProviderException() : base("RedisProvider error encountered.")
        {
        }

        public RedisProviderException(string message) : base(message)
        {
        }

        public RedisProviderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RedisProviderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
