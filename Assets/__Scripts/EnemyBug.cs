using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBug : PT_MonoBehaviour, IEnemy {

    public float speed = 0.5f;
    public float health = 10f;
    public float damageScale = 0.8f;
    public float damageScaleDuration = 0.25f;

    private float damageScaleStartTime;
    private float _maxHealth;
    public Vector3 walkTarget;
    public bool walking;
    public Transform characterTrans;

    public Dictionary<ElementType, float> damageDict;
    [SerializeField] private float _touchDamage = 1;
    public float touchDamage
    {
        get { return _touchDamage;}
        set { _touchDamage = value;}
    }
    public string typeString
    {
        get { return roomXMLString; }
        set { roomXMLString = value; }
    }

    public string roomXMLString;

    private void Awake()
    {
        characterTrans = transform.Find("CharacterTrans");
        _maxHealth = health;
        ResetDamageDict();
    }

    private void Update()
    {
        WalkTo(Mage.S.pos);
    }

    public void FixedUpdate()
    {
        if (walking)
        {
            if ((walkTarget - pos).magnitude < speed * Time.fixedDeltaTime)
            {
                //if is very close to walkTarget, stop there
                pos = walkTarget;
                StopWalking();
            }
            else
            {
                //otherwise, move towards target
                rigidbody.velocity = (walkTarget - pos).normalized * speed;
            }
        }
        else
        {
            //if not walking, velocity should be zero
            rigidbody.velocity = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        //Apply damage from different element types
        float dmg = 0;
        foreach (KeyValuePair<ElementType, float> entry in damageDict)
        {
            dmg += entry.Value;
        }

        //if took damage
        if(dmg > 0)
        {
            //and if it is full scale now (not already scaling)
            if (characterTrans.localScale == Vector3.one)
            {
                //start the damage scale animation
                damageScaleStartTime = Time.time;
            }
        }

        //The damage scale animation
        float damU = (Time.time - damageScaleStartTime) / damageScaleDuration;
        damU = Mathf.Min(1, damU); // limit the max localScale to 1
        float scl = (1 - damU) * damageScale + damU * 1;
        characterTrans.localScale = scl * Vector3.one;

        health -= dmg;
        health = Mathf.Min(_maxHealth, health);

        ResetDamageDict();

        if (health <= 0)
            Die();
    }

    public void WalkTo(Vector3 xTarget)
    {
        walkTarget = xTarget;
        walkTarget.z = 0;
        walking = true;
        Face(walkTarget);
    }

    public void Face(Vector3 poi)
    {
        Vector3 delta = poi - pos; // Find vector to the point of interest
        float rZ = Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x);
        characterTrans.rotation = Quaternion.Euler(0, 0, rZ);
    }

    public void StopWalking()
    {
        walking = false;
        rigidbody.velocity = Vector3.zero;
    }

    private void ResetDamageDict()
    {
        if (damageDict == null)
            damageDict = new Dictionary<ElementType, float>();
        damageDict.Clear();
        damageDict.Add(ElementType.earth, 0);
        damageDict.Add(ElementType.water, 0);
        damageDict.Add(ElementType.air, 0);
        damageDict.Add(ElementType.fire, 0);
        damageDict.Add(ElementType.aether, 0);
        damageDict.Add(ElementType.none, 0);
    }

    public void Damage(float amt, ElementType eT ,bool damageOverTime = false)
    {
        if (damageOverTime)
            amt *= Time.deltaTime;

        switch (eT)
        {
            case ElementType.earth:
                break;
            case ElementType.water:
                break;
            case ElementType.air:
                break;
            case ElementType.fire:
                //Only the max damage from on fire source affects this instance
                damageDict[eT] = Mathf.Max(amt, damageDict[eT]);
                break;
            case ElementType.aether:
                break;
            case ElementType.none:
                break;
            default:
                //By default, damage is added to the other damage by same element
                damageDict[eT] += amt;
                break;
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    
}
