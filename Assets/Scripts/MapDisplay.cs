using UnityEngine;

public class MapDisplay : MonoBehaviour {
    public Renderer heightMapRender;
    public Renderer colorMapRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture, bool isHeightMap) {
        if (isHeightMap) {
            // Allow texture to be viewed while the scene is not running
            heightMapRender.sharedMaterial.mainTexture = texture;
            // Scale the plane to correctly fit the entire texture
            heightMapRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
        } else {
            colorMapRender.sharedMaterial.mainTexture = texture;
            colorMapRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
        }
    }

    public void DrawMesh(MeshData meshData, Texture2D texture) {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}