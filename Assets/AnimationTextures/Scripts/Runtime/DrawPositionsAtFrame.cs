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
        private MeshFilter _meshFilter;
        
        private Mesh _currentMesh;

        private void Update()
        {
            SetVertices();
        }

        private void SetVertices()
        {
            var verts = new Vector3[_texture.width];

            Mesh mesh = _meshFilter.mesh;
            
            for (int i = 0; i < _texture.width; ++i)
            {
                int x = Mathf.RoundToInt(mesh.uv3[i].x * mesh.vertexCount - 0.5f);
                int y = _frame;
                
                Color c = _texture.GetPixel(x, y);
                Vector3 v = new(c.r, c.g, c.b);
                v = _boundingBox.ToAbsolutePosition(v);
                verts[i] = v;
            }
            mesh.SetVertices(verts);
        }

        private void OnDrawGizmos()
        {
            //if (_meshCopy)
            //    Gizmos.DrawMesh(_meshCopy, transform.position);
        }
    }
}