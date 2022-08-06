using UnityEngine;

public static class Extentions {
    public static Vector2 GetYRemovedV2(this Vector3 v3) {
        return new Vector2(v3.x, v3.z);
    }

    public static Vector3 RemoveYCoord(this Vector3 v3) {
        Vector3 resVector = new Vector3(v3.x, 0f, v3.z);
        return resVector;
    }

    public static float InverseLerp(this Vector3 v3, Vector3 a, Vector3 b) {
        Vector3 AB = b - a;
        Vector3 AV = v3 - a;
        return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
    }
}