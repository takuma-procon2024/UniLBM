using System;
using UniLbm.Lbm.Extension;
using UnityEngine;

namespace Projects
{
    public class FanTest : MonoBehaviour
    {
        [SerializeField] private FanKeyData[] fanKeyData;
        
        private void Update()
        {
            foreach (var data in fanKeyData)
            {
                if (!Input.GetKeyDown(data.key)) continue;
                
                if (!data.IsForced)
                {
                    data.IsForced = true;
                    data.forceSource.SetForce(data.force);
                }
                else
                {
                    data.IsForced = false;
                    data.forceSource.SetForce(0);
                }
            }
        }
        
        [Serializable]
        private class FanKeyData
        {
            public KeyCode key;
            public LbmForceSource forceSource;
            public float force;
            
            [NonSerialized]
            public bool IsForced;
        }
    }
} 