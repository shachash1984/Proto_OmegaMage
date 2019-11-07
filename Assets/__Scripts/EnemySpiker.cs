using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpiker : PT_MonoBehaviour , IEnemy{

    public float speed = 5f;
    public string roomXMLString = "{";

    public Vector3 moveDir;
    public Transform characterTrans;

    [SerializeField] private float _touchDamage = 1;
    public float touchDamage
    {
        get { return _touchDamage; }
        set { _touchDamage = value; }
    }
    public string typeString
    {
        get { return roomXMLString; }
        set { roomXMLString = value; }
    }

    private void Awake()
    {
        characterTrans = transform.Find("CharacterTrans");
    }

    private void Start()
    {
        switch (roomXMLString)
        {
            case "^":
                moveDir = Vector3.up;
                break;
            case "v":
                moveDir = Vector3.down;
                break;
            case "{":
                moveDir = Vector3.left;
                break;
            case "}":
                moveDir = Vector3.right;
                break;
            default:
                break;
        }
    }

    private void FixedUpdate()
    {
        rigidbody.velocity = moveDir * speed;
    }

    public void Damage(float amt, ElementType eT, bool damageOverTime = false)
    {
        //Nothing damages the EnemySpiker
    }

    private void OnTriggerEnter(Collider other)
    {
        //Check to see if a wall was hit
        GameObject go = Utils.FindTaggedParent(other.gameObject);
        if (go == null)
            return;

        if(go.tag == "Ground")
        {
            //Make sure that the ground tile is in the direction we're moving using Vector3.Dot
            float dot = Vector3.Dot(moveDir, go.transform.position - pos);
            if (dot > 0)
                moveDir *= -1; // reverse direction
        }
    }
}
