namespace OpenXR.PInvoke {
    using System;
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue
                                               | AttributeTargets.Field,
        Inherited = false, AllowMultiple = true)]
    public sealed class NativeTypeNameAttribute : Attribute {
        public NativeTypeNameAttribute(string positionalString) {
            this.Name = positionalString;
        }

        public string Name { get; }
    }
}
