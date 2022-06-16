using System;
using System.Collections.Generic;
using System.Reflection;

namespace NiftyScriptableSet
{
    public class ProviderMenuScriptableSet<TAssetBaseClass> : ProviderMenu
    {
        public new class ItemData : ProviderMenu.ItemData
        {
            public bool IsObsolete { get; }
            public string Info { get; }
            public Type ItemType { get; }
            
            public ItemData(Type itemType)
            {
                ItemType = itemType;
                DisplayName = ItemType.Name;

                if (TryGetAttribute(itemType, out ProviderMenuUnityObjectAttribute attribute))
                {
                    Info = attribute.Info;
                    _categories = attribute.Categories;
                }
                else
                {
                    Info = "Missing [ScriptableSetItemAttribute] attribute ";
                }

                if (TryGetAttribute(itemType, out ObsoleteAttribute obsoleteAttribute))
                {
                    IsObsolete = true;
                    Info = obsoleteAttribute.Message;
                }
            }
        }
        
        private Dictionary<Type, ItemData> _map;
        public ItemData DefaultItem { get; private set; }
        public bool IsMultiType => _map != null && _map.Count > 0;
        public override IEnumerable<ProviderMenu.ItemData> MenuItems => _map.Values;
        
        public ProviderMenuScriptableSet()
        {
            Type baseType = typeof(TAssetBaseClass);

            Type[] assemblyType = baseType.Assembly.GetTypes();

            if (baseType.GetConstructor(Type.EmptyTypes) != null)
            {
                baseType.GetFields();
                DefaultItem = new ItemData(baseType);
            }

            foreach (Type type in assemblyType)
            {
                if (type.IsSubclassOf(baseType) && type.IsClass && !type.IsAbstract)
                {
                    if (_map == null)
                    {
                        _map = new Dictionary<Type, ItemData>();
                    }
                    _map[type] = new ItemData(type);
                }
            }
        }
        
        private static bool TryGetAttribute<TAttributeData>(Type targetType, out TAttributeData data)
            where TAttributeData : System.Attribute
        {
            if (targetType.IsDefined(typeof(TAttributeData), true))
            {
                data = targetType.GetCustomAttribute<TAttributeData>();
                return true;
            }

            data = null;
            return false;
        }

        public ItemData GetItemData<TAsset>() where TAsset : UnityEngine.Object
        {
            Type requestedType = typeof(TAsset);
            if (_map != null && _map.ContainsKey(requestedType))
            {
                return _map[requestedType];
            }
            if (DefaultItem != null && DefaultItem.ItemType == typeof(TAsset))
            {
                return DefaultItem;
            }
            return null;
        }
    }
}