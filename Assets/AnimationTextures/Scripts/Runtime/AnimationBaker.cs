﻿/*
 * Editor-only component for baking animations into textures.
 * Allows to use animations when GPU instancing, rendering particles or using Entities.
 * The implementation is based on the following article:
 * https://stoyan3d.wordpress.com/2021/07/23/vertex-animation-texture-vat/ (last visit: April 3rd 2025)
 */

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace LovelyBytes.AnimationTextures
{
    public class AnimationBaker : MonoBehaviour
    {
#if UNITY_EDITOR
        
        [SerializeField, Tooltip("The animator used to playback the animation clips")]
        private Animator _animator;


        [SerializeField, Tooltip("The mesh the animation is being baked for")] 
        private Mesh _mesh;
        
        [SerializeField, Tooltip("Bounding Box that encloses all provided animation clips. " +
                                 "Can be generated by the Method 'CreateBoundingBox'." +
                                 "Because of limited floating point precision, we store all animated vertex positions" +
                                 "as relative positions within the bounding box. (in Range (0,1))")]
        private BoundingBox _boundingBox;
        
        [SerializeField, Tooltip("All animation clips within this array will be baked into a single texture.")]
        private AnimationClip[] _animationClips;

        [SerializeField, Range(0, 3), Tooltip("The vertex IDs for sampling the animation clips from the texture (x-coordinate) " +
                                              "will be stored in the given UV channel.")] 
        private int _uvChannel;
        
        [Header("Optional")]
        [SerializeField, Tooltip("Optional. Needs to be assigned when the animated object uses a skinned mesh renderer for deformation")]
        private SkinnedMeshRenderer _renderer;
        [SerializeField, Tooltip("Optional. Will be used to transform the mesh during baking")] 
        private Transform _transform;

        private string _lastOpenedPath = Application.dataPath;
        
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
                    BakeMesh(mesh);
                    
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

            string path = OpenSaveFilePanel("Save Bounding Box Asset", 
                $"{_mesh.name}-BoundingBox","asset");

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
            
            MultiClipInfo multiClipInfo = new (_animationClips);
            int totalFrameCount = multiClipInfo.TotalFrameCount;
            
            var colors = new Color[vertexCount * totalFrameCount];
            var vertexIds = new Vector2[vertexCount];
            var normals = new Vector3[vertexCount];
            
            var texture = new Texture2D(vertexCount, totalFrameCount, TextureFormat.ARGB32, mipChain: false, linear: true)
            {
                // if we bake only a single clip that is looping, we can get the looping behaviour via sampling the texture
                // in repeat mode. Otherwise, we set the wrap mode to clamp, so that the last frame of the last clip doesn't
                // impact the first frame of the first clip.
                wrapMode = _animationClips.Length == 1 && _animationClips[0].wrapMode == WrapMode.Loop 
                    ? TextureWrapMode.Repeat : TextureWrapMode.Clamp
            };

            for (int clipIdx = 0; clipIdx < _animationClips.Length; clipIdx++)
            {
                ClipInfo clipInfo = multiClipInfo.Entries[clipIdx];
                AnimationClip animationClip = clipInfo.Clip;
                
                GetGraphForAnimationClip(_animator, animationClip, out PlayableGraph graph);
                graph.Evaluate(0f);
                BakeMesh(mesh);

                float delta = 1f / animationClip.frameRate;
                int endFrame = clipInfo.StartFrame + clipInfo.FrameCount;

                for (int frameIdx = clipInfo.StartFrame; frameIdx < endFrame; ++frameIdx)
                {
                    for (int vertexIdx = 0; vertexIdx < vertexCount; ++vertexIdx)
                    {
                        Vector3 v = TransformVertex(mesh.vertices[vertexIdx]);
                        v = _boundingBox.ToRelativePosition(v);

                        Color c = new(v.x, v.y, v.z);
                        colors[frameIdx * vertexCount + vertexIdx] = c;
                    }

                    graph.Evaluate(delta);
                    BakeMesh(mesh);
                }
                graph.Destroy();

                // if this is not the last clip, we add the last frame of the current animation again,
                // to avoid interpolation errors at the borders between clips
                if (clipIdx < _animationClips.Length - 1)
                {
                    for (int i = 0; i < vertexCount; ++i)
                    {
                        int insertIdx = endFrame * vertexCount + i;
                        colors[insertIdx] = colors[insertIdx - vertexCount];
                    }
                }
            }

            texture.SetPixels(colors);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            for (int i = 0; i < vertexCount; i++)
            {
                vertexIds[i] = new Vector2((i + 0.5f) / vertexCount, 1f);
                normals[i] = TransformNormal(mesh.normals[i]);
            }
            
            mesh.SetUVs(_uvChannel, vertexIds);
            mesh.SetNormals(normals);

            string texturePath = OpenSaveFilePanel("Save Animation Texture",
                $"{_mesh.name}-AnimationTexture", "asset");
            
            AssetDatabase.CreateAsset(texture, GetRelativePath(texturePath));
            
            string meshPath = OpenSaveFilePanel("Save Mesh",
                $"{_mesh.name}-VertexIDs", "asset");
            
            AssetDatabase.CreateAsset(mesh, GetRelativePath(meshPath));

            if (multiClipInfo.Entries.Length < 1)
                return;

            var multiClipInfoRef = ScriptableObject.CreateInstance<MultiClipInfoReference>();
            multiClipInfoRef.name = $"{_mesh.name}-MultiClipInfo";
            multiClipInfoRef.Value = multiClipInfo;
            
            string clipInfoPath = OpenSaveFilePanel("Save Multi Clip Info for Sampling",
                multiClipInfoRef.name, "asset");
            
            AssetDatabase.CreateAsset(multiClipInfoRef, clipInfoPath);
        }
        
        private void UpdateCorners(ref Vector3 min, ref Vector3 max, Vector3[] verts)
        {
            foreach (Vector3 v in verts)
            {
                Vector3 vt = TransformVertex(v);
                
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

        private void BakeMesh(Mesh mesh)
        {
            if(_renderer)
                _renderer.BakeMesh(mesh);
        }

        private Vector3 TransformVertex(Vector3 vertex)
        {
            return _transform ? _transform.TransformPoint(vertex) : vertex;
        }

        private Vector3 TransformNormal(Vector3 normal)
        {
            return _transform ? _transform.TransformDirection(normal) : normal;
        }

        private string OpenSaveFilePanel(string title, string defaultName, string extension)
        {
            string result = EditorUtility.SaveFilePanel(title, 
                _lastOpenedPath, defaultName, extension);
            
            int assetsIdx = result.IndexOf("Assets", StringComparison.Ordinal);
            
            if (assetsIdx > -1)            
                result = result[assetsIdx..];
            
            if (!string.IsNullOrEmpty(result) && result.Contains('/'))
                _lastOpenedPath = result[..result.LastIndexOf('/')];

            if (!Directory.Exists(_lastOpenedPath))
                _lastOpenedPath = Application.dataPath;
            
            return result;
        }
#endif
    }
}