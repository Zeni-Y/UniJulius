using System.Collections.Generic;
using System.Text.RegularExpressions;
using UniJulius.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniJulius.Editor
{
    public class IsolatedWordWindow : EditorWindow
    {
        private string fileName;

        private string currentFilePath;
        public List<string> wordList;
        
        [MenuItem("Window/UniJulius/Open Isolated Word Window", priority = 1)]
        public static void Open()
        {
            GetWindow<IsolatedWordWindow>("IsolatedWord");
        }

        private void Load(string name = "")
        {
            fileName = name;
            if (string.IsNullOrEmpty(name))
            {
                currentFilePath = EditorUtility.OpenFilePanel("Select IsolatedWord", UniJuliusUtil.IsolatedWordDirectory, "asset");
                if(string.IsNullOrEmpty(currentFilePath)) return;
                fileName = System.IO.Path.GetFileNameWithoutExtension(currentFilePath);
            }
            titleContent.text = "IsolatedWord/" + fileName;
            var isolatedWordData = Resources.Load<IsolatedWordData>("IsolatedWords/" + fileName);
            if (isolatedWordData == null)
            {
                EditorUtility.DisplayDialog("File Not Found", "Target Isolated Word Data does not exist!", "OK");
                return;
            }
            wordList = new List<string>(isolatedWordData.words);
        }
        

        private void Save(bool overwrite, string popupText = "Save Isolated Word")
        {
            if (string.IsNullOrEmpty(currentFilePath) || !overwrite)
            {
                currentFilePath = EditorUtility.SaveFilePanel(popupText, UniJuliusUtil.IsolatedWordDirectory, "IsolateWord", "asset");
                if (string.IsNullOrEmpty(currentFilePath)) return;
                fileName = System.IO.Path.GetFileNameWithoutExtension(currentFilePath);
                titleContent.text = "IsolatedWord/" + fileName;
                var tmp = Regex.Split(currentFilePath, "/Assets/");
                currentFilePath = "Assets/" + tmp[1];
            }
            var isolatedWordData = ScriptableObject.CreateInstance<IsolatedWordData>();
            
            var loadedAsset = AssetDatabase.LoadAssetAtPath(currentFilePath, typeof(IsolatedWordData));
            
            if (loadedAsset == null || !AssetDatabase.Contains(loadedAsset))
            {
                isolatedWordData.words = wordList;
                AssetDatabase.CreateAsset(isolatedWordData, currentFilePath);
            }
            else
            {
                var data = loadedAsset as IsolatedWordData;
                data.words = wordList;
                EditorUtility.SetDirty(data);
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

        private void RegisterFileOperation()
        {
            var fileMenu = rootVisualElement.Q<ToolbarMenu>("file-menu");
            fileMenu.menu.AppendAction("New", a =>
            {
                rootVisualElement.Clear();
                Initialize();
                fileName = "";
                currentFilePath = "";
                titleContent.text = "IsolatedWord";
            }, DropdownMenuAction.Status.Normal);
            
            fileMenu.menu.AppendAction("Open...", a => Load());
            fileMenu.menu.AppendSeparator();
            fileMenu.menu.AppendAction("Save", a => Save(true));
            fileMenu.menu.AppendAction("Save As...", a => Save(false));
        }

        private void Initialize()
        {
            var template = Resources.Load<VisualTreeAsset>("IsolatedWordBuilder");
            template.CloneTree(rootVisualElement);

            RegisterFileOperation();

            rootVisualElement.Q<PropertyField>("wordList").Bind(new SerializedObject(this));
            wordList = new List<string>(3);
            currentFilePath = null;

            var buildButton = rootVisualElement.Q<Button>("buildButton");
            buildButton.clicked += Build;
        }


        private void Build()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                Save(false);
            }
            var kanaList = new List<(string kana, int recogSize)>();
            foreach (var word in wordList)
            {
                kanaList.Add((word, word.Length));
            }
            
            Yomi2Voca.GenerateDict(kanaList, fileName);
            JconfGenerator.GenerateJconf(fileName);
        }
    }
}