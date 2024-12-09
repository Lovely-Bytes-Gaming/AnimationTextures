using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

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

        [SerializeField, Range(0, 3), Tooltip("The UV channel in which to write the vertex IDs for the output mesh")] 
        private int _uvChannel;
        
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
            
            int width = mesh.vertexCount;
            int height = Mathf.CeilToInt(_animationClip.length * _animationClip.frameRate);
            
            var colors = new Color[width * height];
            var vertexIds = new Vector2[width];
            var normals = new Vector3[width];
            
            GetGraphForAnimationClip(_animator, _animationClip, out PlayableGraph graph);
            graph.Evaluate(0f);
            
            var smr = _renderer as SkinnedMeshRenderer;
            smr?.BakeMesh(mesh);
            
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
                smr?.BakeMesh(mesh);
            }
            
            graph.Destroy();
            
            texture.SetPixels(colors);

            for (int i = 0; i < width; i++)
            {
                vertexIds[i] = new Vector2((i + 0.5f) / width, 1f);
                normals[i] = _transform.TransformDirection(mesh.normals[i]);
            }
            
            mesh.SetUVs(_uvChannel, vertexIds);
            mesh.SetNormals(normals);

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