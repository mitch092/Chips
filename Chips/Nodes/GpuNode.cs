using System;
using System.Collections.Generic;

namespace Chips.Nodes
{
    public sealed class GpuNode<T> : INode where T : unmanaged
    {
        private readonly Func<T> m_Create;
        private readonly Action<T> m_Destroy;
        private bool m_Dirty = true;
        private T? m_Node = null;
        private List<INode> m_Children = [];

        public GpuNode(Func<T> create, Action<T> destroy, List<INode> parents) 
        {
            m_Create = create;
            m_Destroy = destroy;
            foreach (var parent in parents) 
            {
                parent.AddChild(this);
            }
        }

        public void AddChild(INode child) 
        {
            m_Children.Add(child);
        }

        public void Invalidate() 
        {
            m_Dirty = true;
            foreach (var child in m_Children) 
            {
                child.Invalidate();
            }
        }

        public T Node 
        {
            get 
            {
                if (m_Dirty)
                {
                    if (m_Node != null)
                    {
                        m_Destroy(m_Node.Value);
                        m_Node = null;
                    }
                    m_Dirty = false;
                }
                m_Node ??= m_Create();
                return m_Node.Value;
            }
        }

        public void Dispose() 
        {
            if (m_Node != null) 
            {                
                foreach (var child in m_Children) 
                {
                    child.Dispose();
                }
                m_Destroy(m_Node.Value);
                m_Node = null;
            }
        }
    }
}
