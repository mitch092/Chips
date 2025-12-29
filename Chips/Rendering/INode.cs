using System;

namespace Chips.Rendering
{
    public interface INode : IDisposable
    {
        public void AddChild(INode node);
        public void Invalidate();
    }
}
