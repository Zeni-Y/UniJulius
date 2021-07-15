using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniJulius.Runtime
{
    [Serializable]
    public class SpellPacketData : ScriptableObject, IRecognized
    {
        public string JconfPath => UniJuliusUtil.GetJconfPath(name);
        public string DictPath => UniJuliusUtil.GetDictPath(name);
        public RecognitionType RecognitionType => RecognitionType.Spell;
        
        public string fileName;
        public List<SpellContainer> spellContainers = new List<SpellContainer>();

    }
}