using UnityEngine;

namespace LovelyBytes.AnimationTextures
{
    /// <summary>
    /// Example script for blending between 2 shader animations that are encoded in the same texture
    /// </summary>
    public class BlendShaderAnimations : MonoBehaviour
    {
        [Min(0)] 
        public int 
            ClipIndexA = 0, 
            ClipIndexB = 1;
        
        [SerializeField] 
        private MultiClipInfoReference _multiClipInfoRef;

        [SerializeField] 
        private float _speed;

        [SerializeField, Range(0f, 1f)] 
        private float _blend;

        [SerializeField] 
        private Renderer _renderer;

        private float _time0, _time1;

        private static readonly int _clipTimeAProp = Shader.PropertyToID("_ClipTimeA"); 
        private static readonly int _clipTimeBProp = Shader.PropertyToID("_ClipTimeB");
        private static readonly int _blendProp = Shader.PropertyToID("_Blend");
        
        private void Update()
        {
            _time0 += Time.deltaTime * _speed;
            _time1 += Time.deltaTime * _speed;
            
            ClipInfo a = _multiClipInfoRef.Value.Entries[ClipIndexA];
            ClipInfo b = _multiClipInfoRef.Value.Entries[ClipIndexB];

            _time0 %= 1f;
            _time1 %= 1f;

            if (_blend < Mathf.Epsilon)
                _time1 = 0f;
            
            if (_blend > 1f - Mathf.Epsilon)
                _time0 = 0f;

            foreach (Material material in _renderer.materials)
            {
                material.SetFloat(_clipTimeAProp, Mathf.Lerp(a.NormalizedStartTime, a.NormalizedEndTime, _time0));
                material.SetFloat(_clipTimeBProp, Mathf.Lerp(b.NormalizedStartTime, b.NormalizedEndTime, _time1));
                material.SetFloat(_blendProp, _blend);
            }
            
        }
    }
}