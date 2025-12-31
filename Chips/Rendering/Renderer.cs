using Chips.Nodes;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chips.Rendering
{
    public class Renderer
    {
        public Renderer(IWindow window, Vector2D<uint> framebufferSize) 
        {
            Vector2D<uint> surfaceSize = new((uint)window.Size.X, (uint)window.Size.Y);

            InputNode<Vector2D<uint>> m_SurfaceSize = new(surfaceSize, _ => { });
            InputNode<Vector2D<uint>> m_FramebufferSize = new(framebufferSize, _ => { });
            InputNode<ScalingMode> m_ScalingMode = new(ScalingMode.FreeNearest, _ => { });

            InputNode<IWindow> m_Window = new(window, _ => { });
            InputNode<string> m_ComputeShader = new(ComputeShader.Source, _ => { });
            InputNode<string> m_RenderShader = new(RenderShader.Source, _ => { });

            InputNode<WebGPU> m_Api = new(RendererUtils.CreateApi(), RendererUtils.FreeApi);
        }
    }
}
