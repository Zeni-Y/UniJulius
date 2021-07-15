using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniJulius.Runtime
{
    [Serializable]
    public class IsolatedWordData : ScriptableObject, IRecognized
    {
        public string JconfPath => UniJuliusUtil.GetJconfPath(name);
        public string DictPath => UniJuliusUtil.GetDictPath(name);
        public RecognitionType RecognitionType => RecognitionType.Isolated;
        
        public List<string> words;

    }
}