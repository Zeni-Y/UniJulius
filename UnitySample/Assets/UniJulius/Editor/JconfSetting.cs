using System.IO;
using UniJulius.Runtime;
using UnityEditor;
using UnityEngine;

namespace UniJulius.Editor
{
    public class JconfSetting : ScriptableObject
    {

        [Header("レベルと零交差による入力検知")]
        [Tooltip("振幅レベルのしきい値．値は 0 から 32767 の範囲で指定する． (default: 2000)")]
        public int lv = 2000;
        [Tooltip("零交差数のしきい値．値は１秒あたりの交差数で指定する． (default: 60)")]
        public int zc = 60;
        [Tooltip("音声区間開始部のマージン．単位はミリ秒． (default: 300)")]
        public int headmargin = 300;
        [Tooltip("音声区間終了部のマージン．単位はミリ秒． (default: 400)")]
        public int tailmargin = 400;

        [Header("入力棄却")]
        [Tooltip("検出された区間長がmsec以下の入力を 棄却する．その区間の認識は中断・破棄される．")]
        public int rejectshort = 500;
            
        [MenuItem("Window/UniJulius/Create Global Jconf Setting", priority = 6)]
        private static void Create()
        {
            var instance = CreateInstance<JconfSetting>();
            AssetDatabase.CreateAsset(instance, UniJuliusUtil.JconfSettingDirectory+"GlobalJconf.asset");
            AssetDatabase.Refresh();
        }

        [MenuItem("Window/UniJulius/Build Global Jconf", priority = 7)]
        private static void Build()
        {
            var jconf = AssetDatabase.LoadAssetAtPath<JconfSetting>(UniJuliusUtil.JconfSettingDirectory + "GlobalJconf.asset");
            //var jconf = Resources.Load() as JconfSetting;
            var path = UniJuliusUtil.ConfigDirectory + "global.jconf";
            using (var stream = new StreamWriter(path, append:false))
            {
                stream.Write("-h " + "../grammar-kit/model/phone_m/hmmdefs_ptm_gid.binhmm\n");
                stream.Write("-hlist " + "../grammar-kit/model/phone_m/logicalTri\n");
                stream.Write("-input " + "mic\n");
                stream.Write("-lv " + jconf.lv + "\n");
                stream.Write("-zc " + jconf.zc + "\n");
                stream.Write("-headmargin " + jconf.headmargin + "\n");
                stream.Write("-tailmargin " + jconf.tailmargin + "\n");
                stream.Write("-rejectshort " + jconf.rejectshort + "\n");
            }
            
            AssetDatabase.Refresh();
        }
    }
}