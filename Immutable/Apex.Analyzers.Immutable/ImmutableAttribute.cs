namespace System
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class ImmutableAttribute : Attribute
    {
    }
}
