using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Buffer = Silk.NET.WebGPU.Buffer;

namespace Chips.Rendering
{
    public static class Renderer
    {
        public static WebGPU CreateApi()
        {
            return WebGPU.GetApi();
        }

        public unsafe static Instance* CreateInstance(WebGPU api)
        {
            InstanceDescriptor descriptor = new();
            return api.CreateInstance(ref descriptor);
        }

        public unsafe static Surface* CreateSurface(IWindow window, WebGPU api, Instance* instance)
        {
            return window.CreateWebGPUSurface(api, instance);
        }

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
                        string? msg = Marshal.PtrToStringAnsi((IntPtr)msgPtr);
                        Console.WriteLine($"Error whle retrieving WGPU Adapter: {msg}");
                    }
                });
            api.InstanceRequestAdapter(instance, ref options, callback, null);
            tcs.Task.WaitAsync(TimeSpan.FromSeconds(3)).Wait();
            return adapter;
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
                        string? msg = Marshal.PtrToStringAnsi((IntPtr)msgPtr);
                        Console.WriteLine($"Error whle retrieving WGPU Device: {msg}");
                    }
                });

            api.AdapterRequestDevice(adapter, ref descriptor, callback, null);
            tcs.Task.WaitAsync(TimeSpan.FromSeconds(3)).Wait();
            return device;
        }

        public unsafe static Queue* CreateQueue(WebGPU api, Device* device)
        {
            return api.DeviceGetQueue(device);
        }

        public unsafe static void ConfigureSurface(WebGPU api, Device* device, Surface* surface, Vector2D<int> surfaceSize)
        {
            SurfaceConfiguration configuration = new()
            {
                Device = device,
                Format = TextureFormat.Bgra8UnormSrgb,
                Width = (uint)surfaceSize.X,
                Height = (uint)surfaceSize.Y,
                Usage = TextureUsage.RenderAttachment,
                PresentMode = PresentMode.Fifo
            };
            api.SurfaceConfigure(surface, ref configuration);
        }

        public static uint[] CreateFramebuffer(Vector2D<int> framebufferSize)
        {
            return new uint[framebufferSize.X * framebufferSize.Y];
        }

        public unsafe static Texture* CreateSourceTexture(WebGPU api, Device* device, Vector2D<int> framebufferSize)
        {
            TextureDescriptor descriptor = new()
            {
                Size = new Extent3D((uint)framebufferSize.X, (uint)framebufferSize.Y, 1),
                Format = TextureFormat.Rgba8Unorm,
                Usage = TextureUsage.CopyDst | TextureUsage.TextureBinding,
            };
            return api.DeviceCreateTexture(device, ref descriptor);
        }

        public unsafe static TextureView* CreateSourceTextureView(WebGPU api, Texture* texture)
        {
            TextureViewDescriptor descriptor = new()
            {
                Format = TextureFormat.Rgba8Unorm,
                Dimension = TextureViewDimension.Dimension2D,
                BaseMipLevel = 0,
                MipLevelCount = 1,
                BaseArrayLayer = 0,
                ArrayLayerCount = 1,
                Aspect = TextureAspect.All
            };
            return api.TextureCreateView(texture, ref descriptor);
        }

        public unsafe static Texture* CreateScaledTexture(WebGPU api, Device* device, Vector2D<int> surfaceSize)
        {
            TextureDescriptor descriptor = new()
            {
                Size = new Extent3D((uint)surfaceSize.X, (uint)surfaceSize.Y, 1),
                Format = TextureFormat.Rgba8Unorm,
                Usage = TextureUsage.StorageBinding | TextureUsage.TextureBinding,
            };
            return api.DeviceCreateTexture(device, ref descriptor);
        }

        public unsafe static TextureView* CreateScaledTextureView(WebGPU api, Texture* texture)
        {
            TextureViewDescriptor descriptor = new()
            {
                Format = TextureFormat.Rgba8Unorm,
                Dimension = TextureViewDimension.Dimension2D,
                BaseMipLevel = 0,
                MipLevelCount = 1,
                BaseArrayLayer = 0,
                ArrayLayerCount = 1,
                Aspect = TextureAspect.All
            };
            return api.TextureCreateView(texture, ref descriptor);
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

        public unsafe static ShaderModule* CreateShaderModule(WebGPU api, Device* device, string shaderSource)
        {
            ShaderModule* module = null;
            nint shaderPointer = SilkMarshal.StringToPtr(ComputeShader.Source, NativeStringEncoding.UTF8);
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
            nint entryPoint = SilkMarshal.StringToPtr(ComputeShaderEntryPoint, NativeStringEncoding.UTF8);
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
            nint vertexShaderEntryPoint = SilkMarshal.StringToPtr(VertesShaderEntryPoint, NativeStringEncoding.UTF8);
            nint fragmentShaderEntryPoint = SilkMarshal.StringToPtr(FragmentShaderEntryPoint, NativeStringEncoding.UTF8);
            RenderPipeline* renderPipeline;
            try
            {
                ColorTargetState colorTargetState = new()
                {
                    Format = TextureFormat.Bgra8UnormSrgb
                };
                FragmentState fragment = new()
                {
                    Module = renderShaderModule,
                    EntryPoint = (byte*)fragmentShaderEntryPoint,
                    Targets = &colorTargetState,
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
                renderBindGroup = api.DeviceCreateBindGroup(device, &descriptor);
            }
            finally
            {
                api.BindGroupLayoutRelease(renderBindGroupLayout);
            }

            return renderBindGroup;
        }
    }
}
