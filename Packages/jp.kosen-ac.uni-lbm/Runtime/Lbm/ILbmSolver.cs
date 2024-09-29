using UnityEngine;

namespace UniLbm.Lbm
{
    public interface ILbmSolver
    {
        public GraphicsBuffer VelDensBuffer { get; }
        public GraphicsBuffer FieldBuffer { get; }
        public int CellRes { get; }
    }
}