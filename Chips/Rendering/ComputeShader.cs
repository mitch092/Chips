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
    let srcV = vec2<u32>(p.srcW, p.srcH);
    let dstV = vec2<u32>(p.dstW, p.dstH);
    let off = vec2<u32>(p.offX, p.offY);
    let id2 = vec2<u32>(id.xy);

    if (any(id2 >= dstV)) {
        return;
    }

    // Integer scaling (pixel-perfect)
    if (p.mode == 0u) {
        let ur = off + (srcV * p.scale);
        if (all(off <= id2) && all(id2 < ur)) {
            let s = (id2 - off) / p.scale;
            let c = textureLoad(src, s, 0);
            textureStore(dst, id2, c);
            return;
        }
        textureStore(dst, id2, vec4<f32>(0.0, 0.0, 0.0, 1.0));
        return;        
    }

    let uv = (vec2<f32>(id2) + vec2(0.5)) / vec2<f32>(dstV);

    // Free nearest
    if (p.mode == 1u) {
        let s = vec2<u32>(uv * vec2<f32>(srcV));
        let c = textureLoad(src, s, 0);
        textureStore(dst, id2, c);
        return;
    }

    // Free linear
    let c = textureSampleLevel(src, samp, uv, 0.0);
    textureStore(dst, id2, c);
}";
    }
}
