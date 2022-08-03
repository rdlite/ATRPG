using UnityEngine;

public class MapChunkSetter : MonoBehaviour {
    private static int resAdd;

    [SerializeField] private int _textureResolution = 64;
    [SerializeField] private RenderTextureFormat _fogTextureFormat;
    [SerializeField] private FilterMode _filterMode;
    [SerializeField] private Material _projectorSolidFowMaterial, _projectorTransparentFowMaterial;
    [SerializeField] private FogProjector _solidFowProjector, _transparentFowProjector;
    [SerializeField] private Camera _solidFowCamera, _transparentFowCamera;
    [SerializeField] private FogOfWarSaver _fogOfWarSaver;
    [SerializeField] private int _antiAliasing = 2;
    [SerializeField] private float _blendTexturesSpeed = 5f;

    private RenderTexture _savedRenderTextureData;

    private void Start() {
        RenderTexture loadedRT = _fogOfWarSaver.LoadFOW(_textureResolution);

        if (loadedRT != null) {
            _savedRenderTextureData = new RenderTexture(loadedRT.width, loadedRT.height, 8);
            _savedRenderTextureData.width = _textureResolution;
            _savedRenderTextureData.height = _textureResolution;
            _savedRenderTextureData.antiAliasing = _antiAliasing;
            _savedRenderTextureData.filterMode = _filterMode;
            _savedRenderTextureData.format = _fogTextureFormat;
            Graphics.Blit(loadedRT, _savedRenderTextureData);
        }

        RenderTexture solidFowTexture = GenerateTexture();
        RenderTexture transparentFowTexture = GenerateTexture();

        _solidFowProjector.Init(solidFowTexture, _projectorSolidFowMaterial, _blendTexturesSpeed, _savedRenderTextureData);
        _transparentFowProjector.Init(transparentFowTexture, _projectorTransparentFowMaterial, _blendTexturesSpeed, null);

        _solidFowCamera.targetTexture = solidFowTexture;
        _transparentFowCamera.targetTexture = transparentFowTexture;

        resAdd++;
    }

    private RenderTexture GenerateTexture() {
        RenderTexture rt = new RenderTexture(
            _textureResolution + resAdd,
            _textureResolution + resAdd,
            0,
            _fogTextureFormat) { filterMode = _filterMode };
        rt.antiAliasing = _antiAliasing;

        return rt;
    }
}