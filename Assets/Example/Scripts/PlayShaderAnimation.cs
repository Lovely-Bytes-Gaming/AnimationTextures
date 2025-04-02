using UnityEngine;

namespace DefaultNamespace
{
    public class PlayShaderAnimation : MonoBehaviour
    {
        [SerializeField]
        private Renderer _renderer;

        [SerializeField] 
        private float _speed = 1f;
        
        private float _frame = 0f;
        private readonly int _frameID = Shader.PropertyToID("_Frame");

        private void Update()
        {
            _renderer.material.SetFloat(_frameID, _frame);
            _frame += Time.deltaTime * _speed;
        }
    }
}