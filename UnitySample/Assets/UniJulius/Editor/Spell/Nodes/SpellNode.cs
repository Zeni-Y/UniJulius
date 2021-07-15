using System.Collections.Generic;
using System.Text.RegularExpressions;
using UniJulius.Runtime;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniJulius.Editor
{
    
    public class SpellNode : UniJuliusNode
    { 
        public SpellPart Part { get; }

        private readonly TextField spellField;
        private readonly TextField kanaField;
        private readonly IntegerField recogSizeField;

        public virtual Port InputPort { get; set; }
        public virtual Port OutputPort { get; set; }
    
        public string Spell => spellField.value;
        public string Kana => kanaField.value;
        public int RecogSize => recogSizeField.value;

        public SpellNode(SpellData spellData)
        {
            title = "SpellNode";
            var template = Resources.Load<VisualTreeAsset>("SpellNode");
            template.CloneTree(mainContainer);

            spellField = mainContainer.Q<TextField>("spell");
            spellField.value = spellData.spell;
            kanaField = mainContainer.Q<TextField>("kana");
            kanaField.value = spellData.kana;
            recogSizeField = mainContainer.Q<IntegerField>("recogSize");
            recogSizeField.value = spellData.recogSize;
            Part = spellData.part;

            var button = mainContainer.Q<Button>("Convert");
            button.clickable.clicked += Convert2Kana;
        }

        public void Reset()
        {
            spellField.value = "";
            kanaField.value = "";
        }

        public void CopyNodeData(SpellNodeData data)
        {
            Guid = data.NodeGuid;
            spellField.value = data.SpellData.spell;
            kanaField.value = data.SpellData.kana;
            recogSizeField.value = data.SpellData.recogSize;
        }


        private void Convert2Kana()
        {
            if(spellField.value == "") return;
            //半角スペース、タブを削除
            Regex regex = new Regex(@"[\f\r\t\v\x85\p{Z}]");
            spellField.value = regex.Replace(spellField.text, "");
            spellField.value = spellField.text.Replace("\r\n", "\n");
            var result = Kanji2Yomi.Convert(spellField.text);

            //アルファベットと数字と"・"を削除
            regex = new Regex(@"[0-9a-zA-Z\+\-・！]");
            result = regex.Replace(result, "");
            kanaField.value = result;
        }

        public virtual void Search(ref List<string> spellList)
        {
            if (OutputPort == null) return;
            foreach (var edge in OutputPort.connections)
            {
                var next = edge.input.node as SpellNode;
                next?.Search(ref spellList);
            }
        }
    }
}
