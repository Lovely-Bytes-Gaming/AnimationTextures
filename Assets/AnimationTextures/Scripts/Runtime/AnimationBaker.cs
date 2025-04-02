using System;
using System.Linq;
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
        private AnimationClip[] _animationClips;

        [SerializeField, Range(0, 3), Tooltip("The UV channel in which to write the vertex IDs for the output mesh")] 
        private int _uvChannel;

        [ContextMenu(nameof(Bake))]
        public void Bake()
        {
            CreateBoundingBox();
            BakeTextureAndMesh();
        }
        
        [ContextMenu(nameof(CreateBoundingBox))]
        public void CreateBoundingBox()
        {
            Mesh mesh = Instantiate(_mesh);
            
            Vector3 min = new (float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new (float.MinValue, float.MinValue, float.MinValue);

            foreach (AnimationClip animationClip in _animationClips)
            {
                GetGraphForAnimationClip(_animator, animationClip, out PlayableGraph graph);
                graph.Evaluate(0f);
                
                float delta = 1f / animationClip.frameRate;
                float t = 0f;
                
                while (t < animationClip.length)
                {
                    if (_renderer is SkinnedMeshRenderer smr)
                        smr.BakeMesh(mesh);
                    
                    UpdateCorners(ref min, ref max, mesh.vertices);
                    graph.Evaluate(delta);

                    t += delta;
                }
                graph.Destroy();
            }
            
            DestroyImmediate(mesh);
            
            _boundingBox = ScriptableObject.CreateInstance<BoundingBox>();
            _boundingBox.Min = min;
            _boundingBox.Max = max;

            string path = EditorUtility.SaveFilePanel("Save Bounding Box Asset", 
                Application.dataPath, $"{_mesh.name}-BoundingBox","asset");

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
            
            int vertexCount = mesh.vertexCount;
            
            int totalFrameCount = _animationClips
                .Sum(animationClip => Mathf.CeilToInt(animationClip.length * animationClip.frameRate))
                + (_animationClips.Length - 1);
            
            var colors = new Color[vertexCount * totalFrameCount];
            var vertexIds = new Vector2[vertexCount];
            var normals = new Vector3[vertexCount];
            var texture = new Texture2D(vertexCount, totalFrameCount, TextureFormat.ARGB32, mipChain: false, linear: true);

            int insertIdx = 0;
            for (int clipIndex = 0; clipIndex < _animationClips.Length; clipIndex++)
            {
                AnimationClip animationClip = _animationClips[clipIndex];
                int frameCount = Mathf.CeilToInt(animationClip.length * animationClip.frameRate);

                GetGraphForAnimationClip(_animator, animationClip, out PlayableGraph graph);
                graph.Evaluate(0f);

                var smr = _renderer as SkinnedMeshRenderer;
                smr?.BakeMesh(mesh);

                float delta = 1f / animationClip.frameRate;

                for (int i = 0; i < frameCount; i++)
                {
                    for (int j = 0; j < vertexCount; j++)
                    {
                        Vector3 v = _transform.TransformPoint(mesh.vertices[j]);
                        v = _boundingBox.ToRelativePosition(v);

                        Color c = new(v.x, v.y, v.z);
                        colors[insertIdx++] = c;
                    }

                    graph.Evaluate(delta);
                    smr?.BakeMesh(mesh);
                }
                graph.Destroy();

                if (clipIndex < _animationClips.Length - 1)
                {
                    for (int i = 0; i < vertexCount; ++i)
                    {
                        colors[insertIdx] = colors[insertIdx - vertexCount];
                        ++insertIdx;
                    }
                }
            }

            texture.SetPixels(colors);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            for (int i = 0; i < vertexCount; i++)
            {
                vertexIds[i] = new Vector2((i + 0.5f) / vertexCount, 1f);
                normals[i] = _transform.TransformDirection(mesh.normals[i]);
            }
            
            mesh.SetUVs(_uvChannel, vertexIds);
            mesh.SetNormals(normals);

            string texturePath = EditorUtility.SaveFilePanel("Save Animation Texture", 
                Application.dataPath, $"{_mesh.name}-AnimationTexture", "asset");
            
            AssetDatabase.CreateAsset(texture, GetRelativePath(texturePath));
            
            string meshPath = EditorUtility.SaveFilePanel("Save Mesh", Application.dataPath,
                $"{_mesh.name}-VertexIDs", "asset");
            
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