using System;
using System.Collections.Generic;
using System.Text;

namespace J2N.Collections.Generic
{
    public static class TypeExtensions
    {
        internal static bool IsNullableType(this Type type)
        {
            // Abort if no type supplied
            if (type == null)
                return false;

            // If this is not a value type, it is a reference type, so it is automatically nullable
            //  (NOTE: All forms of Nullable<T> are value types)
            if (!type.IsValueType)
                return true;

            // Report whether type is a form of the Nullable<> type
            return Nullable.GetUnderlyingType(type) != null;
        }
    }
}
