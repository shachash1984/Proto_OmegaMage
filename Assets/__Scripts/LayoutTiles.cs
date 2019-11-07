using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileTex
{
    //This class enables us to define various textures for tiles
    public string str;
    public Texture2D tex;
}

[System.Serializable]
public class EnemyDef
{
    //Enemy Definition

    public string str;
    public GameObject go;
}

public class LayoutTiles : MonoBehaviour {
    static public LayoutTiles S;

    public TextAsset roomsText; // The Rooms.xml file
    public string roomNumber = "0"; //Current room # as a string 
    //^ roomNumber as string allows encoding in the XML & rooms 0-F
    public GameObject tilePrefab;
    public TileTex[] tileTextures;
    public GameObject portalPrefab;
    public EnemyDef[] enemyDefinitions;

    [Space]
    private bool firstRoom = true;
    public PT_XMLReader roomsXMLR;
    public PT_XMLHashList roomsXML;
    public Tile[,] tiles;
    public Transform tileAnchor;

    private void Start()
    {
        S = this;
        //Make a new GameObject to be the TileAnchor (the parent transform of all tiles). 
        //This keeps Tiles tidy in the Hierarchy pane;
        GameObject tAnc = new GameObject("TileAnchor");
        tileAnchor = tAnc.transform;

        //Read the XML
        roomsXMLR = new PT_XMLReader();
        roomsXMLR.Parse(roomsText.text);
        roomsXML = roomsXMLR.xml["xml"][0]["room"]; // Get all the <rooms>s

        //Build the 0 room
        BuildRoom(roomNumber);

    }

    public Texture2D GetTileTex(string tStr)
    {
        //Search through all the tileTextures for the proper string
        foreach (TileTex tTex in tileTextures)
        {
            if (tTex.str == tStr)
                return tTex.tex;
        }
        return null;
    }

    //Build a room based on room number. This is an alternative version of 
    //BuildRoom that grabs roomXML based on <room> num.
    public void BuildRoom(string rNumStr)
    {
        PT_XMLHashtable roomHT = null;
        for (int i = 0; i < roomsXML.Count; i++)
        {
            PT_XMLHashtable ht = roomsXML[i];
            if (ht.att("num") == rNumStr)
            {
                roomHT = ht;
                break;
            }
        }
        if(roomHT == null)
        {
            Utils.tr("Error", "LayoutTiles.BuildRoom()", "Room not found: " + rNumStr);
            return;
        }
        BuildRoom(roomHT);
    }

    //Build a room from an XML <room> entry
    public void BuildRoom(PT_XMLHashtable room)
    {
        //Destroy any old tiles
        foreach (Transform t in tileAnchor)
        {
            Destroy(t.gameObject);
        }

        //Move the mage out of the way
        Mage.S.pos = Vector3.left * 1000;
        Mage.S.ClearInput();

        string rNumStr = room.att("num");

        //Get the texture names for the floors and walls from <room> attribute
        string floorTexStr = room.att("floor");
        string wallTexStr = room.att("wall");

        //Split the room into rows of tiles based on line breaks in Rooms.xml
        string[] roomRows = room.text.Split('\n');

        //Trim tabs from the beginnings of lines. However, we're leaving spaces and underscores to allow for non-rectangular rooms.
        for (int i = 0; i < roomRows.Length; i++)
        {
            roomRows[i] = roomRows[i].Trim('\t');
        }

        //Clear the tiles array
        tiles = new Tile[100, 100]; //Arbitrary max room size is 100X100

        //Declare a number of local fields that we'll use later
        Tile ti;
        string type, rawType, tileTexStr;
        GameObject go;
        int height;
        float maxY = roomRows.Length - 1;
        List<Portal> portals = new List<Portal>();

        //These loops scan through each tile of each row of the room
        for (int y = 0; y < roomRows.Length; y++)
        {
            for (int x = 0; x < roomRows[y].Length; x++)
            {
                //Set defaults
                height = 0;
                tileTexStr = floorTexStr;

                //Get the character representing the tile
                type = rawType = roomRows[y][x].ToString();
                switch (rawType)
                {
                    case " ":
                    case "_":
                        //skip over empty space
                        continue;
                    case ".": // default floor
                        //Keep type "."
                        break;
                    case "|": //default wall
                        height = 1;
                        break;
                    default:
                        //Anything else will be interpreted as floor
                        break;
                }

                //Set the texture for floor or wall based on <room> attributes
                if (type == ".")
                    tileTexStr = floorTexStr;
                else if (type == "|")
                    tileTexStr = wallTexStr;

                //Instantiate a new TilePrefab
                go = Instantiate(tilePrefab) as GameObject;
                ti = go.GetComponent<Tile>();

                //Set the parent Transform to tileAnchor
                ti.transform.parent = tileAnchor;

                //set the position of the tile
                ti.pos = new Vector3(x, maxY - y, 0);
                tiles[x, y] = ti; // Add ti to the tiles 2D array

                //Set the type, height and texture of the Tile
                ti.type = type;
                ti.height = height;
                ti.tex = tileTexStr;

                //if the type is still rawType, continue to the next iteration
                ////**********//if (rawType == type)
                //**************    continue;

                //Check for specific entities in the room
                switch (rawType)
                {
                    case "X": // Starting position for the Mage
                        //Mage.S.pos = ti.pos;
                        if (firstRoom)
                        {
                            Mage.S.pos = ti.pos;
                            roomNumber = rNumStr;
                            firstRoom = false;
                        }
                        break;
                    case "0":
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                    case "8":
                    case "9":
                    case "A":
                    case "B":
                    case "C":
                    case "D":
                    case "E":
                    case "F":
                        //instantiate a portal
                        GameObject pGO = Instantiate(portalPrefab) as GameObject;
                        Portal p = pGO.GetComponent<Portal>();
                        p.pos = ti.pos;
                        p.transform.parent = tileAnchor; // attaching this to the tileAnchor means that the Portal will be destroyed when a new room is built
                        p.toRoom = rawType;
                        portals.Add(p);
                        break;
                    default:
                        //Try to see if theres an enemy for that letter
                        IEnemy en = EnemyFactory(rawType);
                        if (en == null)
                            break;
                        en.pos = ti.pos;
                        //Make en a child of tileAnchor so it's deleted when the next room is loaded
                        en.transform.parent = tileAnchor;
                        en.typeString = rawType;
                        break;
                }
            }
        }
        //Position the Mage
        foreach (Portal p in portals)
        {
            //if p.toRoom is the same as the room number the Mage just exited
            //then the Mage should enter this room through this Portal
            //Alternatively, if firstRoom == true and there was no X in the
            //room (as a default Mage starting point), move the mage to this
            //Portal as a backup (if you start from a room different from 0
            if(p.toRoom == roomNumber || firstRoom)
            {
                //if there's an X in the Room, firstRoom will be set to false
                Mage.S.StopWalking();
                Mage.S.pos = p.pos; //move mage to this portal location

                //Mage maintains facing from the previous room, so there
                //is no need to rotate to enter this room to face the right direction
                p.justArrived = true;
                firstRoom = false; // stops the mage to move to second portal

            }
        }
        roomNumber = rNumStr;
    }

    private IEnemy EnemyFactory(string sType)
    {
        //See if theres an EnemyDef with that sType
        GameObject prefab = null;
        foreach (EnemyDef ed in enemyDefinitions)
        {
            if (ed.str == sType)
            {
                prefab = ed.go;
                break;
            }
        }
        if(prefab == null)
        {
            //Utils.tr("LayoutTiles.EnemyFactory()", "No EnemyDef for: " + sType);
            return null;
        }

        GameObject go = Instantiate(prefab) as GameObject;
        IEnemy en = (IEnemy)go.GetComponent(typeof(IEnemy));
        return en;
    }
}
