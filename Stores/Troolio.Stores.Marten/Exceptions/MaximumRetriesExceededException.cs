using System.Runtime.Serialization;

namespace Troolio.Stores.Exceptions
{
    public class MaximumRetriesExceededException : Exception
    {
        public MaximumRetriesExceededException() : base("Maximum retries exceeded") { }

        public MaximumRetriesExceededException(string message) : base(message) { }

        public MaximumRetriesExceededException(string message, Exception? innerException) : base(message, innerException) { }

        protected MaximumRetriesExceededException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
