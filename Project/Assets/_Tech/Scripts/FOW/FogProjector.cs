using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FogProjector : MonoBehaviour
{
    private RenderTexture _savedTexture;
    private RenderTexture _fogTexture;
    private RenderTexture _prevTexture;
    private RenderTexture _currTexture;
    private DecalProjector _projector;
    private float _blendAmount;
    private float _blendSpeed;

    public void Init(RenderTexture fogTexture, Material fogMaterial, float blendSpeed, RenderTexture baseSavedTexture)
    {
        _fogTexture = fogTexture;
        _blendSpeed = blendSpeed;

        RenderTexture savedTexture = null;

        if (baseSavedTexture != null)
        {
            savedTexture = new RenderTexture(baseSavedTexture);
            Graphics.Blit(baseSavedTexture, savedTexture);
            _savedTexture = savedTexture;
        }

        _projector = GetComponent<DecalProjector>();
        _projector.enabled = true;

        _prevTexture = new RenderTexture(fogTexture);
        _currTexture = new RenderTexture(fogTexture);

        _projector.material = new Material(fogMaterial);

        _projector.material.SetTexture("_PrevTexture", _prevTexture);
        _projector.material.SetTexture("_CurrTexture", _currTexture);
        _projector.material.SetInt("_IsUseSaveTexture", savedTexture == null ? 0 : 1);
        _projector.material.SetTexture("_BaseSavedTexture", savedTexture);

        StartNewBlend();
    }

    public RenderTexture GetCurrentRenderTexture()
    {
        return _currTexture;
    }

    public RenderTexture GetSavedRenderTexture()
    {
        return _savedTexture;
    }

    public void StartNewBlend()
    {
        StopCoroutine(BlendFog());
        _blendAmount = 0;
        // Swap the textures
        Graphics.Blit(_currTexture, _prevTexture);
        Graphics.Blit(_fogTexture, _currTexture);

        StartCoroutine(BlendFog());
    }

    IEnumerator BlendFog()
    {
        while (_blendAmount < 1)
        {
            _blendAmount += Time.deltaTime * _blendSpeed;
            _projector.material.SetFloat("_Blend", _blendAmount);
            yield return null;
        }

        StartNewBlend();
    }
}