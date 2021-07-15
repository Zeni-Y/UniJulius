using System.IO;
using UniJulius.Runtime;
using UnityEditor;

namespace UniJulius.Editor
{
    public class JconfGenerator
    {
        public static void GenerateJconf(string name, JconfSetting jconfSetting=null)
        {
            var path = UniJuliusUtil.GetJconfPath(name);
            using (var stream = new StreamWriter(path, append:false))
            {
                stream.Write("-w "+ name + ".dict\n");
                stream.Write("-C ../global.jconf\n");
//                stream.Write("-h " + "../../grammar-kit/model/phone_m/hmmdefs_ptm_gid.binhmm\n");
//                stream.Write("-hlist " + "../../grammar-kit/model/phone_m/logicalTri\n");
//                stream.Write("-input " + "mic\n");
            }
            
            AssetDatabase.Refresh();
        }
    }
}
