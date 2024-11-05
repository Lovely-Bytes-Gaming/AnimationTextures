using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ReadUVsFromAsset : MonoBehaviour
{
    [SerializeField]
    private Mesh _mesh;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        List<Vector2> ls = new();
        _mesh.GetUVs(VertexAttribute.TexCoord2 - VertexAttribute.TexCoord0, ls);
        
        foreach (Vector2 vector2 in ls)
        {
            Debug.Log(vector2);
        }
    }
}
