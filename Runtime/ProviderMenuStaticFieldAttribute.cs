using System;

namespace NiftyScriptableSet
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ProviderMenuStaticFieldAttribute : Attribute
    {
        public readonly string Name;
        public readonly string[] Categories;

        public ProviderMenuStaticFieldAttribute(string name, string[] categories)
        {
            Name = name;
            Categories = categories;
        }
    }
}