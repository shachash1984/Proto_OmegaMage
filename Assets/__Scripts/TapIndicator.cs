using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * TapIndicator makes use of the PT_Mover class from ProtoTools. This allows it to
 * use a Bezier curve to alter positions, rotation, scale, etc.
 * */
public class TapIndicator : PT_Mover {

    public float lifeTime = 0.4f;
    public float[] scales; //The scales it interpolates
    public Color[] colors; //The colors it interpolates

    private void Awake()
    {
        scale = Vector3.zero; // hide the indicator
    }

    private void Start()
    {
        //PT_Mover works based on the PT_Loc class, which contains information
        //about position , rotation, and scale. It's similar to transform but simpler

        PT_Loc pLoc;
        List<PT_Loc> locs = new List<PT_Loc>();

        //The position is always the same and always at z=-0.1f;
        Vector3 tPos = pos;
        tPos.z = -0.1f;

        //must have an equal number of scales and colors in the Inspector
        for (int i = 0; i < scales.Length; i++)
        {
            pLoc = new PT_Loc();
            pLoc.scale = Vector3.one * scales[i];
            pLoc.pos = tPos;
            pLoc.color = colors[i];

            locs.Add(pLoc);
        }

        callback = CallbackMethod; // Call CallbackMethod when finished

        //Initiate the move by passing in a series of PT_Locs and duration for the Bezier curve
        PT_StartMove(locs, lifeTime);


    }

    private void CallbackMethod()
    {
        Destroy(gameObject);
    }
}
