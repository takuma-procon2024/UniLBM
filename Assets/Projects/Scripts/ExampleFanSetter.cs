using System.Collections.Generic;
using UI.FanDebug;
using UnityEngine;

namespace Projects.Scripts
{
    public class ExampleFanSetter : MonoBehaviour, IFanSetter
    {
        [SerializeField] private float powerMultiplier = 1.0f;

        private readonly Dictionary<int, ExampleForceSource> _sources = new();

        private void Start()
        {
            foreach (var source in FindObjectsByType<ExampleForceSource>(FindObjectsSortMode.None))
                _sources.Add(source.Index, source);
        }

        public void SetFanPower(float power, int fanIndex)
        {
            _sources[fanIndex].SetForce(power * powerMultiplier);
        }
    }
}