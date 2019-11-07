using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireGroundSpell : PT_MonoBehaviour {

    public float duration = 4;
    public float durationVariance = 0.5f;
    public float fadeTime = 1f;
    public float timeStart;
    public float damagePerSecond = 10;

    private void Start()
    {
        timeStart = Time.time;
        duration = Random.Range(duration - durationVariance, duration + durationVariance);
    }

    private void Update()
    {
        //Determine a number [0..1] (between 0 and 1) that stores the percentage of duration that has passed
        float u = (Time.time - timeStart) / duration;

        float fadePercent = 1 - (fadeTime / duration);

        if (u > fadePercent)
        {
            //if it's after the time to start fading, start to sink into the ground
            float u2 = (u - fadePercent) / (1 - fadePercent);
            Vector3 loc = pos;
            loc.z = u2 * 2;
            pos = loc;
        }
        if (u > 1)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        //Announce when another object enteres the collider
        GameObject go = Utils.FindTaggedParent(other.gameObject);
        if(go== null)
        {
            go = other.gameObject;
        }
        //Utils.tr("Flame hit", go.name);
    }

    private void OnTriggerStay(Collider other)
    {
        EnemyBug recepient = other.GetComponent<EnemyBug>();
        if (recepient)
            recepient.Damage(damagePerSecond, ElementType.fire ,true);
    }
}
