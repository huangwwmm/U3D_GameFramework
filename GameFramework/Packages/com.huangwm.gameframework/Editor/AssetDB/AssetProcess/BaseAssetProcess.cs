using GF.Common;
using GF.Common.Debug;
using GF.Common.Utility;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GFEditor.AssetDB
{
    public abstract class BaseAssetProcess : ScriptableObject
    {
        public abstract void ProcessAll(Object[] assets);
    }
}