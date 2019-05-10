namespace System
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class ImmutableAttribute : Attribute
    {
        public ImmutableAttribute(bool onFaith = false)
        {
            OnFaith = onFaith;
        }

        public bool OnFaith { get; }
    }
}
