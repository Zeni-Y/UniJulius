using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UniJulius.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniJulius.Editor
{
    public class SpellPacketWindow : EditorWindow
    {
        [SerializeField]
        public List<SpellContainer> spellContainers = new List<SpellContainer>(3);

        private string fileName;
        private string currentFilePath;
        
        [MenuItem("Window/UniJulius/Open Spell Packet Window", priority = 3)]
        public static void Open()
        {
            GetWindow<SpellPacketWindow>("SpellPacket");
        }

        private void Load(string name = "")
        {
            fileName = name;
            if (string.IsNullOrEmpty(name))
            {
                currentFilePath = EditorUtility.OpenFilePanel("Select SpellPacket", UniJuliusUtil.SpellPacketDirectory, "asset");
                if(string.IsNullOrEmpty(currentFilePath)) return;
                fileName = System.IO.Path.GetFileNameWithoutExtension(currentFilePath);
            }
            titleContent.text = "SpellPacket/" + fileName;
            var spellPacketData = Resources.Load<SpellPacketData>("SpellPackets/" + fileName);
            if (spellPacketData == null)
            {
                EditorUtility.DisplayDialog("File Not Found", "Target Narrative Data does not exist!", "OK");
                return;
            }
            spellContainers = new List<SpellContainer>(spellPacketData.spellContainers);
        }
        

        private void Save(bool overwrite, string popupText = "Save SpellPacket")
        {
            if (string.IsNullOrEmpty(currentFilePath) || !overwrite)
            {
                currentFilePath = EditorUtility.SaveFilePanel(popupText, UniJuliusUtil.SpellPacketDirectory, "SampleSpellPacket", "asset");
                if (string.IsNullOrEmpty(currentFilePath)) return;
                fileName = System.IO.Path.GetFileNameWithoutExtension(currentFilePath);
                titleContent.text = "SpellPacket/" + fileName;
                var tmp = Regex.Split(currentFilePath, "/Assets/");
                currentFilePath = "Assets/" + tmp[1];
            }
            Debug.Log(currentFilePath);
            var spellPacketData = ScriptableObject.CreateInstance<SpellPacketData>();
            
            spellPacketData.spellContainers = new List<SpellContainer>(spellContainers);
            
            var loadedAsset = AssetDatabase.LoadAssetAtPath(currentFilePath, typeof(SpellPacketData));
            
            if (loadedAsset == null || !AssetDatabase.Contains(loadedAsset))
            {
                AssetDatabase.CreateAsset(spellPacketData, currentFilePath);
            }
            else
            {
                var packet = loadedAsset as SpellPacketData;
                packet.spellContainers = new List<SpellContainer>(spellContainers);
                EditorUtility.SetDirty(packet);
            }
            AssetDatabase.SaveAssets();
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Initialize();
            }
            else
            {
                
                //再コンパイル時にも呼ばれるため、事前に読み込んでおく
                Initialize();
                Load(fileName);
            }
        }

        private void OnDisable()
        {
            //Save(false, "コンパイルが走るとリセットされるので保存するなら保存してください");
        }

        private void Initialize()
        {
            var template = Resources.Load<VisualTreeAsset>("SpellPacketWindow");
            template.CloneTree(rootVisualElement);
            
            var fileMenu = rootVisualElement.Q<ToolbarMenu>("file-menu");
            spellContainers = new List<SpellContainer>(3);
            currentFilePath = null;
            fileMenu.menu.AppendAction("New", a =>
            {
                rootVisualElement.Clear();
                Initialize();
                fileName = "";
                currentFilePath = "";
                titleContent.text = "SpellPacket";
            }, DropdownMenuAction.Status.Normal);
            
            fileMenu.menu.AppendAction("Open...", a =>
            {
                Load();
            }, DropdownMenuAction.Status.Normal);
            fileMenu.menu.AppendSeparator();
            fileMenu.menu.AppendAction("Save", a =>
            {
                Save(true);
            }, DropdownMenuAction.Status.Normal);
            fileMenu.menu.AppendAction("Save As...", a =>
            {
                Save(false);
            }, DropdownMenuAction.Status.Normal);

            rootVisualElement.Q<PropertyField>("spellContainerList").Bind(new SerializedObject(this));
            var buildButton = rootVisualElement.Q<Button>("buildButton");
            buildButton.clicked += Build;
        }


        /// <summary>
        /// TO-DO:fillerには別IDを振る
        /// </summary>
        private void Build()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                Save(false);
            }
            var kanaList = new List<(string kana, int recogSize)>();
            
            var regex = new Regex(@"[^\p{IsHiragana}ー\n]");

            foreach (var spellContainer in spellContainers)
            {
                foreach (var spellNodeData in spellContainer.SpellNodeData)
                {
                    var hiragana = regex.Replace(spellNodeData.SpellData.kana, "");
                    kanaList.Add((hiragana, spellNodeData.SpellData.recogSize));
                }

                var filler = spellContainer.FillerData;
                if (filler != null)
                    foreach (var word in filler.words)
                    {
                        kanaList.Add((word, Mathf.Max(1,word.Length)));
                    }
            }
            
            Yomi2Voca.GenerateDict(kanaList, fileName);
            JconfGenerator.GenerateJconf(fileName);
        }
    }
}