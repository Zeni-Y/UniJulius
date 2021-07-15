using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UniJulius.Runtime
{
    public static class UniJuliusUtil
    {
        public static string SpellContainerDirectory
        {
            get
            {
                const string path = "Assets/UniJulius/Resources/SpellContainers/";
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string SpellPacketDirectory
        {
            get
            {
                const string path = "Assets/UniJulius/Resources/SpellPackets/";
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string IsolatedWordDirectory
        {
            get
            {
                const string path = "Assets/UniJulius/Resources/IsolatedWords/";
                Directory.CreateDirectory(path);
                return path;
            }
        } 

        public static string FillerDirectory
        {
            get
            {
                const string path = "Assets/UniJulius/Resources/FillerData/";
                Directory.CreateDirectory(path);
                return path;
            }
        } 
        public static string JconfSettingDirectory {
            get
            { 
                const string path = "Assets/UniJulius/Resources/JconfSettings/";
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string ConfigDirectory
        {
            get
            {
                const string path = "Assets/StreamingAssets/UniJulius/UserConfig/";
                Directory.CreateDirectory(path);
                return path;
            }
        } 
        
        public static string GetJconfPath(string name)
        {
            Directory.CreateDirectory(ConfigDirectory+name);
            return ConfigDirectory + name + "/" + name + ".jconf";
        }
        
        public static string GetDictPath(string name)
        {
            Directory.CreateDirectory(ConfigDirectory+name);
            return ConfigDirectory + name + "/" + name + ".dict";
        }

        /// <summary>
        /// UniJuliusの認識結果をパースしてRecognitionResultクラスに格納する
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static List<RecognitionResult> ParseResult(string result)
        {
            var instances = result.Split('\n');
            var results = new List<RecognitionResult>(instances.Length - 1);
            
            if (instances[0] == "on_pass1_frame")
            {
                for (var i = 1; i < instances.Length - 1; i++)
                {
                    var items = instances[i].Split('\t');
                    var srInstanceName = items[1];
                    var grammarName = items[2];

                    if (items[0] == "error" || items[0] == "score is zero")
                    {
                        var tmp = new RecognitionResult(
                            ResultType.Pass1Error,
                            srInstanceName,
                            grammarName);
                        results.Add(tmp);
                    }
                    else
                    {
                        var word = items[3];
                        var wordId = int.Parse(items[4]);
                        var confidenceScore = float.Parse(items[5]);
                        var tmp = new RecognitionResult(
                            ResultType.Pass1,
                            srInstanceName,
                            grammarName,
                            word,
                            wordId,
                            confidenceScore,
                            float.MinValue,
                            float.MinValue);
                        results.Add(tmp);
                    }
                }
            }
            else if (instances[0] == "on_pass2_result")
            {
                for (var i = 1; i < instances.Length - 1; i++)
                {
                    var items = instances[i].Split('\t');
                    var srInstanceName = items[1];
                    var grammarName = items[2];

                    if (items[0] == "error")
                    {
                        var tmp = new RecognitionResult(
                            ResultType.Pass2Error,
                            srInstanceName,
                            grammarName);
                        results.Add(tmp);
                    }
                    else
                    {
                        var word = items[3];
                        var wordId = int.Parse(items[4]);
                        var confidenceScore = float.Parse(items[5]);
                        var lmScore = float.Parse(items[6]);
                        var amScore = float.Parse(items[7]);
                        var tmp = new RecognitionResult(
                            ResultType.Pass2,
                            srInstanceName,
                            grammarName,
                            word,
                            wordId,
                            confidenceScore,
                            lmScore,
                            amScore);
                        results.Add(tmp);
                    }
                }
            }

            return results;
        }

    }
}