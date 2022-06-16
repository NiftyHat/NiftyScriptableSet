using System;

namespace NiftyScriptableSet
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ProviderMenuUnityObjectAttribute : Attribute
    {
        public string Icon;
        public string Info;
        public string[] Categories;
        
        public ProviderMenuUnityObjectAttribute()
        {
            
        }
    }
}