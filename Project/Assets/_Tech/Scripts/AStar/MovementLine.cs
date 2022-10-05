using UnityEngine;

public struct MovementLine
{
    private const float _verticalLineGradient = 1e5f;

    private Vector3 _pointOnLine_1, _pointOnLine_2;
    private float _gradient;
    private float _yIntercept;
    private float _gradientPerpendicular;
    private bool _approachSide;

    public MovementLine(Vector3 pointOnLine, Vector2 pointRependicularToLine)
    {
        _gradient = 0;
        _approachSide = false;

        float dx = pointOnLine.x - pointRependicularToLine.x;
        float dy = pointOnLine.z - pointRependicularToLine.y;

        if (dx == 0f)
        {
            _gradientPerpendicular = _verticalLineGradient;
        }
        else
        {
            _gradientPerpendicular = dy / dx;
        }

        if (_gradientPerpendicular == 0)
        {
            _gradient = _verticalLineGradient;
        }
        else
        {
            _gradient = -1 / _gradientPerpendicular;
        }

        _yIntercept = pointOnLine.z - _gradient * pointOnLine.x;
        _pointOnLine_1 = pointOnLine;
        _pointOnLine_2 = pointOnLine.GetYRemovedV2() + new Vector2(1, _gradient);

        _approachSide = GetSide(pointRependicularToLine);
    }

    private bool GetSide(Vector2 p)
    {
        return (p.x - _pointOnLine_1.x) * (_pointOnLine_2.y - _pointOnLine_1.y) > (p.y - _pointOnLine_1.y) * (_pointOnLine_2.x - _pointOnLine_1.x);
    }

    public bool HasCrossedLine(Vector2 p)
    {
        return GetSide(p) != _approachSide;
    }

    public void DrawWithGizmos(float length)
    {
        Vector3 lineDir = new Vector3(1, 0, _gradient).normalized;
        Vector3 lineCentre = _pointOnLine_1;
        Gizmos.DrawLine(lineCentre - lineDir * length / 4f, lineCentre + lineDir * length / 4f);
    }
}