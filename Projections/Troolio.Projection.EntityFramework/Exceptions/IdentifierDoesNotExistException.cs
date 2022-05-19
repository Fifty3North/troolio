using Orleans.Runtime;
using System;
using System.Runtime.Serialization;

namespace Troolio.Core.Projection.Exceptions
{
    public class IdentifierDoesNotExistException : OrleansException
    {
        public IdentifierDoesNotExistException() : base("Identifier does not exist.") { }

        public IdentifierDoesNotExistException(string message) : base(message) { }

        public IdentifierDoesNotExistException(string message, Exception innerException) : base(message, innerException) { }

        protected IdentifierDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
