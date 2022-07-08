using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NiftyScriptableSet
{
    [CustomEditor(typeof(ScriptableSet<>))]
    public class ScriptableSetInspector<TAsset> : Editor where TAsset : UnityEngine.Object
    {
        protected ScriptableSet<TAsset> _assetSet;
        private ReorderableList _reorderableList;
        private GenericMenu _menuAdd;
        private GenericMenu _menuRemove;
        protected ProviderMenuScriptableSet<TAsset> ScriptableSetProvider = new ProviderMenuScriptableSet<TAsset>();
        private List<UnityEngine.Object> _orphanedObjects;
        private Editor _peekEditor;
        private SerializedObject _lastSelected;
        public event Action OnPeekChanged;

        protected virtual void OnEnable()
        {
            _assetSet = target as ScriptableSet<TAsset>;

            var propReferences = serializedObject.FindProperty("_references");

            if (propReferences != null && propReferences.isArray)
            {
                _reorderableList = new ReorderableList(serializedObject, propReferences, true, true, true, true)
                {
                    onAddCallback = HandleAdd,
                    onRemoveCallback = HandleRemove,
                    onSelectCallback = HandleSelect,
                    onChangedCallback = HandleChanged,
                    drawElementCallback = HandleDrawElement
                };
            }

            _orphanedObjects = GetOrphanedAssetData();
        }

        protected virtual void DrawElement(Rect rect, SerializedProperty element, TAsset asset, int index, bool isActive, bool isFocused)
        {
            Color defaultColor = GUI.color;

            if (element != null)
            {
                float iconSize = rect.height;
                Rect iconRect = new Rect(rect.x, rect.y, iconSize, iconSize);
                Rect renameRect = new Rect(iconRect.xMax, rect.y, 20, 20);
                Rect textRect = new Rect(renameRect.xMax, rect.y, rect.width - iconRect.width,
                    EditorGUIUtility.singleLineHeight);
                
                if (element.objectReferenceValue == null)
                {
                    GUI.color = Color.red;
                    GUI.Label(textRect,"NULL");
                    GUI.color = defaultColor;
                }
                else
                {
                    if (asset != null)
                    {
                        var itemData = ScriptableSetProvider.GetItemData<TAsset>();
                        var path = AssetDatabase.GetAssetPath(asset);
                        Texture assetIcon = AssetDatabase.GetCachedIcon(path);
                        GUIContent iconContent = new GUIContent(assetIcon);
                        
                        if (itemData != null && itemData.IsObsolete)
                        {
                            iconContent = EditorGUIUtility.IconContent("Warning");
                            GUI.color = Color.yellow;
                        }

                        string displayName = GetEditorListDisplayName(asset);
                        GUI.Label(iconRect, iconContent);
                        GUI.Label(textRect,displayName);
                        GUI.color = defaultColor;
                    }
                    else
                    {
                        GUI.color = Color.red;
                        GUI.Label(textRect,$"{element.type} does not cast to {nameof(TAsset)}");
                        GUI.color = defaultColor;
                    }
                }
            }
        }

        protected void HandleDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            DrawElement(rect, element, (TAsset)element.objectReferenceValue, index, isActive, isFocused);
        }

        private void HandleChanged(ReorderableList list)
        {
            ClearLastSelected();
        }

        private void HandleSelect(ReorderableList list)
        {
            var selectedElement = list.serializedProperty.GetArrayElementAtIndex(_reorderableList.index);
            if (_lastSelected == null || _lastSelected.targetObject != selectedElement.objectReferenceValue)
            {
                if (selectedElement.objectReferenceValue != null)
                {
                    _lastSelected = new SerializedObject(selectedElement.objectReferenceValue);
                }
            }
        }

        public void ClearLastSelected()
        {
            _lastSelected = null;
        }

        protected virtual void HandleCreateItem(object selectedMenuData)
        {
            Type selectedType = typeof(TAsset);
            string instanceName = selectedType.Name;
            if (selectedMenuData != null)
            {
                var itemData = selectedMenuData as ProviderMenuScriptableSet<TAsset>.ItemData;
                selectedType = itemData?.ItemType;
                if (itemData.DisplayName != null)
                {
                    instanceName = itemData.DisplayName;
                }
            }

            var instance = CreateInstance(selectedType) as TAsset;
            instance.name = instanceName;
            PreProcessItem(instance);
            
            InsertItem(instance);
        }

        protected virtual void PreProcessItem(TAsset item)
        {
            
        }

        private void InsertItem(TAsset item)
        {
            if (item != null)
            {
                int selectedIndex = _reorderableList.index;
                AssetDatabase.AddObjectToAsset(item, _assetSet);
                AddItemToSerializedArray(_reorderableList.serializedProperty, item, selectedIndex);
                AssetDatabase.SaveAssets();
            }
        }

        private void RemoveItem(int selectedIndex)
        {
            if (RemoveItemFromSerializedArray(_reorderableList.serializedProperty, out var removedItem, selectedIndex))
            {
                AssetDatabase.RemoveObjectFromAsset(removedItem);
                AssetDatabase.SaveAssets();
            }
        }

        private List<UnityEngine.Object> GetOrphanedAssetData()
        {
            UnityEngine.Object[] allSubAsset = GetAllSubAssets();
            List<UnityEngine.Object> orphanedItems = new List<UnityEngine.Object>();
            foreach (var item in allSubAsset)
            {
                if (item == _assetSet || _assetSet.References.Contains(item))
                {
                    continue;
                }

                orphanedItems.Add(item);
            }

            return orphanedItems;
        }

        private UnityEngine.Object[] GetAllSubAssets()
        {
            AssetDatabase.Refresh();
            var path = AssetDatabase.GetAssetPath(_assetSet);
            var loadedAsset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
            UnityEngine.Object[] allSubAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            return allSubAssets;
        }
        
        private string _assetRenameString;

        private void DrawAssetOptions(UnityEngine.Object targetAsset)
        {
            if (GUILayout.Button("Ping", GUILayout.Width(60)))
            {
                EditorGUIUtility.PingObject(targetAsset);
            }
            
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(targetAsset.name);
                GUIContent editButtonContent = EditorGUIUtility.IconContent("d_editicon.sml");
                if (GUILayout.Button(editButtonContent, GUILayout.Width(20)))
                {
                    _assetRenameString = targetAsset.name;
                    GUI.FocusControl("RenameField");
                }
                if (!string.IsNullOrEmpty(_assetRenameString))
                {
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        _assetRenameString = null;
                    }
                    bool isUniqueName = _assetSet.References.All(item => item.name != _assetRenameString);
                    if (isUniqueName)
                    {
                        GUI.color = Color.green;
                    }
                    else
                    {
                        GUI.color = Color.red;
                    }
                    GUIContent validButtonContent = EditorGUIUtility.IconContent("Valid");
                    EditorGUI.BeginDisabledGroup(!isUniqueName);
                    {
                        if (GUILayout.Button(validButtonContent) || (isUniqueName && Event.current.keyCode == KeyCode.Return))
                        {
                            Utils.RenameAsset(targetAsset, _assetRenameString);
                            _assetRenameString = null;
                        }
                        EditorGUI.EndDisabledGroup();
                        GUI.SetNextControlName("RenameField");
                        _assetRenameString = GUILayout.TextField(_assetRenameString);
                    }
                }
            }
            GUILayout.EndHorizontal();
            //Rename
            
        }

        private void DrawPeek(UnityEngine.Object target)
        {
            if (_peekEditor == null || _peekEditor.target != target)
            {
                _peekEditor = CreateEditor(target);
            }

            using (var peekChange = new EditorGUI.ChangeCheckScope())
            {
                using (new EditorGUI.IndentLevelScope(1))
                {
                    _peekEditor.OnInspectorGUI();
                }

                if (peekChange.changed)
                {
                    OnPeekChanged?.Invoke();
                }
            }
        }

        private void DrawReferenceRepair(UnityEngine.Object item)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetEditorListDisplayName(item as TAsset));
            if (item is TAsset subAsset)
            {
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    int selectedIndex = _reorderableList.index;
                    AddItemToSerializedArray(_reorderableList.serializedProperty, subAsset, selectedIndex);
                    AssetDatabase.SaveAssets();
                    _orphanedObjects = GetOrphanedAssetData();
                }
            }

            GUILayout.EndHorizontal();
        }

        private void RemoveItem(TAsset selectedItem)
        {
            int removeIndex = _reorderableList.list.IndexOf(selectedItem);
            if (removeIndex > 0 &&
                RemoveItemFromSerializedArray(_reorderableList.serializedProperty, out _, removeIndex))
            {
                AssetDatabase.RemoveObjectFromAsset(selectedItem);
                AssetDatabase.SaveAssets();
            }
        }

        private static bool AddItemToSerializedArray(SerializedProperty property, TAsset data, int index = -1)
        {
            if (property != null && property.isArray)
            {
                property.arraySize++;
                var insertedElement = property.GetArrayElementAtIndex(property.arraySize - 1);
                insertedElement.objectReferenceValue = data;
                if (index > 0)
                {
                    property.MoveArrayElement(property.arraySize, index);
                }

                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
                return true;
            }

            return false;
        }

        private static bool RemoveItemFromSerializedArray(SerializedProperty property, out TAsset removedItem,
            int index = -1)
        {
            if (property != null && property.isArray)
            {
                var elementAtIndex = property.GetArrayElementAtIndex(index);
                if (elementAtIndex != null)
                {
                    removedItem = elementAtIndex.objectReferenceValue as TAsset;
                    property.DeleteArrayElementAtIndex(index);

                    property.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.Update();
                    return true;
                }
            }

            removedItem = null;
            return false;
        }

        public override void OnInspectorGUI()
        {
            if (_reorderableList != null && _reorderableList.serializedProperty != null)
            {
                _reorderableList.DoLayoutList();
            }

            if (_orphanedObjects != null && _orphanedObjects.Count > 0)
            {
                DrawOrphanedObjects(_orphanedObjects);
            }

            if (_lastSelected != null && _lastSelected.targetObject != null)
            {
                DrawAssetOptions(_lastSelected.targetObject);
                DrawPeek(_lastSelected.targetObject);
            }
        }

        public void DrawDefaultInspector()
        {
            base.OnInspectorGUI();
        }

        private void DrawOrphanedObjects(List<UnityEngine.Object> orphanedObjects)
        {
            GUILayout.Label("Orphan Data Fix");
            bool hasBrokenRef = false;
            Color defaultColor = GUI.color;
            foreach (UnityEngine.Object item in orphanedObjects)
            {
                if (item != null)
                {
                    DrawReferenceRepair(item);
                }
                else
                {
                    hasBrokenRef = true;
                }
            }
            if (hasBrokenRef)
            {
                GUI.color = Color.red;
                if (GUILayout.Button("UNITY HATES YOU. CLICK THIS FIX ALL THE BROKEN BULLSHIT"))
                {
                    FixMissingScript(_assetSet);
                }

                GUI.color = defaultColor;
            }
            
        }

        private void HandleRemove(ReorderableList list)
        {
            int selectedIndex = _reorderableList.index;
            if (selectedIndex >= 0)
            {
                RemoveItem(selectedIndex);
            }
        }

        public virtual string GetEditorListDisplayName(TAsset item)
        {
            return item.name;
        }

        protected void HandleAdd(ReorderableList list)
        {
            if (ScriptableSetProvider != null)
            {
                if (ScriptableSetProvider.IsMultiType)
                {
                    GenericMenu menu = ScriptableSetProvider.GetMenu(HandleCreateItem);
                    menu.ShowAsContext();
                }
                else
                {
                    HandleCreateItem(ScriptableSetProvider.DefaultItem);
                }
            }
            else
            {
                var instance = CreateInstance(typeof(TAsset)) as TAsset;
                InsertItem(instance);
            }
        }

        public void RenameAsset(TAsset asset, string name)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            AssetDatabase.RenameAsset(path, name);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        /// <summary>
        /// Function used to remove a sub-asset that is missing the script reference
        /// </summary>
        /// <param name="toDelete">The main asset that holds the sub-asset</param>
        private void FixMissingScript(UnityEngine.Object toDelete)
        {
            //Create a new instance of the object to delete
            ScriptableObject newInstance = CreateInstance(toDelete.GetType());

            //Copy the original content to the new instance
            EditorUtility.CopySerialized(toDelete, newInstance);
            newInstance.name = toDelete.name;

            string toDeletePath = AssetDatabase.GetAssetPath(toDelete);
            string clonePath = toDeletePath.Replace(".asset", "CLONE.asset");

            //Create the new asset on the project files
            AssetDatabase.CreateAsset(newInstance, clonePath);
            AssetDatabase.ImportAsset(clonePath);

            //Unhide sub-assets
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(toDeletePath);
            HideFlags[] flags = new HideFlags[subAssets.Length];
            for (int i = 0; i < subAssets.Length; i++)
            {
                //Ignore the "corrupt" one
                if (subAssets[i] == null)
                    continue;

                //Store the previous hide flag
                flags[i] = subAssets[i].hideFlags;
                subAssets[i].hideFlags = HideFlags.None;
                EditorUtility.SetDirty(subAssets[i]);
            }

            EditorUtility.SetDirty(toDelete);
            AssetDatabase.SaveAssets();

            //Reparent the subAssets to the new instance
            foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(toDeletePath))
            {
                //Ignore the "corrupt" one
                if (subAsset == null)
                    continue;

                //We need to remove the parent before setting a new one
                AssetDatabase.RemoveObjectFromAsset(subAsset);
                AssetDatabase.AddObjectToAsset(subAsset, newInstance);
            }

            //Import both assets back to unity
            AssetDatabase.ImportAsset(toDeletePath);
            AssetDatabase.ImportAsset(clonePath);

            //Reset sub-asset flags
            for (int i = 0; i < subAssets.Length; i++)
            {
                //Ignore the "corrupt" one
                if (subAssets[i] == null)
                    continue;

                subAssets[i].hideFlags = flags[i];
                EditorUtility.SetDirty(subAssets[i]);
            }

            EditorUtility.SetDirty(newInstance);
            AssetDatabase.SaveAssets();

            //Here's the magic. First, we need the system path of the assets
            string globalToDeletePath =
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), toDeletePath);
            string globalClonePath =
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), clonePath);

            //We need to delete the original file (the one with the missing script asset)
            //Rename the clone to the original file and finally
            //Delete the meta file from the clone since it no longer exists

            System.IO.File.Delete(globalToDeletePath);
            System.IO.File.Delete(globalClonePath + ".meta");
            System.IO.File.Move(globalClonePath, globalToDeletePath);

            AssetDatabase.Refresh();
        }
    }
}