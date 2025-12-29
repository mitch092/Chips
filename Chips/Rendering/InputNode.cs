using System.Collections.Generic;

namespace Chips.Rendering
{
    public sealed class InputNode<T> : INode
    {
        private T m_Node;
        private List<INode> m_Children = [];

        public InputNode(T node)
        {
            m_Node = node;
        }

        public T Node
        {
            set
            {
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
        }
    }
}
