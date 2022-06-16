using System;
using System.Collections.Generic;
using NiftyEditorMenu;
using UnityEditor;
using UnityEngine;

namespace NiftyScriptableSet
{
    public abstract class ProviderMenu
    {
        public abstract class ItemData
        {
            protected string[] _categories;
            public string DisplayName { get; protected set; }
            
            public bool TryGetCategories(out string[] categories)
            {
                if (_categories != null && _categories.Length > 0)
                {
                    categories = _categories;
                    return true;
                }
                categories = null;
                return false;
            }
        }
        
        internal class TypedMenuItem<TData> : MenuItemBase where TData : ItemData
        {
            protected readonly TData _itemData;
            private readonly Action<TData> _onSelected;
            protected readonly GenericMenu.MenuFunction2 _menuSelectHandler;

            internal TypedMenuItem(TData itemData, Action<TData> onSelected) : base()
            {
                _itemData = itemData;
                _onSelected = onSelected;
                _menuSelectHandler = OnMenuSelect;
            }

            private void OnMenuSelect(object menuData)
            {
                _onSelected.Invoke(_itemData);
            }

            public override void AddToMenu(string path, GenericMenu menu)
            {
                menu.AddItem(new GUIContent(path + _itemData.DisplayName),false, _menuSelectHandler, _itemData);
            }

            public override string Text => _itemData.DisplayName;
            public override int Priority { get; }
        }
        
        /*
        private class MenuItem : MenuItemBase
        {
            private ItemData _itemData;
            private readonly GenericMenu.MenuFunction2 _menuFunction2;
            public override int Priority { get; }
            public override string Text => _itemData.DisplayName;

            public MenuItem(ItemData itemData, GenericMenu.MenuFunction2 menuFunction2)
            {
                _itemData = itemData;
                _menuFunction2 = menuFunction2;
            }

            public override void AddToMenu(string path, GenericMenu menu)
            {
                if (_menuFunction2 != null)
                {
                    menu.AddItem(new GUIContent(path + _itemData.DisplayName), false, _menuFunction2, _itemData);
                }
            }
        }*/
        
        public abstract IEnumerable<ItemData> MenuItems { get; }

        public virtual GenericMenu GetMenu(Action<ItemData> onItemSelected)
        {
            SortableMenu menuRoot = new SortableMenu();

            if (MenuItems == null)
            {
                menuRoot.AddItem("NO ITEMS IN MENU", null, false, false);
                return menuRoot.CreateMenu();
            }

            Dictionary<string, SortableMenu> mapCategoryMenus = new Dictionary<string, SortableMenu>();
            SortableMenu subMenuAll = menuRoot.AddSubMenu(new SortableMenu("All"));
            menuRoot.AddSeparator();
            SortableMenu subMenuUncategorized = menuRoot.AddSubMenu(new SortableMenu("Uncategorized", 1));

            foreach (ItemData item in MenuItems)
            {
                TypedMenuItem<ItemData> menuItem = new TypedMenuItem<ItemData>(item, onItemSelected);
                subMenuAll.AddItem(menuItem);
                if (item.TryGetCategories(out var itemCategories))
                {
                    foreach (var category in itemCategories)
                    {
                        if (mapCategoryMenus.TryGetValue(category, out var subMenuCategory))
                        {
                            subMenuCategory.AddItem(menuItem);
                        }
                        else
                        {
                            subMenuCategory = new SortableMenu(category);
                            mapCategoryMenus[category] = subMenuCategory;
                            subMenuCategory.AddItem(menuItem);
                            menuRoot.AddSubMenu(subMenuCategory);
                        }
                    }
                }
                else
                {
                    subMenuUncategorized.AddItem(menuItem);
                }
            }
            menuRoot.AddSeparator();
            menuRoot.AddSubMenu(subMenuUncategorized);
            
            menuRoot.Sort((itemA, itemB) =>
                (itemA.Priority - itemB.Priority) * 100 + string.CompareOrdinal(itemA.Text, itemB.Text));
            return menuRoot.CreateMenu();
        }
    }
}