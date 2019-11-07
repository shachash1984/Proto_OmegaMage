using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : PT_MonoBehaviour {

    public string toRoom;
    public bool justArrived = false; // true if Mage has just teleported here

    private void OnTriggerEnter(Collider other)
    {
        if (justArrived)
            return;

        //Get the GameObject of the collider
        GameObject go = other.gameObject;
        //search up for tagged parent
        GameObject goP = Utils.FindTaggedParent(go);
        if (goP != null)
            go = goP;

        //if this isnt the Mage, return
        if (go.tag != "Mage")
            return;

        LayoutTiles.S.BuildRoom(toRoom);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Mage")
            justArrived = false;
    }
}
