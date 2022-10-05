using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BloodDecalAppearance : MonoBehaviour
{
    [SerializeField] private TexturesContainer _bloodTextures;
    [SerializeField] private DecalProjector _projector;
    [SerializeField] private Vector2 _randomWidthSize, _randomHeightSize;

    private bool _isDecalDestroy;

    public void ThrowDecalOnSurface(Vector3 hitPosition, Vector3 outDirection)
    {
        StartCoroutine(DecalAppearance(hitPosition, outDirection));
    }

    public void DestroyDecal()
    {
        _isDecalDestroy = true;
    }

    private IEnumerator DecalAppearance(Vector3 hitPosition, Vector3 outDirection)
    {
        transform.position = hitPosition + Vector3.up * 2f;
        transform.forward = outDirection;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x + 70f, transform.eulerAngles.y + Random.Range(-30f, 30f), transform.eulerAngles.z);
        transform.position -= transform.up * Random.Range(1f, 1.5f);
        transform.position += transform.forward * 3f;
        float endSizeX = Random.Range(_randomWidthSize.x, _randomWidthSize.y);
        float endSizeY = Random.Range(_randomHeightSize.x, _randomHeightSize.y);
        _projector.size = new Vector3(endSizeX / 2f, endSizeY / 2f, _projector.size.z);
        _projector.material = Instantiate(_projector.material);
        _projector.material.SetTexture("_MainTex", _bloodTextures.GetRandomTexture());
        //_projector.material.SetVector("_UpAxis", -transform.forward);
        //_projector.material.SetFloat("_Rotation", Vector3.Dot(transform.forward, Vector3.forward) * 180f);

        float t = 0f;

        while (t <= 1f)
        {
            t += Time.deltaTime;

            _projector.size = Vector3.Lerp(_projector.size, new Vector3(endSizeX, endSizeY, _projector.size.z), 30f * Time.deltaTime);
            _projector.pivot = new Vector3(0f, _projector.size.y / 2f, 0f);

            yield return null;
        }

        yield return new WaitWhile(() => !_isDecalDestroy);

        yield return new WaitForSeconds(Random.Range(2.5f, 4f));

        t = 1f;
        float destroySpeed = Random.Range(.1f, .2f);

        while (t >= 0f)
        {
            t -= Time.deltaTime * destroySpeed;

            _projector.fadeFactor = t;

            yield return null;
        }

        Destroy(gameObject);
    }
}