using Orleans.Runtime;
using System;
using System.Runtime.Serialization;

namespace Troolio.Core.Projection.Exceptions
{
    public class EntityDoesNotExistException : OrleansException
    {
        public EntityDoesNotExistException() : base("Entity does not exist.") { }

        public EntityDoesNotExistException(string message) : base(message) { }

        public EntityDoesNotExistException(string message, Exception innerException) : base(message, innerException) { }

        protected EntityDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
