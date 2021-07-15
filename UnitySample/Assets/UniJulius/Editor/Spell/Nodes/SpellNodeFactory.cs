using System;
using System.Collections.Generic;
using System.Linq;
using UniJulius.Runtime;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniJulius.Editor
{
    public static class SpellNodeFactory
    {
        public static readonly Vector2 DefaultNodeSize = new Vector2(200, 150);

        private static Port GetPortInstance(
            SpellNode node,
            Direction nodeDirection,
            Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Horizontal, nodeDirection, capacity, typeof(float));
        }
        
//        private static void RemovePort(Node node, Port socket)
//        {
//            var targetEdge = edges.ToList()
//                .Where(x => x.output.portName == socket.portName && x.output.node == socket.node);
//            if (targetEdge.Any())
//            {
//                var edge = targetEdge.First();
//                edge.input.Disconnect(edge);
//                RemoveElement(targetEdge.First());
//            }
//
//            node.outputContainer.Remove(socket);
//            node.RefreshPorts();
//            node.RefreshExpandedState();
//        }

        public static SpellNode GetLastNodeInstance()
        {
            var nodeCache = new SpellNode(new SpellData(SpellPart.Last,"", ""))
            {
                title = "Last",
                Guid = Guid.NewGuid().ToString(),
            };

            var generatePort = GetPortInstance(nodeCache, Direction.Input, Port.Capacity.Multi);
            generatePort.portName = "In";
            nodeCache.InputPort = generatePort;
            nodeCache.inputContainer.Add(generatePort);

//            nodeCache.capabilities &= ~Capabilities.Movable;
            nodeCache.capabilities &= ~Capabilities.Deletable;
            
            nodeCache.RefreshExpandedState();
            nodeCache.RefreshPorts();
            nodeCache.SetPosition(new Rect(600,200,100,150));
            return nodeCache;
        }
        
        public static SpellNode GetEntryPointNodeInstance()
        {
            var nodeCache = new SpellNode(new SpellData(SpellPart.Start,"", ""))
            {
                title = "Start",
                Guid = Guid.NewGuid().ToString(),
            };

            var generatePort = GetPortInstance(nodeCache, Direction.Output, Port.Capacity.Multi);
            generatePort.portName = "Next";
            nodeCache.OutputPort = generatePort;
            nodeCache.outputContainer.Add(generatePort);

//            nodeCache.capabilities &= ~Capabilities.Movable;
            nodeCache.capabilities &= ~Capabilities.Deletable;
            
            nodeCache.RefreshExpandedState();
            nodeCache.RefreshPorts();
            nodeCache.SetPosition(new Rect(400,200,100,150));
            return nodeCache;
        }


        
        public static SpellNode CreateNode(SpellData spellData, Vector2 position)
        {
            var tempSpellNode = new SpellNode(spellData)
            {
                title = spellData.part.ToString(),
                Guid = Guid.NewGuid().ToString(),
            };
            tempSpellNode.styleSheets.Add(Resources.Load<StyleSheet>("Node"));
            
            if (spellData.part == SpellPart.Last || spellData.part == SpellPart.Middle)
            {
                var inputPort = GetPortInstance(tempSpellNode, Direction.Input, Port.Capacity.Multi);
                inputPort.portName = "In";
                tempSpellNode.InputPort = inputPort;
                tempSpellNode.inputContainer.Add(inputPort);
            }

            if (spellData.part == SpellPart.Start || spellData.part == SpellPart.Middle)
            {
                var outputPort = GetPortInstance(tempSpellNode, Direction.Output, Port.Capacity.Multi);
                outputPort.portName = "Out";
                tempSpellNode.OutputPort = outputPort;
                tempSpellNode.outputContainer.Add(outputPort);
            }
            tempSpellNode.RefreshExpandedState();
            tempSpellNode.RefreshPorts();
            tempSpellNode.SetPosition(new Rect(position,
                DefaultNodeSize));

            return tempSpellNode;
        }
    }
}