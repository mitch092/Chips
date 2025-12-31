using System;
using System.Collections.Generic;

namespace Chips.Nodes
{
    public sealed class InputNode<T> : INode
    {
        private T m_Node;
        private readonly Action<T> m_Destroy;
        private readonly List<INode> m_Children = [];

        public InputNode(T node, Action<T> destroy)
        {
            m_Node = node;
            m_Destroy = destroy;
        }

        public static implicit operator T(InputNode<T> inputNode) => inputNode.Node;

        public T Node
        {
            get => m_Node;
            set
            {
                m_Destroy(m_Node);
                m_Node = value;
                Invalidate();
            }
        }

        public void Invalidate()
        {
            foreach (var child in m_Children)
            {
                child.Invalidate();
            }
        }

        public void AddChild(INode child)
        {
            m_Children.Add(child);
        }

        public void Dispose()
        {
            foreach (var child in m_Children)
            {
                child.Dispose();
            }
            m_Destroy(m_Node);
        }
    }
}
