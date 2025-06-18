using System.Runtime.CompilerServices;

namespace VKProxy.Core.Infrastructure;

public static partial class StringHashing
{
#if NET7_0_OR_GREATER

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetNonRandomizedHashCodeOrdinalIgnoreCase")]
    public static extern int HashOrdinalIgnoreCase(this string c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOrdinalIgnoreCaseHashCode(ReadOnlySpan<char> value)
    {
        // note:
        //  - Overload that specify StringComparison is slow because of internal branching by switch statements.

        return string_GetHashCodeOrdinalIgnoreCase(self: null, value);

        #region Local Functions

        // note:
        //  - UnsafeAccessor can't be defined within a Generic type.
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetHashCodeOrdinalIgnoreCase")]
        static extern int string_GetHashCodeOrdinalIgnoreCase(string self, ReadOnlySpan<char> value);

        #endregion Local Functions
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OrdinalIgnoreCaseEquals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
        => MemoryExtensions.Equals(x, y, StringComparison.OrdinalIgnoreCase);

#endif
}