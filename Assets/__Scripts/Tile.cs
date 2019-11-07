using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : PT_MonoBehaviour {

    #region Public Fields
    public string type;
    #endregion

    #region Hidden Private Fields
    private string _tex;
    private int _height = 0;
    private Vector3 _pos;
    #endregion

    #region Properties
    //height moves the tile up or down. Walls have height=1
    public int height
    {
        get { return _height; }
        set
        {
            _height = value;
            AdjustHeight();
        }
    }
    
    public string tex
    {
        get { return _tex; }
        set
        {
            _tex = value;
            name = "TilePrefab_" + _tex;
            Texture2D t2D = LayoutTiles.S.GetTileTex(_tex);
            if (t2D == null)
                Utils.tr("Error", "Tile.type{set}=", value, "No matching Texture2D int LayoutTiles.S.tileTextures!");
            else
                renderer.material.mainTexture = t2D;
        }
    }

    new public Vector3 pos
    {
        get { return _pos; }
        set
        {
            _pos = value;
            AdjustHeight();
        }
    }
    #endregion

    #region Methods
    public void AdjustHeight()
    {
        //Moves the block up or down based on _height
        Vector3 vertOffset = Vector3.back * (_height - 0.5f);
        //The -0.5f shifts the Tile down 0.5 units so that its top surface is at z=0 when pos.z=0 and height=0
        transform.position = _pos + vertOffset;
    }



    #endregion
}
