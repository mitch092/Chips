namespace Chips.Rendering
{
    public static class ComputeShader
    {
        public const string Source = @"
struct Params {
    srcW : u32,
    srcH : u32,
    dstW : u32,
    dstH : u32,
    scale : u32,
    offX : u32,
    offY : u32,
    mode : u32,
};

@group(0) @binding(0)
var src : texture_2d<f32>;

@group(0) @binding(1)
var dst : texture_storage_2d<rgba8unorm, write>;

@group(0) @binding(2)
var<uniform> p : Params;

@group(0) @binding(3)
var samp : sampler;

@compute @workgroup_size(8,8)
fn cs_main(@builtin(global_invocation_id) id : vec3<u32>) {
    if (id.x >= p.dstW || id.y >= p.dstH) {
        return;
    }

    // Integer scaling (pixel-perfect)
    if (p.mode == 0u) {
        if (id.x < p.offX || id.y < p.offY ||
            id.x >= p.offX + p.srcW * p.scale ||
            id.y >= p.offY + p.srcH * p.scale) {
            textureStore(dst, vec2<i32>(id.xy), vec4(0,0,0,1));
            return;
        }

        let sx = (id.x - p.offX) / p.scale;
        let sy = (id.y - p.offY) / p.scale;
        let c = textureLoad(src, vec2<i32>(sx, sy), 0);
        textureStore(dst, vec2<i32>(id.xy), c);
        return;
    }

    let uv = (vec2<f32>(id.xy) + vec2(0.5)) / vec2<f32>(p.dstW, p.dstH);

    // Free nearest
    if (p.mode == 1u) {
        let sx = u32(uv.x * f32(p.srcW));
        let sy = u32(uv.y * f32(p.srcH));
        let c = textureLoad(src, vec2<i32>(sx, sy), 0);
        textureStore(dst, vec2<i32>(id.xy), c);
        return;
    }

    // Free linear
    let c = textureSampleLevel(src, samp, uv, 0.0);
    textureStore(dst, vec2<i32>(id.xy), c);
}";
    }
}
