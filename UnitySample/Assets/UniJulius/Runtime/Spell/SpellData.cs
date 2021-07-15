using System;

namespace UniJulius.Runtime
{
    public enum SpellPart
    {
        Start,
        Middle,
        Last
    }
    [Serializable]
    public struct SpellData
    {
        public SpellData(SpellPart part, string spell, string kana, int recogSize = 1)
        {
            this.part = part;
            this.spell = spell;
            this.kana = kana;
            this.recogSize = recogSize;
        }
        public SpellPart part;
        public string spell;
        public string kana;
        public int recogSize;

    }
}