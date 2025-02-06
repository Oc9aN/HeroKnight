using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    // 벡터간 부동 소수점 비교
    public static bool VectorsApproximatelyEqual(Vector3 a, Vector3 b)
    {
        return Mathf.Approximately(a.x, b.x) &&
               Mathf.Approximately(a.y, b.y) &&
               Mathf.Approximately(a.z, b.z);
    }

    public static bool FloatSignEqual(float a, float b)
    {
        return Mathf.Sign(a) == Mathf.Sign(b);
    }
}
