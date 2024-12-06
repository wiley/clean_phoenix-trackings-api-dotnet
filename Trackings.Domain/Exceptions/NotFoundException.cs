using System;

namespace Trackings.Domain.Exceptions
{
    [Serializable]
    public class NotFoundException : Exception
    {
        public NotFoundException(string message = "") : base(message) { }
    }
}
