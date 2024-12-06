using System;

namespace Trackings.Domain.Utils
{
    public class ValidateObjects
    {
        public static bool IsValidEnumValue<T>(Enum value)
        {
            return Enum.IsDefined(typeof(T), value);
        }
    }
}
