using UnityEngine;

namespace LovelyBytes.AnimationTextures
{
    public class BoundingBoxDrawer : MonoBehaviour
    {
        [SerializeField]
        private BoundingBox _boundingBox;

        [SerializeField]
        private Color _boundingBoxColor;
        
        private void OnDrawGizmos()
        {
            if (!_boundingBox)
                return;
            
            Gizmos.color = _boundingBoxColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(_boundingBox.Center, _boundingBox.Extents);
        }
    }
}