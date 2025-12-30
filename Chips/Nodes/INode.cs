using System;

namespace Chips.Nodes
{
    public interface INode : IDisposable
    {
        public void AddChild(INode node);
        public void Invalidate();
    }
}
