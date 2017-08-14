using UnityEngine;
using System.Collections;

public class ClickableTile : MonoBehaviour {

	public int tileX;
	public int tileY;
	public TileMap map;

	void OnMouseUp() {
		Debug.Log ("Click!");

		map.MoveSelectedUnitTo(tileX, tileY);
	}

	void OnMouseOver(){
		if(Input.GetMouseButtonDown(1)){
			map.deleteBlock(tileX, tileY);
            Debug.Log("right clicked....");
    		}
		if (Input.GetKeyUp(KeyCode.H)){ //grass - coin
			map.deleteBlock(tileX, tileY);
			map.createBlock(tileX, tileY, 0);
            
        }
		if (Input.GetKeyUp(KeyCode.J)){ //death - sawmp
			map.deleteBlock(tileX, tileY);
			map.createBlock(tileX, tileY, 1);
            
        }
		if (Input.GetKeyUp(KeyCode.K)){ //block - mountain
			map.deleteBlock(tileX, tileY);
			map.createBlock(tileX, tileY, 2);
            

        }
		if (Input.GetKeyUp(KeyCode.L)){ //endTile
			map.deleteBlock(tileX, tileY);
			map.createBlock(tileX, tileY, 3);
            
        }
        if (Input.GetKeyUp(KeyCode.F))
        { //block-mountain but with collectable
            map.deleteBlock(tileX, tileY);
            map.createBlock(tileX, tileY, 2);
            map.createCollectable(tileX, tileY, 6);
            Debug.Log("tile " + tileX+ "," + tileY + ": 6");
        }
        if (Input.GetKeyUp(KeyCode.G))
        { //open but with collectable
            map.createCollectable(tileX, tileY, 7);
            Debug.Log("tile " + tileX + "," + tileY + ": 7");
        }
        //key,keyGate,coinedOrNot

    }

}
