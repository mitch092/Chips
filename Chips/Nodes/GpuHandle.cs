namespace Chips.Nodes
{
    public readonly unsafe struct GpuHandle<T>(T* ptr) where T : unmanaged
    {
        public readonly T* Ptr = ptr;
        public static implicit operator T*(GpuHandle<T> h) => h.Ptr;
        public static implicit operator GpuHandle<T>(T* p) => new(p);
    }
}
