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

            InputNode<IWindow> m_Window = new(window, _ => { });
            InputNode<WebGPU> m_Api = new(CreateApi(), FreeApi);
            InputNode<string> m_ComputeShaderSource = new(ComputeShader.Source, _ => { });
            InputNode<string> m_RenderShaderSource = new(RenderShader.Source, _ => { });
            InputNode<Vector2D<uint>> m_SurfaceSize = new(surfaceSize, _ => { });
            InputNode<Vector2D<uint>> m_FramebufferSize = new(framebufferSize, _ => { });
            InputNode<ScalingMode> m_ScalingMode = new(ScalingMode.FreeNearest, _ => { });

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

            GpuNode<GpuHandle<Texture>> m_SourceTexture = new(
                () => CreateSourceTexture(m_Api, m_Device.Node, m_FramebufferSize),
                sourceTexture => FreeTexture(m_Api, sourceTexture),
                [m_Api, m_Device, m_FramebufferSize]);

            GpuNode<GpuHandle<TextureView>> m_SourceTextureView = new(
                () => CreateTextureView(m_Api, m_SourceTexture.Node),
                sourceTextureView => FreeTextureView(m_Api, sourceTextureView),
                [m_Api, m_SourceTexture]);

            GpuNode<GpuHandle<Texture>> m_ScaledTexture = new(
                () => CreateScaledTexture(m_Api, m_Device.Node, m_SurfaceConfiguration.Node),
                scaledTexture => FreeTexture(m_Api, scaledTexture),
                [m_Api, m_Device, m_SurfaceConfiguration]);

            GpuNode<GpuHandle<TextureView>> m_ScaledTextureView = new(
                () => CreateTextureView(m_Api, m_ScaledTexture.Node),
                scaledTextureView => FreeTextureView(m_Api, scaledTextureView),
                [m_Api, m_ScaledTexture]);

            GpuNode<GpuHandle<Sampler>> m_Sampler = new(
                () => CreateSampler(m_Api, m_Device.Node),
                sampler => FreeSampler(m_Api, sampler),
                [m_Api, m_Device]);

            GpuNode<GpuHandle<Buffer>> m_ParamsBuffer = new(
                () => CreateParamsBuffer(m_Api, m_Device.Node),
                paramsBuffer => FreeParamsBuffer(m_Api, paramsBuffer),
                [m_Api, m_Device]);

            GpuNode<GpuHandle<ShaderModule>> m_ComputeShaderModule = new(
                () => CreateShaderModule(m_Api, m_Device.Node, m_ComputeShaderSource),
                computeShaderModule => FreeShaderModule(m_Api, computeShaderModule),
                [m_Api, m_Device, m_ComputeShaderSource]);

            GpuNode<GpuHandle<ShaderModule>> m_RenderShaderModule = new(
                () => CreateShaderModule(m_Api, m_Device.Node, m_RenderShaderSource),
                renderShaderModule => FreeShaderModule(m_Api, renderShaderModule),
                [m_Api, m_Device, m_RenderShaderSource]);

            GpuNode<GpuHandle<ComputePipeline>> m_ComputePipeline = new(
                () => CreateComputePipeline(m_Api, m_Device.Node, m_ComputeShaderModule.Node), 
                computePipeline => FreeComputePipeline(m_Api, computePipeline), 
                [m_Api, m_Device, m_ComputeShaderModule]);

            GpuNode<GpuHandle<RenderPipeline>> m_RenderPipeline = new(
                () => CreateRenderPipeline(m_Api, m_Device.Node, m_RenderShaderModule.Node), 
                renderPipeline => FreeRenderPipeline(m_Api, renderPipeline), 
                [m_Api, m_Device, m_RenderShaderModule]);

            GpuNode<GpuHandle<BindGroup>> m_ComputeBindGroup = new(
                () => CreateComputeBindGroup(m_Api, m_Device.Node, m_ComputePipeline.Node, m_SourceTextureView.Node, m_ScaledTextureView.Node, m_ParamsBuffer.Node, m_Sampler.Node), 
                computeBindGroup => FreeBindGroup(m_Api, computeBindGroup), 
                [m_Api, m_Device, m_ComputePipeline, m_SourceTextureView, m_ScaledTextureView, m_ParamsBuffer, m_Sampler]);

            GpuNode<GpuHandle<BindGroup>> m_RenderBindGroup = new(
                () => CreateRenderBindGroup(m_Api, m_Device.Node, m_RenderPipeline.Node, m_ScaledTextureView.Node, m_Sampler.Node), 
                renderBindGroup => FreeBindGroup(m_Api, renderBindGroup), 
                [m_Api, m_Device, m_RenderPipeline, m_ScaledTextureView, m_Sampler]);

            GpuNode<GpuHandle<ScaleParams>> m_ScaleParams = new(
                () => CreateScaleParams(m_ScalingMode, m_SurfaceConfiguration.Node, m_FramebufferSize), 
                scaleParams => FreeScaleParams(scaleParams), 
                [m_ScalingMode, m_SurfaceConfiguration, m_FramebufferSize]);
        }
    }
}
