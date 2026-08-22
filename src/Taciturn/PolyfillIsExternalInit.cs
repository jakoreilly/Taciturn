#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

// netstandard2.0 has no System.Runtime.CompilerServices.IsExternalInit, which the
// compiler requires to emit `init` accessors (and therefore positional records).
// This is the standard polyfill — an empty marker type the compiler only needs to
// find, never to instantiate.
internal static class IsExternalInit
{
}
#endif
