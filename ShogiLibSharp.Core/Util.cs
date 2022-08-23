using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ShogiLibSharp.Core
{
    internal static class Util
    {
        // https://stackoverflow.com/questions/66939092/how-to-get-rid-of-bounds-check
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T FastAccessValue<T>(T[] ar, int index)
        {
            ref T tableRef = ref MemoryMarshal.GetArrayDataReference(ar);
            return Unsafe.Add(ref tableRef, (nint)index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T FastAccessRef<T>(T[] ar, int index)
        {
            ref T tableRef = ref MemoryMarshal.GetArrayDataReference(ar);
            return ref Unsafe.Add(ref tableRef, (nint)index);
        }
    }
}

