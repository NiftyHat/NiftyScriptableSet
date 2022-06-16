using System;
using System.Collections.Generic;
using System.Reflection;

namespace NiftyScriptableSet
{
    public class ProviderMenuDataTemplate<TTemplateClass> : ProviderMenu
    {
        public new class ItemData : ProviderMenu.ItemData
        {
            public TTemplateClass Instance { get; }
            
            public ItemData(TTemplateClass instance, string displayName, string[] categories = null)
            {
                Instance = instance;
                DisplayName = displayName;
                _categories = categories;
            }
        }
        
        public ProviderMenuDataTemplate()
        {
            Type baseType = typeof(TTemplateClass);

            FieldInfo[] fieldInfoList = baseType.GetFields(BindingFlags.Static);
            List<ItemData> items = new List<ItemData>();
            foreach (FieldInfo fieldInfo in fieldInfoList)
            {
                if (TryGetAttribute<ProviderMenuStaticFieldAttribute>(fieldInfo, out var attribute))
                {
                    TTemplateClass val = (TTemplateClass)fieldInfo.GetValue(null);
                    if (val != null)
                    {
                        items.Add(new ItemData(val, attribute.Name ?? fieldInfo.Name, attribute.Categories));
                    }
                    MenuItems = items;
                }
            }
        }
        
        public override IEnumerable<ProviderMenu.ItemData> MenuItems { get; }

        private static bool TryGetAttribute<TAttributeData>(FieldInfo fieldInfo, out TAttributeData data)
            where TAttributeData : System.Attribute
        {
            if (fieldInfo.IsDefined(typeof(TAttributeData), true))
            {
                data = fieldInfo.GetCustomAttribute<TAttributeData>();
                return true;
            }

            data = null;
            return false;
        }
    }
}