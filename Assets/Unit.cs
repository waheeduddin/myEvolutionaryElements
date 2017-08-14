using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour {

    public int tileX;
    public int tileY;
    public TileMap map;

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.I))
        {
            Debug.Log("x position is: " + tileX);
            Debug.Log("y position is: " + tileY);

        }
        if (Input.GetKeyUp(KeyCode.A))
        {
		    if(tileX > 0)
                map.MoveSelectedUnitTo(tileX - 1, tileY);
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
		    if(tileY > 0)
                map.MoveSelectedUnitTo(tileX, tileY - 1);
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
		    if(tileY < map.mapSizeY)
                map.MoveSelectedUnitTo(tileX, tileY + 1);
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
		    if(tileX < map.mapSizeX)
                map.MoveSelectedUnitTo(tileX + 1 , tileY);
        }
        
    }
}
