using System.IO;
using UnityEngine;

public class FogOfWarSaver : MonoBehaviour {
    [SerializeField] private FogProjector _fogOfWarProjector;

    private Texture2D _baseTex, _currentTex, _mergedTexture;
    private RenderTexture _currentRT;
    private RenderTexture _baseRT;

    private string _fowPath = Application.streamingAssetsPath + "/FOWRenderTextures/";

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            SaveRTToFile();
        }
    }

    public void SaveRTToFile() {
        _currentRT = _fogOfWarProjector.GetCurrentRenderTexture();
        _baseRT = _fogOfWarProjector.GetSavedRenderTexture();

        _baseTex = null;
        _currentTex = null;

        RenderTexture.active = _currentRT;
        _currentTex = new Texture2D(_currentRT.width, _currentRT.height, TextureFormat.R8, false);
        _currentTex.ReadPixels(new Rect(0, 0, _currentRT.width, _currentRT.height), 0, 0);
        RenderTexture.active = null;

        if (_baseRT != null) {
            RenderTexture.active = _baseRT;
            _baseTex = new Texture2D(_baseRT.width, _baseRT.height, TextureFormat.R8, false);
            _baseTex.ReadPixels(new Rect(0, 0, _baseRT.width, _baseRT.height), 0, 0);
            RenderTexture.active = null;
        }

        _mergedTexture = MergeTextures(_baseTex, _currentTex);

        byte[] bytes;
        bytes = _mergedTexture.EncodeToPNG();

        string path = _fowPath;

        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }

        File.WriteAllBytes(GetPathToTexture(), bytes);
    }

    public Texture2D MergeTextures(Texture2D savedBaseTexture, Texture2D currentFowTexture) {
        if (savedBaseTexture == null) {
            return currentFowTexture;
        }

        int startX = 0;
        int startY = savedBaseTexture.height - currentFowTexture.height;

        for (int x = startX; x < savedBaseTexture.width; x++) {
            for (int y = startY; y < savedBaseTexture.height; y++) {
                Color bgColor = savedBaseTexture.GetPixel(x, y);
                Color wmColor = currentFowTexture.GetPixel(x - startX, y - startY);

                if (bgColor.r != 0f || wmColor.r != 0f) {
                    savedBaseTexture.SetPixel(x, y, Color.white);
                }
            }
        }

        savedBaseTexture.Apply();
        return savedBaseTexture;
    }

    public RenderTexture LoadFOW(int rtWithHeight) {
        RenderTexture outputTexture = null;
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(GetPathToTexture())) {
            fileData = File.ReadAllBytes(GetPathToTexture());
            tex = new Texture2D(rtWithHeight, rtWithHeight);
            tex.LoadImage(fileData);
            outputTexture = new RenderTexture(rtWithHeight, rtWithHeight, 8);
            Graphics.Blit(tex, outputTexture);
        }

        return outputTexture;
    }

    private string GetPathToTexture() {
        return _fowPath + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ".png";
    }
}