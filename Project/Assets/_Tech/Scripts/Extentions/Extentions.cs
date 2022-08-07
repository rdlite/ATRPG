using UnityEngine;
using UnityEngine.UI;

public static class Extentions {
    public static void SetTransparency(this Image thisColor, float value) {
        thisColor.color = new Color(thisColor.color.r, thisColor.color.g, thisColor.color.b, value);
    }

    public static Color SetTransparency(this Color thisColor, float value) {
        thisColor.a = value;
        return thisColor;
    }

    public static float GetTransparency(this Image thisColor) {
        return thisColor.color.a;
    }

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