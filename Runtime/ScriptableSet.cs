using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NiftyScriptableSet
{
    public interface IScriptableSet
    {
        void Remove(Object asset);
        void Add(Object asset);
    }
    public class ScriptableSet<TAsset> : ScriptableObject, IScriptableSet where TAsset : Object
    {
        [SerializeField] [HideInInspector] protected List<TAsset> _references;
        public List<TAsset> References => _references;

        public void Add(TAsset asset)
        {
            if (_references.Contains(asset))
            {
                return;
            }
            _references.Add(asset);
        }

        public void Add(TAsset[] assetList)
        {
            for (int i = 0; i < assetList.Length; i++)
            {
                TAsset asset = assetList[i];
                Add(asset);
            }
        }
        
        public void Remove(TAsset asset)
        {
            _references.Remove(asset);
        }

        public void Add(Object asset)
        {
            if (asset is TAsset typedAsset)
            {
                Add(typedAsset);
            }
        }

        public void Remove(Object asset)
        {
            if (asset is TAsset typedAsset)
            {
                Remove(typedAsset);
            }
        }
    }
}