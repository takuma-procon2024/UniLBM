using System;
using TriInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Projects.Scripts
{
    public class TofPlacer : MonoBehaviour
    {
        [SerializeField] private float2 startPos;
        [SerializeField] private float2 margin;
        [SerializeField] private int2 num;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            for (var i = 0; i < num.y; i++)
            for (var j = 0; j < num.x; j++)
            {
                var pos = new float3(startPos.x + j * margin.x, startPos.y + i * margin.y, 0);
                pos += (float3)transform.position;
                Gizmos.DrawWireCube(pos, new float3(1));
            }
        }

        [Button]
        private void Place()
        {
            if (Application.isPlaying) throw new Exception("Only in Editor Mode");

            for (var i = 0; i < transform.childCount; i++)
                DestroyImmediate(transform.GetChild(i).gameObject);

            for (var i = 0; i < num.y; i++)
            for (var j = 0; j < num.x; j++)
            {
                var pos = new float3(j * margin.x, i * margin.y, 0);
                pos += new float3(startPos, 0);
                pos += (float3)transform.position;
                
                var go = new GameObject($"Tof_{i}_{j}");
                go.transform.SetParent(transform);
                go.transform.position = pos;
            }
        }
    }
}