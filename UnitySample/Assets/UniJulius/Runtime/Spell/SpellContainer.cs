using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniJulius.Runtime
{
    [Serializable]
    public class SpellContainer : ScriptableObject, IRecognized
    {
        public string JconfPath => UniJuliusUtil.GetJconfPath(name);
        public string DictPath => UniJuliusUtil.GetDictPath(name);
        public RecognitionType RecognitionType => RecognitionType.Spell;

        public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
        public List<SpellNodeData> SpellNodeData = new List<SpellNodeData>();
        public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
        public List<CommentBlockData> CommentBlockData = new List<CommentBlockData>();
        public FillerData FillerData; 
    }
}