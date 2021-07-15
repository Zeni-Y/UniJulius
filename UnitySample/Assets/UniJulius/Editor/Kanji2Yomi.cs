using System;
using System.Text;
using NMeCab.Specialized;

namespace UniJulius.Editor
{
    public static class Kanji2Yomi
    {
        private static string Katakana2Hiragana(string s) {
            StringBuilder sb = new StringBuilder();
            char[] target = s.ToCharArray();
            char c;
            for (int i = 0; i < target.Length; i++) {
                c = target[i];
                if (c >= 'ァ' && c <= 'ヴ') { //-> カタカナの範囲
                    c = (char)(c - 0x0060);  //-> 変換
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// NMecabを利用して漢字をかなに変換する
        /// </summary>
        /// <param name="target">変換対象</param>
        /// <returns>変換結果</returns>
        /// <exception cref="Exception"></exception>
        public static string Convert(string target) {
            target = target.Replace("\n", "\\");
            
            var dictPath = @"Assets/Packages/LibNMeCab.IpaDicBin.0.10.0/content/ipadic-NEologd";
            var result = "";
            using (var tagger = MeCabIpaDicTagger.Create(dictPath)) // Taggerインスタンスを生成
            {
                var nodes = tagger.Parse(target); // 形態素解析を実行
                foreach (var node in nodes) // 形態素ノード配列を順に処理
                { 
//                    Debug.Log(node);
                    //文字列の連結は別の機能で高速に実行したい
                    if (node.Surface == "\\")
                    {
                        result += "\\";
                    }
                    else if (string.IsNullOrEmpty(node.Reading))
                    {
                        result += Katakana2Hiragana(node.Surface);
                    }
                    else
                    {
                        result += Katakana2Hiragana(node.Reading);
                    }
                }
            }
            
            if (result == null)
            {
                throw new Exception();
            }
        
            result = result.Replace("\\", "\n");

            return result;
        }
    }
}
