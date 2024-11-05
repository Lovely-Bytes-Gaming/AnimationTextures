using System;
using UnityEngine;

namespace LovelyBytes.AnimationTextures
{
    public class BoundingBoxToMaterial : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private BoundingBox _boundingBox;
        
        private readonly int _minBoundsID = Shader.PropertyToID("_MinBounds");
        private readonly int _maxBoundsID = Shader.PropertyToID("_MaxBounds");

        private void OnValidate()
        {
            _renderer.sharedMaterial.SetVector(_minBoundsID, _boundingBox.Min);
            _renderer.sharedMaterial.SetVector(_maxBoundsID, _boundingBox.Max);
        }

        private void Awake()
        {
            OnValidate();
        }
    }
}