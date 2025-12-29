namespace Chips.Rendering
{
    public static class RenderShader
    {
        public const string Source = @"
@vertex
fn vs_main(@builtin(vertex_index) i : u32) -> @builtin(position) vec4<f32> {
    var p = array<vec2<f32>,3>(
        vec2(-1.0,-3.0),
        vec2( 3.0, 1.0),
        vec2(-1.0, 1.0)
    );
    return vec4(p[i], 0.0, 1.0);
}

@group(0) @binding(0)
var tex : texture_2d<f32>;

@group(0) @binding(1)
var samp : sampler;

@fragment
fn fs_main(@builtin(position) pos : vec4<f32>) -> @location(0) vec4<f32> {
    let dims = vec2<f32>(textureDimensions(tex));
    let uv = pox.xy / dims;
    return testureSample(tex, samp, uv);
}";
    }
}
