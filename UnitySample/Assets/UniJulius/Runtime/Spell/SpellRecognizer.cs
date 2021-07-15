using System;
using System.Linq;
using UnityEngine;

namespace UniJulius.Runtime
{
    /// <summary>
    /// 未実装
    /// </summary>
    public class SpellRecognizer : MonoBehaviour
    {
        [SerializeField]
        private string filename;
        private SpellContainer spellContainer;
        private string currentSpell;
        private SpellNodeData current;

        private void Start()
        {
            spellContainer = Resources.Load<SpellContainer>("SpellContainers/"+filename);
            Debug.Log(spellContainer.SpellNodeData.Count());
            current = spellContainer.SpellNodeData.First(x => x.SpellData.part == SpellPart.Start);
            Debug.Log("Start kana is: "+current.SpellData.kana);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (current.SpellData.part == SpellPart.Last)
                {
                    Debug.Log("This is last spell: " + current.SpellData.kana);
                    return;
                }
                current = SearchNextSpell(current);
                Debug.Log("Next kana is: "+current.SpellData.kana);
            }
        }

        public void Begin(string jconfPath)
        {
//            Julius.Begin(jconfPath);
        }

        public void RegisterInitDict()
        {
            
        }
        


        private SpellNodeData SearchNextSpell(SpellNodeData data)
        {
            var link = spellContainer.NodeLinks.FirstOrDefault(x => x.BaseNodeGuid == data.NodeGuid);
//            if (link == null || string.IsNullOrEmpty(link.TargetNodeGuid)) return new SpellNodeData();
            var nextGuid = link?.TargetNodeGuid;
            var next = spellContainer.SpellNodeData.First(x => x.NodeGuid == nextGuid);
            return next;
        }
    }
}