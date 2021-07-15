using System.Collections.Generic;
using UniJulius.Runtime;
using UnityEditor;
using UnityEngine;

namespace UniJulius.Editor
{
    public class FillerDataGenerator
    {
        [MenuItem("Window/UniJulius/Create FillerData", priority = 5)]
        private static void Create()
        {
            var instance = ScriptableObject.CreateInstance<FillerData>();
            AssetDatabase.CreateAsset(instance, UniJuliusUtil.FillerDirectory+"FillerData.asset");
            AssetDatabase.Refresh();
        }
    }
}