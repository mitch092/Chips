using Chips.Nodes;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.Windowing;
using System;
using static Chips.Rendering.RendererUtils;
using Buffer = Silk.NET.WebGPU.Buffer;

namespace Chips.Rendering
{
    public class Renderer
    {
        private readonly InputNode<IWindow> m_Window;
        private readonly InputNode<WebGPU> m_Api;
        private readonly InputNode<string> m_ComputeShaderSource;
        private readonly InputNode<string> m_RenderShaderSource;
        private readonly InputNode<Vector2D<uint>> m_SurfaceSize;
        private readonly InputNode<Vector2D<uint>> m_FramebufferSize;
        private readonly InputNode<ScalingMode> m_ScalingMode;
        private readonly GpuNode<GpuHandle<Instance>> m_Instance;
        private readonly GpuNode<GpuHandle<Surface>> m_Surface;
        private readonly GpuNode<GpuHandle<Adapter>> m_Adapter;
        private readonly GpuNode<GpuHandle<Device>> m_Device;
        private readonly GpuNode<GpuHandle<Queue>> m_Queue;
        private readonly GpuNode<GpuHandle<SurfaceConfiguration>> m_SurfaceConfiguration;
        private readonly GpuNode<GpuHandle<uint>> m_Framebuffer;
        private readonly GpuNode<GpuHandle<Texture>> m_SourceTexture;
        private readonly GpuNode<GpuHandle<TextureView>> m_SourceTextureView;
        private readonly GpuNode<GpuHandle<Texture>> m_ScaledTexture;
        private readonly GpuNode<GpuHandle<TextureView>> m_ScaledTextureView;
        private readonly GpuNode<GpuHandle<Sampler>> m_Sampler;
        private readonly GpuNode<GpuHandle<Buffer>> m_ParamsBuffer;
        private readonly GpuNode<GpuHandle<ShaderModule>> m_ComputeShaderModule;
        private readonly GpuNode<GpuHandle<ShaderModule>> m_RenderShaderModule;
        private readonly GpuNode<GpuHandle<ComputePipeline>> m_ComputePipeline;
        private readonly GpuNode<GpuHandle<RenderPipeline>> m_RenderPipeline;
        private readonly GpuNode<GpuHandle<BindGroup>> m_ComputeBindGroup;
        private readonly GpuNode<GpuHandle<BindGroup>> m_RenderBindGroup;
        private readonly GpuNode<GpuHandle<ScaleParams>> m_ScaleParams;

        public unsafe Renderer(IWindow window, Vector2D<uint> framebufferSize)
        {
            Vector2D<uint> surfaceSize = new((uint)window.Size.X, (uint)window.Size.Y);

            m_Api = new(CreateApi(), FreeApi);
            m_Window = new(window, _ => { });
            m_ComputeShaderSource = new(ComputeShader.Source, _ => { });
            m_RenderShaderSource = new(RenderShader.Source, _ => { });
            m_SurfaceSize = new(surfaceSize, _ => { });
            m_FramebufferSize = new(framebufferSize, _ => { });
            m_ScalingMode = new(ScalingMode.FreeNearest, _ => { });

            m_Instance = new(
                () => CreateInstance(m_Api),
                instance => FreeInstance(m_Api, instance),
                [m_Api]);

            m_Surface = new(
                () => CreateSurface(m_Api, m_Window.Node, m_Instance.Node),
                surface => FreeSurface(m_Api, surface),
                [m_Api, m_Window, m_Instance]);

            m_Adapter = new(
                () => CreateAdapter(m_Api, m_Instance.Node, m_Surface.Node),
                adapter => FreeAdapter(m_Api, adapter),
                [m_Api, m_Instance, m_Surface]);

            m_Device = new(
                () => CreateDevice(m_Api, m_Adapter.Node),
                device => FreeDevice(m_Api, device),
                [m_Api, m_Adapter]);

            m_Queue = new(
                () => CreateQueue(m_Api, m_Device.Node),
                queue => FreeQueue(m_Api, queue),
                [m_Api, m_Device]);

            m_SurfaceConfiguration = new(
                () => CreateSurfaceConfiguration(m_Api, m_Device.Node, m_Surface.Node, m_SurfaceSize),
                surfaceConfiguration => FreeSurfaceConfiguration(surfaceConfiguration),
                [m_Api, m_Device, m_Surface, m_SurfaceSize]);

            m_Framebuffer = new(
                () => CreateFramebuffer(m_FramebufferSize),
                framebuffer => FreeFramebuffer(framebuffer),
                [m_Api, m_FramebufferSize]);

            m_SourceTexture = new(
                () => CreateSourceTexture(m_Api, m_Device.Node, m_FramebufferSize),
                sourceTexture => FreeTexture(m_Api, sourceTexture),
                [m_Api, m_Device, m_FramebufferSize]);

            m_SourceTextureView = new(
                () => CreateTextureView(m_Api, m_SourceTexture.Node),
                sourceTextureView => FreeTextureView(m_Api, sourceTextureView),
                [m_Api, m_SourceTexture]);

            m_ScaledTexture = new(
                () => CreateScaledTexture(m_Api, m_Device.Node, m_SurfaceConfiguration.Node),
                scaledTexture => FreeTexture(m_Api, scaledTexture),
                [m_Api, m_Device, m_SurfaceConfiguration]);

            m_ScaledTextureView = new(
                () => CreateTextureView(m_Api, m_ScaledTexture.Node),
                scaledTextureView => FreeTextureView(m_Api, scaledTextureView),
                [m_Api, m_ScaledTexture]);

            m_Sampler = new(
                () => CreateSampler(m_Api, m_Device.Node),
                sampler => FreeSampler(m_Api, sampler),
                [m_Api, m_Device]);

            m_ParamsBuffer = new(
                () => CreateParamsBuffer(m_Api, m_Device.Node),
                paramsBuffer => FreeParamsBuffer(m_Api, paramsBuffer),
                [m_Api, m_Device]);

            m_ComputeShaderModule = new(
                () => CreateShaderModule(m_Api, m_Device.Node, m_ComputeShaderSource),
                computeShaderModule => FreeShaderModule(m_Api, computeShaderModule),
                [m_Api, m_Device, m_ComputeShaderSource]);

            m_RenderShaderModule = new(
                () => CreateShaderModule(m_Api, m_Device.Node, m_RenderShaderSource),
                renderShaderModule => FreeShaderModule(m_Api, renderShaderModule),
                [m_Api, m_Device, m_RenderShaderSource]);

            m_ComputePipeline = new(
                () => CreateComputePipeline(m_Api, m_Device.Node, m_ComputeShaderModule.Node),
                computePipeline => FreeComputePipeline(m_Api, computePipeline),
                [m_Api, m_Device, m_ComputeShaderModule]);

            m_RenderPipeline = new(
                () => CreateRenderPipeline(m_Api, m_Device.Node, m_RenderShaderModule.Node),
                renderPipeline => FreeRenderPipeline(m_Api, renderPipeline),
                [m_Api, m_Device, m_RenderShaderModule]);

            m_ComputeBindGroup = new(
                () => CreateComputeBindGroup(m_Api, m_Device.Node, m_ComputePipeline.Node, m_SourceTextureView.Node, m_ScaledTextureView.Node, m_ParamsBuffer.Node, m_Sampler.Node),
                computeBindGroup => FreeBindGroup(m_Api, computeBindGroup),
                [m_Api, m_Device, m_ComputePipeline, m_SourceTextureView, m_ScaledTextureView, m_ParamsBuffer, m_Sampler]);

            m_RenderBindGroup = new(
                () => CreateRenderBindGroup(m_Api, m_Device.Node, m_RenderPipeline.Node, m_ScaledTextureView.Node, m_Sampler.Node),
                renderBindGroup => FreeBindGroup(m_Api, renderBindGroup),
                [m_Api, m_Device, m_RenderPipeline, m_ScaledTextureView, m_Sampler]);

            m_ScaleParams = new(
                () => CreateScaleParams(m_ScalingMode, m_SurfaceConfiguration.Node, m_FramebufferSize),
                scaleParams => FreeScaleParams(scaleParams),
                [m_Api, m_ScalingMode, m_SurfaceConfiguration, m_FramebufferSize]);

            m_Window.Node.Closing += OnClose;
        }

        private void OnClose()
        {
            m_Window.Node.Closing -= OnClose;
            m_Api.Dispose();
        }

        public unsafe void Present()
        {
            Render(m_Api, m_Surface.Node, m_Device.Node, m_Queue.Node, m_ParamsBuffer.Node, m_ScaleParams.Node, m_Framebuffer.Node, m_SourceTexture.Node,
                m_ComputePipeline.Node, m_ComputeBindGroup.Node, m_RenderPipeline.Node, m_RenderBindGroup.Node);
        }

        public Vector2D<uint> Size => m_FramebufferSize;

        public unsafe Span<uint> Framebuffer
        {
            get 
            {
                Vector2D<uint> size = Size;
                int sizeBytes = (int)(size.X * size.Y * sizeof(uint)); 
                return new(m_Framebuffer.Node, sizeBytes);
            }
        }
    }
}
