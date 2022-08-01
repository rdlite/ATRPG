using UnityEngine;

public class MovementPath {
    public readonly Vector3[] LookPoints;
    public readonly MovementLine[] TurnBoundaries;
    public readonly int FinishLineIndex;

    public int Length => LookPoints.Length;
    public Vector3 LastPoint => LookPoints[LookPoints.Length - 1];

    public MovementPath(Vector3[] waypoints, Vector3 startPos, float turnDst) {
        LookPoints = waypoints;
        TurnBoundaries = new MovementLine[LookPoints.Length];
        FinishLineIndex = TurnBoundaries.Length - 1;

        Vector3 prevPoint = startPos.GetYRemovedV2();
        for (int i = 0; i < LookPoints.Length; i++) {
            Vector3 currentPoint = LookPoints[i];
            Vector3 dirToCurrentPoint = (currentPoint - prevPoint).normalized;
            Vector3 turnBoundaryPoint = (i == FinishLineIndex) ? currentPoint : (currentPoint - dirToCurrentPoint * turnDst);
            TurnBoundaries[i] = new MovementLine(turnBoundaryPoint, prevPoint - dirToCurrentPoint * turnDst);
            prevPoint = turnBoundaryPoint;
        }
    }

    public void DrawWithGizmos() {
        Gizmos.color = Color.white;
        foreach (Vector3 point in LookPoints) {
            Gizmos.DrawSphere(point, .5f);
        }

        foreach (MovementLine line in TurnBoundaries) {
            line.DrawWithGizmos(10);
        }
    }
}