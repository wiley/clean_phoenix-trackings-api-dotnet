using System;

namespace Trackings.Domain.Exceptions
{
    [Serializable]
    public class ConflictException : Exception
    {
        public ConflictException(string message = "") : base(message) { }
    }
}
