uint get_index(uint3 index, uint3 size)
{
    return index.x + index.y * size.x + index.z * size.x * size.y;
}

bool is_in_wall(uint3 index, uint3 size)
{
    return index.x == 0 || index.x == size.x - 1 || index.y == 0 || index.y == size.y - 1 || index.z == 0 || index.z ==
        size.z - 1;
}

float3 hsv_2_rgb(float3 hsv)
{
    float3 rgb;

    if (hsv.y == 0)
    {
        // S（彩度）が0と等しいならば無色もしくは灰色
        rgb.r = rgb.g = rgb.b = hsv.z;
    }
    else
    {
        // 色環のH（色相）の位置とS（彩度）、V（明度）からRGB値を算出する
        hsv.x *= 6.0;
        float i = floor(hsv.x);
        float f = hsv.x - i;
        float aa = hsv.z * (1 - hsv.y);
        float bb = hsv.z * (1 - (hsv.y * f));
        float cc = hsv.z * (1 - (hsv.y * (1 - f)));
        if (i < 1)
        {
            rgb.r = hsv.z;
            rgb.g = cc;
            rgb.b = aa;
        }
        else if (i < 2)
        {
            rgb.r = bb;
            rgb.g = hsv.z;
            rgb.b = aa;
        }
        else if (i < 3)
        {
            rgb.r = aa;
            rgb.g = hsv.z;
            rgb.b = cc;
        }
        else if (i < 4)
        {
            rgb.r = aa;
            rgb.g = bb;
            rgb.b = hsv.z;
        }
        else if (i < 5)
        {
            rgb.r = cc;
            rgb.g = aa;
            rgb.b = hsv.z;
        }
        else
        {
            rgb.r = hsv.z;
            rgb.g = aa;
            rgb.b = bb;
        }
    }
    return rgb;
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

float length2(in float3 v)
{
    return dot(v, v);
}