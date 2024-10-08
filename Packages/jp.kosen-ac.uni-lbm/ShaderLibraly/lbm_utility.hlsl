#define FLUID_TYPE 0
#define OUTFLOW_BOUNDARY_TYPE 2

bool is_cloth_boundary(in uint field_val)
{
    return (field_val & 0x80000000) != 0;
}

struct lbm_particle_data
{
    // XYZ: Position, W: Lifetime
    float4 pos_lifetime;
    // XYZ: Prev Position, W: VelLength
    float4 prev_pos_vel;
};

uint get_index(uint3 index, uint3 size)
{
    return index.x + index.y * size.x + index.z * size.x * size.y;
}

float3 hsv_2_rgb(float3 hsv)
{
    static const float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(hsv.xxx + k.xyz) * 6.0 - k.www);
    return hsv.z * lerp(k.xxx, clamp(p - k.xxx, 0.0, 1.0), hsv.y);
}

float random1(float2 co)
{
    return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
}

float3 random3(float3 s)
{
    return frac(
        sin(float3(
            dot(s, float3(127.1, 311.7, 524.3)),
            dot(s, float3(513.4, 124.1, 153.1)),
            dot(s, float3(269.5, 183.3, 536.1))
        )) * 43758.5453123
    );
}

float length_sq(in float3 v)
{
    return dot(v, v);
}

float length_sq(in float2 v)
{
    return dot(v, v);
}
