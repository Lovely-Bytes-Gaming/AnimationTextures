using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LovelyBytes.AnimationTextures
{
    public class AnimationBaker : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField]
        private Renderer _renderer;
        
        [SerializeField]
        private Animator _animator;

        [SerializeField] 
        private Transform _transform;

        [SerializeField] 
        private Mesh _mesh;
        
        [SerializeField]
        private BoundingBox _boundingBox;
        
        [SerializeField]
        private AnimationClip _animationClip;
        
        [ContextMenu(nameof(CreateBoundingBox))]
        public void CreateBoundingBox()
        {
            Mesh mesh = Instantiate(_mesh);
            
            Vector3 min = new (float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new (float.MinValue, float.MinValue, float.MinValue);

            GetGraphForAnimationClip(_animator, _animationClip, out PlayableGraph graph);
            graph.Evaluate(0f);
            
            float delta = 1f / _animationClip.frameRate;
            float t = 0f;
            
            while (t - _animationClip.length < 0f)
            {
                if (_renderer is SkinnedMeshRenderer smr)
                    smr.BakeMesh(mesh);
                
                UpdateCorners(ref min, ref max, mesh.vertices);
                graph.Evaluate(delta);

                t += delta;
            }
            
            DestroyImmediate(mesh);
            graph.Destroy();
            
            _boundingBox = ScriptableObject.CreateInstance<BoundingBox>();
            _boundingBox.Min = min;
            _boundingBox.Max = max;

            string path = EditorUtility.SaveFilePanel("Save Bounding Box Asset", 
                Application.dataPath, $"{_animationClip.name}-BoundingBox","asset");

            if (string.IsNullOrEmpty(path))
                return;

            path = GetRelativePath(path);
            
            AssetDatabase.CreateAsset(_boundingBox, path);
            _boundingBox = AssetDatabase.LoadAssetAtPath<BoundingBox>(path);
            Debug.Log("Successfully created bounding box");
        }

        [ContextMenu(nameof(BakeTextureAndMesh))]
        public void BakeTextureAndMesh()
        {
            if (!_boundingBox)
            {
                Debug.LogError("No bounding box assigned!");
                return;
            }

            Mesh mesh = Instantiate(_mesh);
            
            int frames = Mathf.CeilToInt(_animationClip.length * _animationClip.frameRate);
            int width = mesh.vertexCount;
            int height = frames;
            
            Debug.Log($"width: {width}, height: {height}");
            
            var colors = new Color[width * height];
            var vertexIds = new Vector2[width];
            
            GetGraphForAnimationClip(_animator, _animationClip, out PlayableGraph graph);
            graph.Evaluate(0f);
            
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: false, linear: true);
            
            float delta = 1f / _animationClip.frameRate;

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3 v = _transform.TransformPoint(mesh.vertices[j]);
                    v = _boundingBox.ToRelativePosition(v);
                    
                    Color c = new(v.x, v.y, v.z);
                    colors[i * width + j] = c;
                }

                graph.Evaluate(delta);
                
                if (_renderer is SkinnedMeshRenderer smr)
                    smr.BakeMesh(mesh);
            }
            texture.SetPixels(colors);

            for (int i = 0; i < width; i++)
            {
                vertexIds[i] = new Vector2((i + 0.5f) / width, 1f);
            }
            
            mesh.SetUVs(1, vertexIds);
            
            //for (int i = 0; i < 8; ++i)
            //{
            //    VertexAttribute attr = VertexAttribute.TexCoord0 + i;
            //    
            //    if (mesh.HasVertexAttribute(attr))
            //        continue;
            //    
            //    mesh.SetUVs(attr - VertexAttribute.TexCoord0, vertexIds);
            //    Debug.Log($"Vertex IDs written to {attr}");
            //    break;
            //}
            
            string texturePath = EditorUtility.SaveFilePanel("Save Animation Texture", 
                Application.dataPath, $"{_animationClip.name}-Texture", "asset");
            
            AssetDatabase.CreateAsset(texture, GetRelativePath(texturePath));
            
            string meshPath = EditorUtility.SaveFilePanel("Save Mesh", Application.dataPath,
                $"{mesh.name.Replace("(Clone)", "")}-VertexIDs", "asset");
            
            AssetDatabase.CreateAsset(mesh, GetRelativePath(meshPath));
        }
        
        private void UpdateCorners(ref Vector3 min, ref Vector3 max, Vector3[] verts)
        {
            foreach (Vector3 v in verts)
            {
                Vector3 vt = _transform.TransformPoint(v);
                
                if (vt.x < min.x) min.x = vt.x;
                if (vt.x > max.x) max.x = vt.x;
                
                if (vt.y < min.y) min.y = vt.y;
                if (vt.y > max.y) max.y = vt.y;
                
                if (vt.z < min.z) min.z = vt.z;
                if (vt.z > max.z) max.z = vt.z;
            }
        }

        private static void GetGraphForAnimationClip(Animator animator, AnimationClip animationClip, out PlayableGraph graph)
        {
            graph = PlayableGraph.Create();
            var playableOutput = AnimationPlayableOutput.Create(graph, "Animation", animator);
            var clipPlayable = AnimationClipPlayable.Create(graph, animationClip);
            playableOutput.SetSourcePlayable(clipPlayable);
        }

        private static string GetRelativePath(string path)
        {
            int idx = path.IndexOf("Assets", StringComparison.Ordinal);
            return path[idx..];
        }
#endif
    }
}