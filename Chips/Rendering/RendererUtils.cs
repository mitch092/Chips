using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.Windowing;
using System;
using System.Threading.Tasks;
using Buffer = Silk.NET.WebGPU.Buffer;

namespace Chips.Rendering
{
    public static class RendererUtils
    {
        public static WebGPU CreateApi()
        {
            return WebGPU.GetApi();
        }

        public static void FreeApi(WebGPU api)
        {
            api.Dispose();
        }

        public unsafe static Instance* CreateInstance(WebGPU api)
        {
            InstanceDescriptor descriptor = new();
            return api.CreateInstance(ref descriptor);
        }

        public unsafe static void FreeInstance(WebGPU api, Instance* instance)
        {
            api.InstanceRelease(instance);
        }

        public unsafe static Surface* CreateSurface(IWindow window, WebGPU api, Instance* instance)
        {
            return window.CreateWebGPUSurface(api, instance);
        }

        public unsafe static void FreeSurface(WebGPU api, Surface* surface)
        {
            api.SurfaceRelease(surface);
        }
        // TODO: Write Free* functions for the rest of the functions below.
        public unsafe static Adapter* CreateAdapter(WebGPU api, Instance* instance, Surface* surface)
        {
            RequestAdapterOptions options = new()
            {
                CompatibleSurface = surface,
                BackendType = BackendType.WebGpu,
                PowerPreference = PowerPreference.HighPerformance,
            };

            Adapter* adapter = null;
            TaskCompletionSource tcs = new();

            PfnRequestAdapterCallback callback = PfnRequestAdapterCallback.From(
                (status, wgpuAdapter, msgPtr, userDataPtr) =>
                {
                    if (status == RequestAdapterStatus.Success)
                    {
                        adapter = wgpuAdapter;
                        Console.WriteLine("Retrieved WGPU Adapter.");
                        tcs.TrySetResult();
                    }
                    else
                    {
                        string? msg = SilkMarshal.PtrToString((nint)msgPtr);//Marshal.PtrToStringAnsi((IntPtr)msgPtr);
                        Console.WriteLine($"Error whle retrieving WGPU Adapter: {msg}");
                    }
                });
            api.InstanceRequestAdapter(instance, ref options, callback, null);
            tcs.Task.WaitAsync(TimeSpan.FromSeconds(3)).Wait();
            return adapter;
        }

        public unsafe static void FreeAdapter(WebGPU api, Adapter* adapter) 
        {
            api.AdapterRelease(adapter);
        }

        public unsafe static Device* CreateDevice(WebGPU api, Adapter* adapter)
        {
            DeviceDescriptor descriptor = new();

            Device* device = null;
            TaskCompletionSource tcs = new();

            PfnRequestDeviceCallback callback = PfnRequestDeviceCallback.From(
                (status, wgpuDevice, msgPtr, userDataPtr) =>
                {
                    if (status == RequestDeviceStatus.Success)
                    {
                        device = wgpuDevice;
                        Console.WriteLine("Retrieved WGPU Device.");
                    }
                    else
                    {
                        string? msg = SilkMarshal.PtrToString((nint)msgPtr);//Marshal.PtrToStringAnsi((IntPtr)msgPtr);
                        Console.WriteLine($"Error whle retrieving WGPU Device: {msg}");
                    }
                });

            api.AdapterRequestDevice(adapter, ref descriptor, callback, null);
            tcs.Task.WaitAsync(TimeSpan.FromSeconds(3)).Wait();
            return device;
        }

        public unsafe static void FreeDevice(WebGPU api, Device* device) 
        {
            api.DeviceRelease(device);
        }

        public unsafe static Queue* CreateQueue(WebGPU api, Device* device)
        {
            return api.DeviceGetQueue(device);
        }

        public unsafe static void FreeQueue(WebGPU api, Queue* queue) 
        {
            api.QueueRelease(queue);
        }

        public unsafe static SurfaceConfiguration* CreateSurfaceConfiguration(WebGPU api, Device* device, Surface* surface, Vector2D<uint> surfaceSize)
        {
            SurfaceConfiguration* surfaceConfiguration = (SurfaceConfiguration*)SilkMarshal.Allocate(sizeof(SurfaceConfiguration));
            surfaceConfiguration->Device = device;
            surfaceConfiguration->Format = TextureFormat.Bgra8UnormSrgb;
            surfaceConfiguration->Width = surfaceSize.X;
            surfaceConfiguration->Height = surfaceSize.Y;
            surfaceConfiguration->Usage = TextureUsage.RenderAttachment;
            surfaceConfiguration->PresentMode = PresentMode.Fifo;
            api.SurfaceConfigure(surface, surfaceConfiguration);
            return surfaceConfiguration;
        }

        public unsafe static void FreeSurfaceConfiguration(SurfaceConfiguration* surfaceConfiguration)
        {
            SilkMarshal.Free((nint)surfaceConfiguration);
        }

        public unsafe static uint* CreateFramebuffer(Vector2D<uint> framebufferSize)
        {
            return (uint*)SilkMarshal.Allocate((int)(framebufferSize.X * framebufferSize.Y * sizeof(uint)));
        }

        public unsafe static void FreeFramebuffer(uint* framebuffer) 
        {
            SilkMarshal.Free((nint)framebuffer);
        }

        public unsafe static Texture* CreateSourceTexture(WebGPU api, Device* device, Vector2D<uint> framebufferSize)
        {
            TextureDescriptor descriptor = new()
            {
                Size = new Extent3D(framebufferSize.X, framebufferSize.Y, 1),
                Format = TextureFormat.Rgba8Unorm,
                Usage = TextureUsage.CopyDst | TextureUsage.TextureBinding,
                Dimension = TextureDimension.Dimension2D,
                MipLevelCount = 1,
                SampleCount = 1
            };
            return api.DeviceCreateTexture(device, ref descriptor);
        }

        // Use for both source and scaled textures.
        public unsafe static TextureView* CreateTextureView(WebGPU api, Texture* texture)
        {
            return api.TextureCreateView(texture, null);
        }

        public unsafe static Texture* CreateScaledTexture(WebGPU api, Device* device, Vector2D<uint> surfaceSize)
        {
            TextureDescriptor descriptor = new()
            {
                Size = new Extent3D(surfaceSize.X, surfaceSize.Y, 1),
                Format = TextureFormat.Rgba8Unorm,
                Usage = TextureUsage.StorageBinding | TextureUsage.TextureBinding,
                Dimension = TextureDimension.Dimension2D,
                MipLevelCount = 1,
                SampleCount = 1
            };
            return api.DeviceCreateTexture(device, ref descriptor);
        }

        public unsafe static Sampler* CreateSampler(WebGPU api, Device* device)
        {
            SamplerDescriptor descriptor = new()
            {
                MinFilter = FilterMode.Linear,
                MagFilter = FilterMode.Linear,
                AddressModeU = AddressMode.ClampToEdge,
                AddressModeV = AddressMode.ClampToEdge,
            };
            return api.DeviceCreateSampler(device, ref descriptor);
        }

        public unsafe static Buffer* CreateParamsBuffer(WebGPU api, Device* device)
        {
            BufferDescriptor descriptor = new()
            {
                Size = (ulong)sizeof(ScaleParams),
                Usage = BufferUsage.Uniform | BufferUsage.CopyDst,
            };
            return api.DeviceCreateBuffer(device, ref descriptor);
        }

        // Use for both compute shader and render shader.
        public unsafe static ShaderModule* CreateShaderModule(WebGPU api, Device* device, string shaderSource)
        {
            ShaderModule* module = null;
            nint shaderPointer = SilkMarshal.StringToPtr(shaderSource);
            try
            {
                ShaderModuleWGSLDescriptor wgslDescriptor = new()
                {
                    Chain = new ChainedStruct
                    {
                        SType = SType.ShaderModuleWgslDescriptor
                    },
                    Code = (byte*)shaderPointer
                };
                ShaderModuleDescriptor descriptor = new()
                {
                    NextInChain = &wgslDescriptor.Chain
                };
                module = api.DeviceCreateShaderModule(device, ref descriptor);
            }
            finally
            {
                SilkMarshal.Free(shaderPointer);
            }
            return module;
        }

        public const string ComputeShaderEntryPoint = "cs_main";
        public unsafe static ComputePipeline* CreateComputePipeline(WebGPU api, Device* device, ShaderModule* computeShaderModule)
        {
            nint entryPoint = SilkMarshal.StringToPtr(ComputeShaderEntryPoint);
            ComputePipeline* computePipeline;
            try
            {
                ComputePipelineDescriptor descriptor = new()
                {
                    Compute = new()
                    {
                        Module = computeShaderModule,
                        EntryPoint = (byte*)entryPoint
                    }
                };
                computePipeline = api.DeviceCreateComputePipeline(device, ref descriptor);
            }
            finally
            {
                SilkMarshal.Free(entryPoint);
            }
            return computePipeline;
        }

        public const string VertesShaderEntryPoint = "vs_main";
        public const string FragmentShaderEntryPoint = "fs_main";
        public unsafe static RenderPipeline* CreateRenderPipeline(WebGPU api, Device* device, ShaderModule* renderShaderModule)
        {
            nint vertexShaderEntryPoint = SilkMarshal.StringToPtr(VertesShaderEntryPoint);
            nint fragmentShaderEntryPoint = SilkMarshal.StringToPtr(FragmentShaderEntryPoint);
            RenderPipeline* renderPipeline;
            try
            {
                ColorTargetState* colorTargetStates = stackalloc ColorTargetState[1];
                colorTargetStates[0] = new()
                {
                    Format = TextureFormat.Bgra8UnormSrgb
                };

                FragmentState fragment = new()
                {
                    Module = renderShaderModule,
                    EntryPoint = (byte*)fragmentShaderEntryPoint,
                    Targets = colorTargetStates,
                    TargetCount = 1,
                };
                RenderPipelineDescriptor descriptor = new()
                {
                    Vertex = new()
                    {
                        Module = renderShaderModule,
                        EntryPoint = (byte*)vertexShaderEntryPoint
                    },
                    Fragment = &fragment,
                    Primitive = new()
                    {
                        Topology = PrimitiveTopology.TriangleList,
                        CullMode = CullMode.None,
                    }
                };
                renderPipeline = api.DeviceCreateRenderPipeline(device, &descriptor);
            }
            finally
            {
                SilkMarshal.Free(vertexShaderEntryPoint);
                SilkMarshal.Free(fragmentShaderEntryPoint);
            }
            return renderPipeline;
        }

        public unsafe static BindGroup* CreateComputeBindGroup(
            WebGPU api,
            Device* device,
            ComputePipeline* computePipeline,
            TextureView* sourceTextureView,
            TextureView* scaledTextureView,
            Buffer* paramsBuffer,
            Sampler* sampler)
        {
            BindGroupEntry* computeBindGroupEntries = stackalloc BindGroupEntry[4];
            computeBindGroupEntries[0] = new()
            {
                Binding = 0,
                TextureView = sourceTextureView,
            };
            computeBindGroupEntries[1] = new()
            {
                Binding = 1,
                TextureView = scaledTextureView,
            };
            computeBindGroupEntries[2] = new()
            {
                Binding = 2,
                Buffer = paramsBuffer,
                Offset = 0,
                Size = (ulong)sizeof(ScaleParams),
            };
            computeBindGroupEntries[3] = new()
            {
                Binding = 3,
                Sampler = sampler,
            };

            BindGroup* computeBindGroup = null;
            BindGroupLayout* computeBindGroupLayout = api.ComputePipelineGetBindGroupLayout(computePipeline, 0);
            try
            {
                BindGroupDescriptor descriptor = new()
                {
                    Layout = computeBindGroupLayout,
                    Entries = computeBindGroupEntries,
                    EntryCount = 4
                };
                computeBindGroup = api.DeviceCreateBindGroup(device, &descriptor);
            }
            finally
            {
                api.BindGroupLayoutRelease(computeBindGroupLayout);
            }

            return computeBindGroup;
        }

        public unsafe static BindGroup* CreateRenderBindGroup(
            WebGPU api,
            Device* device,
            RenderPipeline* renderPipeline,
            TextureView* scaledTextureView,
            Sampler* sampler)
        {
            BindGroupEntry* renderBindGroupEntries = stackalloc BindGroupEntry[2];
            renderBindGroupEntries[0] = new()
            {
                Binding = 0,
                TextureView = scaledTextureView,
            };
            renderBindGroupEntries[1] = new()
            {
                Binding = 1,
                Sampler = sampler,
            };

            BindGroup* renderBindGroup = null;
            BindGroupLayout* renderBindGroupLayout = api.RenderPipelineGetBindGroupLayout(renderPipeline, 0);
            try
            {
                BindGroupDescriptor descriptor = new()
                {
                    Layout = renderBindGroupLayout,
                    Entries = renderBindGroupEntries,
                    EntryCount = 2
                };
                renderBindGroup = api.DeviceCreateBindGroup(device, ref descriptor);
            }
            finally
            {
                api.BindGroupLayoutRelease(renderBindGroupLayout);
            }

            return renderBindGroup;
        }

        public unsafe static ScaleParams* CreateScaleParams(ScalingMode mode, Vector2D<uint> surfaceSize, Vector2D<uint> framebufferSize)
        {
            uint scale = Math.Max(1u, Math.Min(
                surfaceSize.X / framebufferSize.X,
                surfaceSize.Y / framebufferSize.Y));

            uint scaleW = framebufferSize.X * scale;
            uint scaleH = framebufferSize.Y * scale;

            ScaleParams* scaleParams = (ScaleParams*)SilkMarshal.Allocate(sizeof(ScaleParams));
            scaleParams->SrcW = framebufferSize.X;
            scaleParams->SrcH = framebufferSize.Y;
            scaleParams->DstW = surfaceSize.X;
            scaleParams->DstH = surfaceSize.Y;
            scaleParams->Scale = scale;
            scaleParams->OffX = (surfaceSize.X - scaleW) / 2;
            scaleParams->OffY = (surfaceSize.Y - scaleH) / 2;
            scaleParams->Mode = (uint)mode;

            return scaleParams;
        }

        public unsafe static void Render(
            WebGPU api,
            Surface* surface,
            Device* device,
            Queue* queue,
            Buffer* paramsBuffer,
            ScaleParams* scaleParams,
            uint* framebuffer,
            Vector2D<uint> framebufferSize,
            Texture* sourceTexture,
            ComputePipeline* computePipeline,
            BindGroup* computeBindGroup,
            Vector2D<uint> surfaceSize,
            RenderPipeline* renderPipeline,
            BindGroup* renderBindGroup)
        {
            CommandEncoder* commandEncoder = null;
            ComputePassEncoder* computePassEncoder = null;
            TextureView* backbufferView = null;
            RenderPassEncoder* renderPassEncoder = null;
            CommandBuffer* commandBuffer = null;
            try
            {
                api.QueueWriteBuffer(queue, paramsBuffer, 0, scaleParams, (nuint)sizeof(ScaleParams));

                ImageCopyTexture imageCopyTexture = new()
                {
                    Texture = sourceTexture
                };
                nuint size = framebufferSize.X * framebufferSize.Y * sizeof(uint);
                TextureDataLayout textureDataLayout = new()
                {
                    BytesPerRow = framebufferSize.X * sizeof(uint),
                    RowsPerImage = framebufferSize.Y,
                };
                Extent3D extent3D = new(framebufferSize.X, framebufferSize.Y, 1);
                api.QueueWriteTexture(queue, ref imageCopyTexture, framebuffer, size, ref textureDataLayout, ref extent3D);

                commandEncoder = api.DeviceCreateCommandEncoder(device, null);

                computePassEncoder = api.CommandEncoderBeginComputePass(commandEncoder, null);
                api.ComputePassEncoderSetPipeline(computePassEncoder, computePipeline);
                api.ComputePassEncoderSetBindGroup(computePassEncoder, 0, computeBindGroup, 0, null);
                uint workgroupX = ((surfaceSize.X + 7) / 8);
                uint workgroupY = ((surfaceSize.Y + 7) / 8);
                api.ComputePassEncoderDispatchWorkgroups(computePassEncoder, workgroupX, workgroupY, 1);
                api.ComputePassEncoderEnd(computePassEncoder);

                SurfaceTexture backbuffer = new();
                api.SurfaceGetCurrentTexture(surface, ref backbuffer);
                backbufferView = api.TextureCreateView(backbuffer.Texture, null);
                RenderPassColorAttachment* colorAttachments = stackalloc RenderPassColorAttachment[1];
                colorAttachments[0] = new()
                {
                    View = backbufferView,
                    LoadOp = LoadOp.Clear,
                    StoreOp = StoreOp.Store,
                    ClearValue = new Color(0, 0, 0, 1)
                };
                RenderPassDescriptor renderPassDescriptor = new()
                {
                    ColorAttachmentCount = 1,
                    ColorAttachments = colorAttachments,
                };

                renderPassEncoder = api.CommandEncoderBeginRenderPass(commandEncoder, ref renderPassDescriptor);
                api.RenderPassEncoderSetPipeline(renderPassEncoder, renderPipeline);
                api.RenderPassEncoderSetBindGroup(renderPassEncoder, 0, renderBindGroup, 0, null);
                api.RenderPassEncoderDraw(renderPassEncoder, 3, 1, 0, 0);
                api.RenderPassEncoderEnd(renderPassEncoder);

                commandBuffer = api.CommandEncoderFinish(commandEncoder, null);
                api.QueueSubmit(queue, 1, ref commandBuffer);
                api.SurfacePresent(surface);
            }
            finally
            {
                api.ComputePassEncoderRelease(computePassEncoder);
                api.RenderPassEncoderRelease(renderPassEncoder);
                api.TextureViewRelease(backbufferView);
                api.CommandBufferRelease(commandBuffer);
                api.CommandEncoderRelease(commandEncoder);
            }
        }
    }
}
