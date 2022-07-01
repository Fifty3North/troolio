using Orleans.Runtime;
using System.Runtime.Serialization;

namespace Troolio.Core.Projection.Exceptions
{
    public class PrimaryKeyNotFoundException : OrleansException
    {
        public PrimaryKeyNotFoundException() : base("Primary Key not found for entity.") { }

        public PrimaryKeyNotFoundException(string message) : base(message) { }

        public PrimaryKeyNotFoundException(string message, Exception innerException) : base(message, innerException) { }

        protected PrimaryKeyNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
