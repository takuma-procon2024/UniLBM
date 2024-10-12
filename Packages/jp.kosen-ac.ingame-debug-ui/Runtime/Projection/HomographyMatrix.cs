using Unity.Burst;
using Unity.Mathematics;

namespace Projection
{
    public static class HomographyMatrix
    {
        [BurstCompile]
        public static float4x4 CalcHomographyMatrix(in float2 p00, in float2 p01, in float2 p10, in float2 p11)
        {
            // Src が未知数でないことを利用して解くと簡単に解ける

            var x00 = p00.x;
            var y00 = p00.y;
            var x01 = p01.x;
            var y01 = p01.y;
            var x10 = p10.x;
            var y10 = p10.y;
            var x11 = p11.x;
            var y11 = p11.y;

            var a = x10 - x11;
            var b = x01 - x11;
            var c = x00 - x01 - x10 + x11;
            var d = y10 - y11;
            var e = y01 - y11;
            var f = y00 - y01 - y10 + y11;

            var h32 = (c * d - a * f) / (b * d - a * e);
            var h31 = (c * e - b * f) / (a * e - b * d);
            var h11 = x10 - x00 + h31 * x10;
            var h12 = x01 - x00 + h32 * x01;
            var h21 = y10 - y00 + h31 * y10;
            var h22 = y01 - y00 + h32 * y01;

            return new float4x4(
                h11, h12, x00, 0,
                h21, h22, y00, 0,
                h31, h32, 1, 0,
                0, 0, 0, 1
            );
        }
    }
}