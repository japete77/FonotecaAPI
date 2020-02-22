using Microsoft.AspNetCore.Http;
using System;
using System.Runtime.Serialization;

namespace Core.Exceptions
{
    [Serializable]
    public class DuplicateKeyException : ExceptionBase
    {
        public DuplicateKeyException()
            : this(ExceptionCodes.COLLECTION_DUPLICATED_KEY, "Duplicated key")
        {
        }

        public DuplicateKeyException(int code, string message, Exception innerException = null, int httpCode = StatusCodes.Status400BadRequest) : base(code, message, innerException, httpCode)
        {
        }

        protected DuplicateKeyException(int code, SerializationInfo info, StreamingContext context) : base(code, info, context)
        {
        }
    }
}