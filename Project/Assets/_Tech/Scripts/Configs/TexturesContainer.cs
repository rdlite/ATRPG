using UnityEngine;

[CreateAssetMenu(fileName = "New textures container", menuName = "Containers/Textures container")]
public class TexturesContainer : ScriptableObject {
    public Texture2D[] Textures;

    public Texture2D GetRandomTexture() {
        return Textures[Random.Range(0, Textures.Length)];
    }
}