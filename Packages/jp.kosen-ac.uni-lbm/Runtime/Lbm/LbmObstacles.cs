using System.Diagnostics.CodeAnalysis;
using UniLbm.Common;
using Unity.Mathematics;
using UnityEngine;

namespace UniLbm.Lbm
{
    /// <summary>
    ///     障害物セルの表示処理を行う
    /// </summary>
    public class LbmObstacles
    {
        private readonly MaterialWrapper<Props> _material;
        private readonly RenderParams _renderParams;
        private readonly int _vertexCount;

        public LbmObstacles(Material material, ILbmSolver lbmSolver, int layer)
        {
            _material = new MaterialWrapper<Props>(material);
            _vertexCount = lbmSolver.CellRes * lbmSolver.CellRes * lbmSolver.CellRes;

            var matSize = _material.GetFloat(Props.size);
            _renderParams = new RenderParams(material)
            {
                worldBounds = new Bounds
                {
                    min = Vector3.zero,
                    max = new float3(lbmSolver.CellRes * matSize)
                },
                layer = layer
            };

            SetBuffers(lbmSolver);
        }

        public void Update()
        {
            Graphics.RenderPrimitives(in _renderParams, MeshTopology.Points, _vertexCount);
        }

        #region Material

        private void SetBuffers(ILbmSolver lbmSolver)
        {
            _material.SetBuffer(Props.field, lbmSolver.FieldBuffer);
            _material.SetBuffer(Props.field_velocity, lbmSolver.FieldVelocityBuffer);
            _material.SetInt(Props.cell_res, lbmSolver.CellRes);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Props
        {
            field,
            field_velocity,
            cell_res,
            size
        }

        #endregion
    }
}