using System;
using System.Collections.Generic;
using UniJulius.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniJulius.Editor
{
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private EditorWindow window;
        private SpellGraphView graphView;

        private Texture2D indentationIcon;
        
        public void Configure(EditorWindow window, SpellGraphView graphView)
        {
            this.window = window;
            this.graphView = graphView;
            indentationIcon = new Texture2D(1, 1);
            indentationIcon.SetPixel(0,0,new Color(0,0,0,0));
            indentationIcon.Apply();
        }
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
                new SearchTreeGroupEntry(new GUIContent("Spell"), 1),
                new SearchTreeEntry(new GUIContent("Start Spell", indentationIcon))
                {
                    level = 2, userData = "Start Spell"
                },
                new SearchTreeEntry(new GUIContent("Middle Spell", indentationIcon))
                {
                    level = 2, userData = "Middle Spell"
                },
                new SearchTreeEntry(new GUIContent("Comment Block",indentationIcon))
                {
                    level = 1,
                    userData = "Group"
                }
            };

            return tree;
        }
        
        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            //Editor window-based mouse position
            var mousePosition = window.rootVisualElement.ChangeCoordinatesTo(window.rootVisualElement.parent,
                context.screenMousePosition - window.position.position);
            var graphMousePosition = graphView.contentViewContainer.WorldToLocal(mousePosition);
            SpellNode node;
            switch (SearchTreeEntry.userData)
            {
                case "Start Spell":
                    node = SpellNodeFactory.CreateNode(new SpellData(SpellPart.Start, "",""), graphMousePosition);
                    graphView.AddNewSpellNode(node);
                    return true;
                case "Middle Spell":
                    node = SpellNodeFactory.CreateNode(new SpellData(SpellPart.Middle,"",""), graphMousePosition);
                    graphView.AddNewSpellNode(node);
                    return true;
                case "Group":
                    var rect = new Rect(graphMousePosition, graphView.DefaultCommentBlockSize);
                    graphView.CreateCommentBlock(rect);
                    return true;
            }
            return false;
        }
    }
}