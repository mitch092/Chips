using Chips.Nodes;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.Windowing;

namespace Chips.Rendering
{
    public class Renderer
    {
        public unsafe Renderer(IWindow window, Vector2D<uint> framebufferSize)
        {
            Vector2D<uint> surfaceSize = new((uint)window.Size.X, (uint)window.Size.Y);

            InputNode<Vector2D<uint>> m_SurfaceSize = new(surfaceSize, _ => { });
            InputNode<Vector2D<uint>> m_FramebufferSize = new(framebufferSize, _ => { });
            InputNode<ScalingMode> m_ScalingMode = new(ScalingMode.FreeNearest, _ => { });

            InputNode<IWindow> m_Window = new(window, _ => { });
            InputNode<string> m_ComputeShader = new(ComputeShader.Source, _ => { });
            InputNode<string> m_RenderShader = new(RenderShader.Source, _ => { });

            InputNode<WebGPU> m_Api = new(RendererUtils.CreateApi(), RendererUtils.FreeApi);

            GpuNode<GpuHandle<Instance>> m_Instance = new(
                () => RendererUtils.CreateInstance(m_Api), 
                instance => RendererUtils.FreeInstance(m_Api, instance), 
                [m_Api]);

            GpuNode<GpuHandle<Surface>> m_Surface = new(
                () => RendererUtils.CreateSurface(m_Api, m_Window.Node, m_Instance.Node),
                surface => RendererUtils.FreeSurface(m_Api, surface),
                [m_Api, m_Window, m_Instance]);

            GpuNode<GpuHandle<Adapter>> m_Adapter = new(
                () => RendererUtils.CreateAdapter(m_Api, m_Instance.Node, m_Surface.Node), 
                adapter => RendererUtils.FreeAdapter(m_Api, adapter), 
                [m_Api, m_Instance, m_Surface]);

            GpuNode<GpuHandle<Device>> m_Device = new(
                () => RendererUtils.CreateDevice(m_Api, m_Adapter.Node), 
                device => RendererUtils.FreeDevice(m_Api, device), 
                [m_Api, m_Adapter]);

            GpuNode<GpuHandle<Queue>> m_Queue = new(
                () => RendererUtils.CreateQueue(m_Api, m_Device.Node),
                queue => RendererUtils.FreeQueue(m_Api, queue),
                [m_Api, m_Device]);
        }
    }
}
