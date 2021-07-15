using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UniJulius.Runtime;
using UnityEngine.UIElements;

namespace UniJulius.Editor
{
    public class GraphSaveUtility
    {
        private List<Edge> Edges => graphView.edges.ToList();
        private List<SpellNode> Nodes => graphView.nodes.ToList().Cast<SpellNode>().ToList();

        private List<Group> CommentBlocks =>
            graphView.graphElements.ToList().Where(x => x is Group).Cast<Group>().ToList();

        private SpellContainer spellContainer;
        private SpellGraphView graphView;

        public static GraphSaveUtility GetInstance(SpellGraphView graphView)
        {
            return new GraphSaveUtility
            {
                graphView = graphView
            };
        }

        public void SaveGraph(string filePath)
        {
            var spellContainerObject = ScriptableObject.CreateInstance<SpellContainer>();
            if (!SaveNodes(filePath, spellContainerObject)) return;

            SaveExposedProperties(spellContainerObject);
            SaveCommentBlocks(spellContainerObject);
            
            var loadedAsset = AssetDatabase.LoadAssetAtPath(filePath, typeof(SpellContainer));

            if (loadedAsset == null || !AssetDatabase.Contains(loadedAsset)) 
			{
                AssetDatabase.CreateAsset(spellContainerObject, filePath);
            }
            else 
			{
                SpellContainer container = loadedAsset as SpellContainer;
                container.NodeLinks = new List<NodeLinkData>(spellContainerObject.NodeLinks);
                container.SpellNodeData = new List<SpellNodeData>(spellContainerObject.SpellNodeData);
                container.ExposedProperties = new List<ExposedProperty>(spellContainerObject.ExposedProperties);
                container.CommentBlockData = new List<CommentBlockData>(spellContainerObject.CommentBlockData);
                container.FillerData = spellContainerObject.FillerData;
                EditorUtility.SetDirty(container);
            }

            AssetDatabase.SaveAssets();
        }

        private bool SaveNodes(string filePath, SpellContainer spellContainerObject)
        {
            if (Edges.Any())
            {
                var connectedSockets = Edges.Where(x => x.input.node != null).ToArray();
                for (var i = 0; i < connectedSockets.Count(); i++)
                {
                    var outputNode = (connectedSockets[i].output.node as SpellNode);
                    var inputNode = (connectedSockets[i].input.node as SpellNode);
                    spellContainerObject.NodeLinks.Add(new NodeLinkData
                    {
                        BaseNodeGuid = outputNode.Guid,
                        PortName = connectedSockets[i].output.portName,
                        TargetNodeGuid = inputNode.Guid
                    });
                }
            }

            foreach (var node in Nodes)
            {
                spellContainerObject.SpellNodeData.Add(new SpellNodeData
                {
                    NodeGuid = node.Guid,
                    SpellData = new SpellData(node.Part,node.Spell,node.Kana,node.RecogSize),
                    Position = node.GetPosition().position,
                });
            }

            if (graphView.FillerField.value != null)
            {
                spellContainerObject.FillerData = graphView.FillerField.value as FillerData;
            }

            return true;
        }

        private void SaveExposedProperties(SpellContainer dialogueContainer)
        {
            dialogueContainer.ExposedProperties.Clear();
            dialogueContainer.ExposedProperties.AddRange(graphView.ExposedProperties);
        }

        private void SaveCommentBlocks(SpellContainer dialogueContainer)
        {
            foreach (var block in CommentBlocks)
            {
                var nodes = block.containedElements.Where(x => x is SpellNode).Cast<SpellNode>().Select(x => x.Guid)
                    .ToList();

                dialogueContainer.CommentBlockData.Add(new CommentBlockData
                {
                    ChildNodes = nodes,
                    Title = block.title,
                    Position = block.GetPosition().position
                });
            }
        }

        public void LoadNarrative(string filePath)
        {
            spellContainer = Resources.Load<SpellContainer>("SpellContainers/"+filePath);

            if (spellContainer == null)
            {
                EditorUtility.DisplayDialog("File Not Found", "Target Narrative Data does not exist!", "OK");
                return;
            }

            ClearGraph();
            AttachFillerData();
            GenerateSpellNodes();
            ConnectSpellNodes();
            AddExposedProperties();
            GenerateCommentBlocks();
        }

        /// <summary>
        /// Set Entry point GUID then Get All Nodes, remove all and their edges. Leave only the entrypoint node. (Remove its edge too)
        /// </summary>
        private void ClearGraph()
        {
            Nodes.Find(x => x.Part == SpellPart.Last).Reset();
            foreach (var perNode in Nodes)
            {
                Edges.Where(x => x.input.node == perNode).ToList()
                    .ForEach(edge => graphView.RemoveElement(edge));
                if (perNode.Part == SpellPart.Last) continue;
                graphView.RemoveElement(perNode);
            }
        }

        private void AttachFillerData()
        {
            if (spellContainer.FillerData != null)
                graphView.FillerField.value = spellContainer.FillerData;

        }

        /// <summary>
        /// Create All serialized nodes and assign their guid and dialogue text to them
        /// </summary>
        private void GenerateSpellNodes()
        {
            var lastData = spellContainer.SpellNodeData.Find(x => x.SpellData.part == SpellPart.Last);
            var lastNode = Nodes.Find(x => x.Part == SpellPart.Last);
            lastNode.CopyNodeData(lastData);
     
            foreach (var perNode in spellContainer.SpellNodeData)
            {
                if (perNode.SpellData.part == SpellPart.Last) continue;
                var spellData = new SpellData(perNode.SpellData.part,perNode.SpellData.spell,perNode.SpellData.kana);
                var tempNode = SpellNodeFactory.CreateNode(spellData, perNode.Position);
                tempNode.Guid = perNode.NodeGuid;
                graphView.AddElement(tempNode);
            }
        }

        private void ConnectSpellNodes()
        {
            for (var i = 0; i < Nodes.Count; i++)
            {
                var k = i; //Prevent access to modified closure
                var connections = spellContainer.NodeLinks.Where(x => x.BaseNodeGuid == Nodes[k].Guid).ToList();
                for (var j = 0; j < connections.Count(); j++)
                {
                    var targetNodeGuid = connections[j].TargetNodeGuid;
                    var targetNode = Nodes.First(x => x.Guid == targetNodeGuid);
                    LinkNodesTogether(Nodes[i].outputContainer[j].Q<Port>(), (Port) targetNode.inputContainer[0]);

                    targetNode.SetPosition(new Rect(
                        spellContainer.SpellNodeData.First(x => x.NodeGuid == targetNodeGuid).Position,
                        SpellNodeFactory.DefaultNodeSize));
                }
            }
        }

        private void LinkNodesTogether(Port outputSocket, Port inputSocket)
        {
            var tempEdge = new Edge()
            {
                output = outputSocket,
                input = inputSocket
            };
            tempEdge?.input.Connect(tempEdge);
            tempEdge?.output.Connect(tempEdge);
            graphView.Add(tempEdge);
        }

        private void AddExposedProperties()
        {
            graphView.ClearBlackBoardAndExposedProperties();
            foreach (var exposedProperty in spellContainer.ExposedProperties)
            {
                graphView.AddPropertyToBlackBoard(exposedProperty);
            }
        }

        private void GenerateCommentBlocks()
        {
            foreach (var commentBlock in CommentBlocks)
            {
                graphView.RemoveElement(commentBlock);
            }

            foreach (var commentBlockData in spellContainer.CommentBlockData)
            {
               var block = graphView.CreateCommentBlock(new Rect(commentBlockData.Position, graphView.DefaultCommentBlockSize),
                    commentBlockData);
               block.AddElements(Nodes.Where(x=>commentBlockData.ChildNodes.Contains(x.Guid)));
            }
        }
    }
}