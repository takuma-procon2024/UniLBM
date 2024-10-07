using Unity.Mathematics;

namespace UniLbm.Lbm.Extension
{
    /// <summary>
    ///     ToFセンサのデータを反映するためのインターフェース
    /// </summary>
    public interface ILbmToFSensor
    {
        /// <summary>
        ///     ToFセンサオブジェクトのUnity空間における位置
        /// </summary>
        public float3 Position { get; }

        /// <summary>
        ///     ToFセンサオブジェクトのUnity空間における向き
        /// </summary>
        public float3 Direction { get; }

        /// <summary>
        ///     ToFセンサから取得した値
        /// </summary>
        /// <remarks>distanceが負なら計算に寄与しない</remarks>
        public float Distance { get; }
    }
}