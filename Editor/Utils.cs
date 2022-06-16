using System.Collections.Generic;
using UnityEditor;

namespace NiftyScriptableSet
{
    public static class Utils
    {
        /*
        public static void RenameAsset(UnityEngine.Object asset, string newName)
        {
            // Check that asset is not null first using your prefered validation method
 
            var assetPath = AssetDatabase.GetAssetPath(asset);
 
            if (AssetDatabase.IsMainAsset(asset))
            {
                AssetDatabase.RenameAsset(assetPath, newName);
            }
            else if (AssetDatabase.IsSubAsset(asset))
            {
                asset.name = newName;
                EditorUtility.SetDirty(asset);
                var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
 
                AssetDatabase.RemoveObjectFromAsset(asset);
                EditorUtility.SetDirty(mainAsset);
                AssetDatabase.SaveAssetIfDirty(mainAsset);
                AssetDatabase.AddObjectToAsset(asset, mainAsset);
                EditorUtility.SetDirty(mainAsset);
                AssetDatabase.SaveAssetIfDirty(mainAsset);
            }
            else
            {
                throw new Exception("Object is not an asset.");
            }
        }*/

        public static void RenameAsset(UnityEngine.Object asset, string newName)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (AssetDatabase.IsMainAsset(asset))
            {
                AssetDatabase.RenameAsset(assetPath, newName);
                asset.name = newName;
            }
            if (AssetDatabase.IsSubAsset(asset))
            {
                AssetDatabase.ClearLabels(asset);
                asset.name = newName;
                AssetDatabase.SetLabels(asset, new[]{newName});
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }
        
        /// <summary>
        /// Gets all children of `SerializedProperty` at 1 level depth.
        /// </summary>
        /// <param name="serializedProperty">Parent `SerializedProperty`.</param>
        /// <returns>Collection of `SerializedProperty` children.</returns>
        public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty serializedProperty)
        {
            SerializedProperty currentProperty = serializedProperty.Copy();
            SerializedProperty nextSiblingProperty = serializedProperty.Copy();
            {
                nextSiblingProperty.Next(false);
            }
 
            if (currentProperty.Next(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;
 
                    yield return currentProperty;
                }
                while (currentProperty.Next(false));
            }
        }
 
        /// <summary>
        /// Gets visible children of `SerializedProperty` at 1 level depth.
        /// </summary>
        /// <param name="serializedProperty">Parent `SerializedProperty`.</param>
        /// <returns>Collection of `SerializedProperty` children.</returns>
        public static IEnumerable<SerializedProperty> GetVisibleChildren(this SerializedProperty serializedProperty)
        {
            SerializedProperty currentProperty = serializedProperty.Copy();
            SerializedProperty nextSiblingProperty = serializedProperty.Copy();
            {
                nextSiblingProperty.NextVisible(false);
            }
 
            if (currentProperty.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;
 
                    yield return currentProperty;
                }
                while (currentProperty.NextVisible(false));
            }
        }
    }
}