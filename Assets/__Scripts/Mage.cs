using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.SceneManagement;

//The MPhase enum is used to track the phase of the mouse interaction
public enum MPhase { idle, down, drag}

public enum ElementType { earth, water, air, fire, aether, none = 6}

//MouseInfo stores information about the mouse in each fram of interaction
[System.Serializable]
public class MouseInfo
{
    public Vector3 loc; // 3D loc of the mouse near z=0
    public Vector3 screenLoc; //Screen position of the mouse
    public Ray ray; // Ray from the mouse into 3D space
    public float time; //Time this mouseInfo was recorded
    public RaycastHit hitInfo; // Info about what was hit by the ray
    public bool hit; // Whether the mouse was over any collider

    //These methods see if the mouseRay hits anything
    public RaycastHit Raycast()
    {
        hit = Physics.Raycast(ray, out hitInfo);
        return hitInfo;
    }

    public RaycastHit Raycast(int mask)
    {
        hit = Physics.Raycast(ray, out hitInfo, mask);
        return hitInfo;
    }
}

public class Mage : PT_MonoBehaviour {

    static public Mage S;
    static public bool DEBUG = false;

    public float mTapTime = 0.1f; // How long is considered a tap
    public GameObject tapIndicatorPrefab;
    public float mDragDist = 5; // Min dist in pixels to be a drag

    public float activeScreenWidth = 0.75f; // % of the screen to use
    public float speed = 2f;
    public float minDestinationDistance = 1f;

    public GameObject[] elementPrefabs;
    public float elementRotDist = 0.5f;
    public float elementRotSpeed = 0.5f;
    public int maxNumSelectedElements = 1;
    public Color[] elementColors;

    public float lineMinDelta = 0.1f;
    public float lineMaxDelta = 0.5f;
    public float lineMaxLength = 8f;
    public GameObject fireGroundSpellPrefab;
    public float health = 4;
    public float damageTime = -100; //Time that damage occured. It's set to -100 so that the mage doesnt act damaged immediately when the scene starts
    public float knockBackDist = 1;
    public float knockBackDur = 0.5f;
    public float invincibleDur = 0.5f;
    public int invTimesToBlink = 4;

    private bool invincibleBool = false;
    private bool knockBackBool = false;
    private Vector3 knockBackDir;
    private Transform viewCharacterTrans;
    protected Transform spellAnchor;
    public float totalLineLength;
    public List<Vector3> linePts;
    protected LineRenderer liner;
    protected float lineZ = -0.1f;

    public MPhase mPhase = MPhase.idle;
    public List<MouseInfo> mouseInfos = new List<MouseInfo>();
    public string actionStartTag; // ["Mage", "Ground", "Enemy"] 
    public bool walking = false;
    public Vector3 walkTarget;
    public Transform characterTrans;

    public List<Element> selectedElements = new List<Element>();

    private void Awake()
    {
        S = this;
        mPhase = MPhase.idle;

        //find the characterTrans to rotate with Fade()
        characterTrans = transform.Find("CharacterTrans");
        viewCharacterTrans = characterTrans.Find("View_Character");

        liner = GetComponent<LineRenderer>();
        liner.enabled = false;

        GameObject saGO = new GameObject("Spell Anchor");
        spellAnchor = saGO.transform;
    }

    private void Update()
    {
        //Find whether the mouse button 0 was pressed or released this frame
        bool b0Down = Input.GetMouseButtonDown(0);
        bool b0Up = Input.GetMouseButtonUp(0);

        // Handle all input here (except for inventory buttons)
        /*There are only a few possible actions;
         * 1. Tap on the ground to move to that point
         * 2. Drag on the ground with no spell selected to move to the Mage
         * 3. Drag on the ground with spell to cast along the ground
         * 4. Tap on an enemy to attack (or force-push away without an element
         * */

        //An example of using < to return a bool value
        
        bool inActiveArea = (float)Input.mousePosition.x / Screen.width < activeScreenWidth;

        // This is handled as an if statement instead of switch because a tap
        // can sometimes happen within a single frame
        if(mPhase == MPhase.idle)
        {
            if(b0Down && inActiveArea)
            {
                mouseInfos.Clear();
                AddMouseInfo();

                if (mouseInfos[0].hit)
                {
                    MouseDown();
                    mPhase = MPhase.down;
                }
            }
        }
        if (mPhase == MPhase.down)
        {
            AddMouseInfo();
            if (b0Up)
            {
                MouseTap();
                mPhase = MPhase.idle;
            }
            else if(Time.time - mouseInfos[0].time > mTapTime)
            {
                //If its been down longer than a tap, this may be a drag,
                // but to be a drag, It must also have moved a certain number of pixels on the screen
                float dragDist = (lastMouseInfo.screenLoc - mouseInfos[0].screenLoc).magnitude;
                if (dragDist >= mDragDist)
                    mPhase = MPhase.drag;

                //However, drag will immediatelt start after mTapTaime if there are no elements selected
                if(selectedElements.Count == 0)
                {
                    mPhase = MPhase.drag;
                }
            }
        }
        if(mPhase == MPhase.drag)
        {
            AddMouseInfo();
            if (b0Up)
            {
                MouseDragUp();
                mPhase = MPhase.idle;
            }
            else
                MouseDrag();
        }

        OrbitSelectedElements();
    }

    public void FixedUpdate()
    {
        if (invincibleBool)
        {
            float blinkU = (Time.time - damageTime) / invincibleDur;
            blinkU *= invTimesToBlink;
            blinkU %= 1.0f; // decimal remainder
            bool visible = (blinkU > 0.5f);
            if(Time.time - damageTime > invincibleDur)
            {
                invincibleBool = false;
                visible = true;
            }

            viewCharacterTrans.gameObject.SetActive(visible);
        }

        if (knockBackBool)
        {
            if (Time.time - damageTime > knockBackDur)
                knockBackBool = false;
            float knockBackSpeed = knockBackDist / knockBackDur;
            vel = knockBackDir * knockBackSpeed;
            return;
        }

        if (walking)
        {
            if ((walkTarget - pos).magnitude < minDestinationDistance * Time.fixedDeltaTime)
            {
                //if mage is very close to walkTarget, stop there
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

    public MouseInfo AddMouseInfo()
    {
        MouseInfo mInfo = new MouseInfo();
        mInfo.screenLoc = Input.mousePosition;
        mInfo.loc = Utils.mouseLoc; // Gets the position of the mouse at z=0
        mInfo.ray = Utils.mouseRay; // Gets the ray from the Camera through the mousePointer
        mInfo.time = Time.time;
        mInfo.Raycast(); 
        if(mouseInfos.Count == 0)
        {
            //If this is the first mouseInfo
            mouseInfos.Add(mInfo);
        }
        else
        {
            float lastTime = mouseInfos[mouseInfos.Count - 1].time;
            if(mInfo.time != lastTime)
            {
                //if tim has passed since the last mouseInfo
                mouseInfos.Add(mInfo);
            }
            //This time test is necessary because AddMouseInfo() could be called twice in one frame
        }
        return mInfo;
    }

    public MouseInfo lastMouseInfo
    {
        //Access to the latest MouseInfo
        get
        {
            if (mouseInfos.Count == 0)
                return null;
            return (mouseInfos[mouseInfos.Count - 1]);
        }
    }

    public void MouseDown()
    {
        if (DEBUG)
            Debug.Log("MageMouseDown()");
        GameObject clickedGO = mouseInfos[0].hitInfo.collider.gameObject;
        // if the mouse wasnt clicked on anything, this would throw an error
        // because hitInfo would be null. However, we know that MouseDown()
        // is only called when the mouse WAS clicking on something, 
        // so hitInfo is never null

        GameObject taggedParent = Utils.FindTaggedParent(clickedGO);
        if (taggedParent == null)
            actionStartTag = "";
        else
            actionStartTag = taggedParent.tag;
        //Should be either "Ground", "Mage", Or "Enemy"
    }

    public void MouseTap()
    {
        if (DEBUG)
            Debug.Log("MageMouseTap()");
        

        switch (actionStartTag)
        {
            case "Mage":
                break;
            case "Ground":
                //Move to tapped point at z=0 whether or not an element is selected
                WalkTo(lastMouseInfo.loc);
                ShowTap(lastMouseInfo.loc);
                break;
            default:
                break;
        }

       
    }

    public void MouseDrag()
    {
        if (DEBUG)
            Debug.Log("MageMouseDrag()");

        //Drag is meaningless unless the mouse started on the ground
        if (actionStartTag != "Ground")
            return;
        //if there is no element selected, the player should follow the mouse
        if (selectedElements.Count == 0)
        {
            //Continuosly walk toward the current mouseInfo pos
            WalkTo(mouseInfos[mouseInfos.Count - 1].loc);
        }
        else
        {
            //Ground spell, need to draw line
            AddPointToLiner(mouseInfos[mouseInfos.Count - 1].loc);
        }
    }

    public void MouseDragUp()
    {
        if (DEBUG)
            Debug.Log("MageMouseDragUp()");
        //Drag is meaningless unless the mouse started on the ground
        if (actionStartTag != "Ground")
            return;

        //if there is no element selected stop walking now
        if(selectedElements.Count == 0)
        {
            //stop walking when drag is stopped
            StopWalking();
        }
        else
        {
            //Cast the spell
            CastGroundSpell();
            //Clear the liner
            ClearLiner();
        }
    }

    private void CastGroundSpell()
    {
        if (selectedElements.Count == 0)
            return;
        switch (selectedElements[0].type) 
        {
            case ElementType.earth:
                break;
            case ElementType.water:
                break;
            case ElementType.air:
                break;
            case ElementType.fire:
                GameObject fireGO;
                foreach (Vector3 pt in linePts)
                {
                    fireGO = Instantiate(fireGroundSpellPrefab) as GameObject;
                    fireGO.transform.parent = spellAnchor;
                    fireGO.transform.position = pt;
                }
                break;
            case ElementType.aether:
                break;
            case ElementType.none:
                break;
            default:
                break;
        }
        ClearElements();
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
        //Use Atan2 to get the rotation around z that points the X-axis of _Mage:CharacterTrans toward poi
        float rZ = Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x);
        characterTrans.rotation = Quaternion.Euler(0, 0, rZ);
    }

    public void StopWalking()
    {
        walking = false;
        rigidbody.velocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject otherGo = collision.gameObject;

        //Collision with wall can also stop walking
        Tile ti = otherGo.GetComponent<Tile>();
        if (ti)
        {
            if(ti.height > 0)
            {
                //Then this ti is a wall and Mage should stop
                StopWalking();
            }
        }

        EnemyBug bug = collision.gameObject.GetComponent<EnemyBug>();
        //if otherGO is an EnemyBug, pass otherGO to collisionDamage()
        if (bug)
            CollisionDamage(bug);
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemySpiker spiker = other.GetComponent<EnemySpiker>();
        if (spiker != null)
            CollisionDamage(spiker);
    }

    private void CollisionDamage(IEnemy enemy)
    {
        //Dont take damage if you are invincible
        if (invincibleBool)
            return;
        StopWalking();
        ClearInput();
        health -= enemy.touchDamage;
        if (health <= 0)
        {
            Die();
            return;
        }

        damageTime = Time.time;
        knockBackBool = true;
        knockBackDir = (pos - enemy.pos).normalized;
        invincibleBool = true;
    }

    private void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ShowTap(Vector3 loc)
    {
        GameObject go = Instantiate(tapIndicatorPrefab) as GameObject;
        go.transform.position = loc;
    }

    //Chooses an Element_Sphere of elType and adds it to selectedElements
    public void SelectElement(ElementType elType)
    {
        if (elType == ElementType.none)
        {
            ClearElements();
            return;
        }

        if (maxNumSelectedElements == 1)
            ClearElements();

        if (selectedElements.Count >= maxNumSelectedElements)
            return;

        GameObject go = Instantiate(elementPrefabs[(int)elType]) as GameObject;
        Element el = go.GetComponent<Element>();
        el.transform.parent = transform;

        selectedElements.Add(el);
    }

    private void ClearElements()
    {
        foreach (Element el in selectedElements)
        {
            Destroy(el.gameObject);
        }
        selectedElements.Clear();
    }

    //Called every update
    private void OrbitSelectedElements()
    {
        if (selectedElements.Count == 0)
            return;

        Element el;
        Vector3 vec;
        float theta0, theta;
        float tau = Mathf.PI * 2; // tau is 360 degrees in radians

        //Divide the circle into the number of elements that are orbitting
        float rotPerElement = tau / selectedElements.Count;

        //The base rotation angle (theta0) is set based on time
        theta0 = elementRotDist * Time.time * tau;

        for (int i = 0; i < selectedElements.Count; i++)
        {
            //Determine the rotation angle for each element
            theta = theta0 + i * rotPerElement;
            el = selectedElements[i];

            //converting the angle into unit vector;
            vec = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0);

            //Multiplying the unit vector by the elementRotDist
            vec *= elementRotDist;

            //Raise the element to waist height
            vec.z = -0.5f;
            el.lPos = vec;
        }

    }

    //adds a new point to the line. igonres the point if it's too close to
    //existing ones and adds extra points if it's too far away
    private void AddPointToLiner(Vector3 pt)
    {
        //set the z of the pt to lineZ to elevate it slightly
        pt.z = lineZ;

        //always add the point if linePts is empty
        if(linePts.Count == 0)
        {
            linePts.Add(pt);
            totalLineLength = 0;
            return;
        }

        if (totalLineLength > lineMaxLength)
            return;

        //if there is a previous point (pt0), then find how far pt is from it
        Vector3 pt0 = linePts[linePts.Count - 1];
        Vector3 dir = pt - pt0;
        float delta = dir.magnitude;
        dir.Normalize();

        totalLineLength += delta;

        //if it's less than the min distance
        if(delta < lineMinDelta)
        {
            //then it's too close, dont add it
            return;
        }

        //if it's further than the max distance then extra points...
        if(delta > lineMinDelta)
        {
            //then add extra point in between
            float numToAdd = Mathf.Ceil(delta / lineMinDelta);
            float minDelta = delta / numToAdd;
            Vector3 ptMid;
            for (int i = 0; i < numToAdd; i++)
            {
                ptMid = pt0 + (dir * minDelta * i);
                linePts.Add(ptMid);
            }

            linePts.Add(pt);
            UpdateLiner();
        }
    }

    private void UpdateLiner()
    {
        //getting the type of the selected element
        int el = (int)selectedElements[0].type;

        //set the line color based on that type
        liner.startColor = elementColors[el];
        liner.endColor = elementColors[el];

        //Update the representation of the ground spell about to be cast
        liner.positionCount = linePts.Count;
        for (int i = 0; i < linePts.Count; i++)
        {
            liner.SetPosition(i, linePts[i]);
        }
        liner.enabled = true;
    }

    private void ClearLiner()
    {
        liner.enabled = false;
        linePts.Clear();
    }

    public void ClearInput()
    {
        mPhase = MPhase.idle;
    }

}
