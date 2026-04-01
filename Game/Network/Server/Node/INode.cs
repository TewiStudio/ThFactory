/*using Tewi.Game.Network.Core;
using System;
using System.Collections.Generic;

namespace Tewi.Game.Network.Server.Node
{
    public interface INode
    {
        public NodeState CurrentState { get; }

        internal void SetID(int id);
        internal void SetRecipeID(ushort id);
        internal void OnTick();
    }

    public abstract class Node : INode
    {
        protected NodeState _state;
        NodeState INode.CurrentState => _state;

        void INode.SetID(int id)
        {
            NodeState nodeState = _state;
            nodeState.internalIndex = id;
            _state = nodeState;
        }

        void INode.SetRecipeID(ushort id)
        {
            NodeState nodeState = _state;
            nodeState.recipeId = id;
            _state = nodeState;
        }

        void INode.OnTick()
        {

        }
    }

    public class Processor : Node
    {
        public Processor()
        {
        }
    }

    public static class NodeFactory
    {
        private static Dictionary<ushort, Func<INode>> _creators = new();

        public static void Register(ushort type, Func<INode> creator)
        {
            _creators[type] = creator;
        }

        public static INode Create(ushort type)
        {
            if (_creators.TryGetValue(type, out var creator))
                return creator();

            throw new Exception($"Unknown node type: {type}");
        }
    }
}*/