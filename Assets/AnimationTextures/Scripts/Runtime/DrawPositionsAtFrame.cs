using UnityEngine;

namespace LovelyBytes.AnimationTextures
{
    public class DrawPositionsAtFrame : MonoBehaviour
    {
        [SerializeField]
        private Texture2D _texture;

        [SerializeField] 
        private int _frame;

        [SerializeField] 
        private BoundingBox _boundingBox;

        [SerializeField]
        private Mesh _mesh;
        private Mesh _meshCopy;

        private Mesh _currentMesh;
        
        private void SetVertices()
        {
            var verts = new Vector3[_texture.width];

            for (int i = 0; i < _texture.width; ++i)
            {
                int x = Mathf.RoundToInt(_meshCopy.uv2[i].x * _meshCopy.vertexCount - 0.5f);
                int y = _frame;
                
                Color c = _texture.GetPixel(x, y);
                Vector3 v = new(c.r, c.g, c.b);
                v = _boundingBox.ToAbsolutePosition(v);
                verts[i] = v;
            }
            
            _meshCopy.SetVertices(verts);
        }

        private void OnValidate()
        {
            if (!_texture || !_mesh || !_boundingBox) 
                return;
            
            if (_currentMesh != _mesh)
            {
                _currentMesh = _mesh;
                    
                if (_meshCopy)
                    DestroyImmediate(_meshCopy);
                    
                _meshCopy = Instantiate(_mesh);
            }
                
            SetVertices();
        }

        private void OnDrawGizmos()
        {
            if (_meshCopy)
                Gizmos.DrawMesh(_meshCopy, transform.position);
        }
    }
}