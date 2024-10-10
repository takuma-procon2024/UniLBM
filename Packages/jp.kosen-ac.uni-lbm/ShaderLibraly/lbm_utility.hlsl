/**
 * 流体であることを示す境界値
 */
#define FLUID_TYPE 0
/**
 * 流出境界であることを示す境界値
 */
#define OUTFLOW_BOUNDARY_TYPE 2

/**
 * 渡された境界値が布の境界であるかを判定する
 * @param field_val 境界値
 * @return 布境界であるか
 */
bool is_cloth_boundary(in uint field_val)
{
    // MSBが1の場合は布の境界
    return (field_val & 0x80000000) != 0;
}

/**
 * パーティクルデータ
 */
struct lbm_particle_data
{
    // XYZ: Position, W: Lifetime
    float4 pos_lifetime;
    // XYZ: Prev Position, W: VelLength
    float4 prev_pos_vel;
};

/**
 * 3次元インデックスを1次元のインデックスに変換する
 * @param index 3次元インデックス
 * @param size 3次元サイズ
 * @return 1次元インデックス
 */
uint get_index(in uint3 index, in uint3 size)
{
    return index.x + index.y * size.x + index.z * size.x * size.y;
}

/**
 * hsv色空間からrgb色空間に変換する
 * @param hsv HSVパラメータ
 * @return RGBパラメータ
 */
float3 hsv_2_rgb(in float3 hsv)
{
    static const float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(hsv.xxx + k.xyz) * 6.0 - k.www);
    return hsv.z * lerp(k.xxx, clamp(p - k.xxx, 0.0, 1.0), hsv.y);
}

/**
 * 1次元の疑似乱数生成
 * @param co シード
 * @return 生成された疑似乱数
 */
float random1(in float2 co)
{
    return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
}

/**
 * 3次元の疑似乱数生成
 * @param s シード
 * @return 生成された3次元疑似乱数
 */
float3 random3(in float3 s)
{
    return frac(
        sin(float3(
            dot(s, float3(127.1, 311.7, 524.3)),
            dot(s, float3(513.4, 124.1, 153.1)),
            dot(s, float3(269.5, 183.3, 536.1))
        )) * 43758.5453123
    );
}

/**
 * ベクトルの長さの2乗を計算する
 * @param v ベクトル
 * @return ベクトルの長さの2乗
 */
float length_sq(in float3 v)
{
    return dot(v, v);
}

/**
 * ベクトルの長さの2乗を計算する
 * @param v ベクトル
 * @return ベクトルの長さの2乗
 */
float length_sq(in float2 v)
{
    return dot(v, v);
}
