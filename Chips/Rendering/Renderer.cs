using Chips.Nodes;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.Windowing;
using static Chips.Rendering.RendererUtils;

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
            InputNode<WebGPU> m_Api = new(CreateApi(), FreeApi);

            GpuNode<GpuHandle<Instance>> m_Instance = new(
                () => CreateInstance(m_Api),
                instance => FreeInstance(m_Api, instance),
                [m_Api]);

            GpuNode<GpuHandle<Surface>> m_Surface = new(
                () => CreateSurface(m_Api, m_Window.Node, m_Instance.Node),
                surface => FreeSurface(m_Api, surface),
                [m_Api, m_Window, m_Instance]);

            GpuNode<GpuHandle<Adapter>> m_Adapter = new(
                () => CreateAdapter(m_Api, m_Instance.Node, m_Surface.Node),
                adapter => FreeAdapter(m_Api, adapter),
                [m_Api, m_Instance, m_Surface]);

            GpuNode<GpuHandle<Device>> m_Device = new(
                () => CreateDevice(m_Api, m_Adapter.Node),
                device => FreeDevice(m_Api, device),
                [m_Api, m_Adapter]);

            GpuNode<GpuHandle<Queue>> m_Queue = new(
                () => CreateQueue(m_Api, m_Device.Node),
                queue => FreeQueue(m_Api, queue),
                [m_Api, m_Device]);

            GpuNode<GpuHandle<SurfaceConfiguration>> m_SurfaceConfiguration = new(
                () => CreateSurfaceConfiguration(m_Api, m_Device.Node, m_Surface.Node, m_SurfaceSize),
                surfaceConfiguration => FreeSurfaceConfiguration(surfaceConfiguration),
                [m_Api, m_Device, m_Surface, m_SurfaceSize]);

            GpuNode<GpuHandle<uint>> m_Framebuffer = new(
                () => CreateFramebuffer(m_FramebufferSize),
                framebuffer => FreeFramebuffer(framebuffer),
                [m_FramebufferSize]);

            //GpuNode<GpuHandle<Texture>> m_SourceTexture = new(() => CreateSourceTexture(), sourceTexture => );
        }
    }
}
