using UnityEngine;

public class StunEffect : MonoBehaviour
{
    private Transform _snapPoint;

    public void SnapToPoint(Transform snapPoint)
    {
        _snapPoint = snapPoint;
    }

    private void Update()
    {
        if (_snapPoint != null)
        {
            transform.position = _snapPoint.transform.position;
        }
    }
}