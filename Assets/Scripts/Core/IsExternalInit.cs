// Polyfill for C# 9 records under Unity 6 / .NET Standard 2.1.
// Record positional members + init-only setters reference this type, but it
// only exists in .NET 5+. Declaring it as internal satisfies the compiler.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
