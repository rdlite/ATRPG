using UnityEngine;

public class PushPointer : MonoBehaviour {
    private const string SHADER_BASEMAP_PROPERTY = "_BaseMap";

    [SerializeField] private LineRenderer _line;
    [SerializeField] private Transform _endPointerArrow;
    [SerializeField] private Color _defaultMovementColor, _damageColor;
    [SerializeField] private float _arrowLineMovementSpeed = 1f;

    public void ShowLine(Vector3 startPoint, Vector3 endPoint, bool isBreakLine)
    {
        foreach (Transform arrowMeshes in _endPointerArrow)
        {
            if (arrowMeshes.TryGetComponent(out Renderer renderer))
            {
                renderer.material.SetColor("_BaseColor", !isBreakLine ? _defaultMovementColor : _damageColor);
            }
        }

        _line.material.SetColor("_BaseColor", !isBreakLine ? _defaultMovementColor : _damageColor);

        _line.useWorldSpace = true;
        _line.SetPosition(0, startPoint);
        _line.SetPosition(1, endPoint);

        _endPointerArrow.position = endPoint;
        _endPointerArrow.forward = endPoint - startPoint;
    }

    public void HideLine()
    {
        Destroy(gameObject);
    }

    private void Update() {
        _line.material.SetTextureOffset(SHADER_BASEMAP_PROPERTY, new Vector2(-Time.time * _arrowLineMovementSpeed, 0f));
    }
}