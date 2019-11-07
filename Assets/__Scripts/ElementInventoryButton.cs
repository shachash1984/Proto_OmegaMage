using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementInventoryButton : MonoBehaviour {

    public ElementType type;

    private void Awake()
    {
        //parsing the first char of the name of this GameObject into an int
        char c = gameObject.name[0];
        string s = c.ToString();
        int typeNum = int.Parse(s);

        //cast to element type
        type = (ElementType)typeNum;
    }

    private void OnMouseUpAsButton()
    {
        //Tell the mage to add this element type
        Mage.S.SelectElement(type);
    }
}
