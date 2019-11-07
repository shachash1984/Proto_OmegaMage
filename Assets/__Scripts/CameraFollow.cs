using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : PT_MonoBehaviour {

    static public CameraFollow S;

    public Transform targetTransform;
    public float camEasing = 0.1f;
    public Vector3 followOffset = new Vector3(0, 0, -2f);

    private void Awake()
    {
        S = this;
    }

    private void FixedUpdate()
    {
        Vector3 pos1 = targetTransform.position + followOffset;
        pos = Vector3.Lerp(pos, pos1, camEasing);
    }
}
