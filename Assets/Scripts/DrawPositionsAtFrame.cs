using LovelyBytes.AnimationTextures;
using UnityEngine;

namespace DefaultNamespace
{
    public class DrawPositionsAtFrame : MonoBehaviour
    {
        [SerializeField]
        private Texture2D _texture;

        [SerializeField] 
        private float _frame;

        [SerializeField] 
        private BoundingBox _boundingBox;

        [SerializeField]
        private Mesh _mesh;
        
        private Mesh _meshCopy;
        
        private void SetVertices()
        {
            _meshCopy = Instantiate(_mesh);
            var verts = new Vector3[_texture.width];

            for (int i = 0; i < _texture.width; ++i)
            {
                float x = _meshCopy.uv2[i].x - 0.5f / _meshCopy.vertexCount;
                float y = _frame;
                
                Color c = _texture.GetPixelBilinear(x, y);
                Vector3 v = new(c.r, c.g, c.b);
                v = _boundingBox.ToAbsolutePosition(v);
                verts[i] = v;
            }
            
            _meshCopy.SetVertices(verts);
        }

        private void OnValidate()
        {
            if (_texture && _mesh && _boundingBox)
            {
                SetVertices();
            }    
        }

        private void Update()
        {
            _frame += Time.deltaTime;
            OnValidate();
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawMesh(_meshCopy, transform.position);
        }
    }
}