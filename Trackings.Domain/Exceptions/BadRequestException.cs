using System;

namespace Trackings.Domain.Exceptions
{
    [Serializable]
    public class BadRequestException : Exception
    {
        public BadRequestException() { }
    }
}
