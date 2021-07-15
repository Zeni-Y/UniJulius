using System;
using UnityEngine;

namespace UniJulius.Runtime
{
    [Serializable]
    public class SpellNodeData
    {
        public string NodeGuid;
        public SpellData SpellData;
        public Vector2 Position;
    }
}