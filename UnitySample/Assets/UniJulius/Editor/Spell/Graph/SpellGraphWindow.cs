using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UniJulius.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniJulius.Editor
{
    public class SpellGraphWindow : EditorWindow
    {
        private string fileName;
        private SpellGraphView graphView;
        private SpellContainer spellContainer;
        private ObjectField fillerField;

        private string currentFilePath = "";

        [MenuItem("Window/UniJulius/Open Spell Container Graph", priority = 2)]
        public static void Open()
        {
            GetWindow<SpellGraphWindow>("Spell Graph");
        }

        private void ConstructGraphView()
        {
            graphView = new SpellGraphView(this)
            {
                name = "Spell Graph",
            };
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);
            var tab = Resources.Load<VisualTreeAsset>("BlackBoard");
            tab.CloneTree(rootVisualElement);
        }
        
        private void GenerateMiniMap()
        {
            var miniMap = new MiniMap {anchored = true};
            var cords = graphView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x - 10, 30));
            miniMap.SetPosition(new Rect(cords.x, cords.y, 200, 140));
            graphView.Add(miniMap);
        }

        private void GenerateBlackBoard()
        {
            var blackboard = new Blackboard(graphView);
            blackboard.Add(new BlackboardSection {title = "Exposed Variables"});
            blackboard.addItemRequested = _blackboard =>
            {
                graphView.AddPropertyToBlackBoard(ExposedProperty.CreateInstance(), false);
            };
            blackboard.editTextRequested = (_blackboard, element, newValue) =>
            {
                var oldPropertyName = ((BlackboardField) element).text;
                if (graphView.ExposedProperties.Any(x => x.PropertyName == newValue))
                {
                    EditorUtility.DisplayDialog("Error", "This property name already exists, please chose another one.",
                        "OK");
                    return;
                }

                var targetIndex = graphView.ExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
                graphView.ExposedProperties[targetIndex].PropertyName = newValue;
                ((BlackboardField) element).text = newValue;
            };
            blackboard.SetPosition(new Rect(10,30,200,300));
            graphView.Add(blackboard);
            graphView.Blackboard = blackboard;
        }
        
        private void Save(bool overwrite, string popupText = "Save SpellContainer")
        {
            var saveUtility = GraphSaveUtility.GetInstance(graphView);
            //上書き保存で保存先のファイルが判明しているとき
            if (overwrite)
            {
                if (!string.IsNullOrEmpty(currentFilePath))
                {
                    saveUtility.SaveGraph(currentFilePath);
                    return;
                }
            }
            var path = EditorUtility.SaveFilePanel(popupText, UniJuliusUtil.SpellContainerDirectory,"Spell" ,"asset");
            //filePathをAssets/~に変換する
            if(string.IsNullOrEmpty(path)) return;
            fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            var tmp = Regex.Split(path, "/Assets/");
            currentFilePath = "Assets/" + tmp[1];
            titleContent.text = "Spell Graph / " + fileName;
            saveUtility.SaveGraph(currentFilePath);
        }

        private void Load()
        {
            var path = EditorUtility.OpenFilePanel("Select Spell", UniJuliusUtil.SpellContainerDirectory, "asset");
            if(string.IsNullOrEmpty(path)) return;
            fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            //filePathをAssets/~に変換する
            var tmp = Regex.Split(path, "/Assets/");
            currentFilePath = "Assets/" + tmp[1];
            var saveUtility = GraphSaveUtility.GetInstance(graphView);
            titleContent.text = "Spell Graph / " + fileName;
            saveUtility.LoadNarrative(fileName);
        }

        
        void RegisterFileOperation()
        {
            var fileMenu = rootVisualElement.Q<ToolbarMenu>("fileMenu");
            fileMenu.menu.AppendAction("New", a =>
            {
                rootVisualElement.Clear();
                Initialize();
                fileName = "";
                currentFilePath = "";
                titleContent.text = "Spell Graph";
            }, DropdownMenuAction.Status.Normal);
            fileMenu.menu.AppendAction("Open...", a => Load());
            fileMenu.menu.AppendSeparator();
            fileMenu.menu.AppendAction("Save", a => Save(true));
            fileMenu.menu.AppendAction("Save As...", a => Save(false));
        }

        //一度セーブをしてからSpellContainerを使って行う
        /// <summary>
        /// TO-DO:fillerには別IDを振る
        /// </summary>
        private void Build()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                Save(false);
            }
            var spellEdges = graphView.edges.ToList();
            if(!spellEdges.Any()) return;
            
            var nodes = graphView.nodes.ToList().Cast<SpellNode>();
            var allKana = nodes.Aggregate("", (current, node) => current + (node.Kana + "\n"));
            var kanaList = new List<(string kana, int recogSize)>();
            var regex = new Regex(@"[^\p{IsHiragana}ー\n]");
            foreach (var node in nodes)
            {
                //ひらがなのみにする
                var hiragana = regex.Replace(node.Kana, "");
                kanaList.Add((hiragana, node.RecogSize));
            }
            
            var filler = fillerField.value as FillerData;

            if (filler != null)
                foreach (var word in filler.words)
                {
                    kanaList.Add((word, Mathf.Max(1,word.Length)));
                }

            Yomi2Voca.GenerateDict(kanaList, fileName);
            JconfGenerator.GenerateJconf(fileName);
        }
        
        void OnEnable()
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Initialize();
            }
            else
            {
                //再コンパイル時にも呼ばれるため、事前に読み込んでおく
                Initialize();
                var saveUtility = GraphSaveUtility.GetInstance(graphView);
                saveUtility.LoadNarrative(fileName);
                
            }
        }


        private void OnDisable()
        {
            //変更を検知して、変更時のみ保存処理が入るようにしたい
            //Save(false, "コンパイルが走るとリセットされるので保存するなら保存してください");
            
        }
        
        void Initialize()
        {
            ConstructGraphView();
            GenerateMiniMap();
            RegisterFileOperation();

            fillerField = rootVisualElement.Q<ObjectField>("fillerFile");
            fillerField.objectType = typeof(FillerData);
            graphView.FillerField = fillerField;
            
            var buildButton = rootVisualElement.Q<Button>("buildButton");
            buildButton.clickable.clicked += Build;
        }
        
    }
}