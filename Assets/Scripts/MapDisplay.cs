using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class MapDisplay : MonoBehaviour {
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture) {
        // Allow texture to be viewed while the scene is not running
        textureRender.sharedMaterial.mainTexture = texture;
        // Scale the plane to correctly fit the entire texture
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshGenerator.MeshData meshData, Texture2D texture) {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}