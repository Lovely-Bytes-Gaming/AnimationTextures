using UnityEngine;

namespace LovelyBytes.AnimationTextures
{
    [CreateAssetMenu(menuName = "LovelyBytes/AnimationTextures/BoundingBox")]
    public class BoundingBox : ScriptableObject
    {
        public Vector3 Min;
        public Vector3 Max;

        public Vector3 Extents => Max - Min;
        public Vector3 Center => Min + Extents * 0.5f;

        public Vector3 ToRelativePosition(Vector3 position)
        {
            return new Vector3
            {
                x = Mathf.InverseLerp(Min.x, Max.x, position.x),
                y = Mathf.InverseLerp(Min.y, Max.y, position.y),
                z = Mathf.InverseLerp(Min.z, Max.z, position.z)
            };
        }

        public Vector3 ToAbsolutePosition(Vector3 position)
        {
            position.Scale(Extents);
            return Min + position;
        }
    }
}