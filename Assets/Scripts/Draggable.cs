using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/*
* The GameObject also needs a collider otherwise OnMouseUpAsButton() can not be detected.
*/
public class Draggable: MonoBehaviour
{
    public Transform trans;
    public static bool DraggingObject = false;
    public int ZOffset;
    public Tilemap tilemap;
    
    protected bool _draggingObject = false;
    protected GameObject _deletedSelf;
    public GameObject Prefab;
    
    private Vector3 _offset;
    
    
    public void Update()
    {
        if (_draggingObject)
        {
            try
            {
                //Drags while holding down, adds the offset to not let the item's middle snap to the cursor
                DragObject(trans, GetMousePosition() ,_offset);
            }
            catch 
            {
                // nothing ever happens
            }
        }
    }

    protected void DragObject(Transform trans, Vector3 targetPosition, Vector3 offset)
    {
        var realPositon = targetPosition + offset;
        
        //Snaps the location to the grid
        realPositon.x = (float)Math.Round(realPositon.x, 1);
        realPositon.y = (float)Math.Round(realPositon.y, 1);
        realPositon.z = (realPositon.y - ZOffset) / 10; //Layering of depth
        var gridposition = realPositon;

        var cellPosition = tilemap.WorldToCell(targetPosition);
        TileBase tile = tilemap.GetTile(cellPosition);
        
        //Only move items on wood
        if (tile.name.ToLower().Contains("wood"))
        {
            trans.position = gridposition; 
        }
    }

    public void OnMouseDown()
    {
        //Gotta do it twice becasue otherwise id move all movables in the scene
        _draggingObject = true;
        DraggingObject = true;
        
        //Add a slight offset of the mouse position in relation to the draggable object
        _offset = trans.position - GetMousePosition(trans.position.z);
    }

    public void OnMouseUp()
    {
        //Gotta do it twice becasue otherwise id move all movables in the scene
        _draggingObject = false;
        DraggingObject = false;
        
        //Destroys object when it touches the destroy objects
        foreach (var gObject in GameObject.FindGameObjectsWithTag("PropDestroyer"))
        {
            //If the mouse hovers over a blacklisted item, stop doing the scroll and move
            RectTransform rectTransform = gObject.GetComponent<RectTransform>();
            if (rectTransform != null &&
                RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, null))
            {
                Destroy(_deletedSelf ?? gameObject); //byebye
            }
        }
    }

    /// <summary>
    /// Gets the current position of the mouse in reference to the world
    /// </summary>
    /// <param name="zOffset">The z position on which the mouse will be</param>
    /// <returns></returns>
    protected Vector3 GetMousePosition(float zOffset =0)
    {
        Vector3 positionInWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        positionInWorld.z = zOffset;
        return positionInWorld;
    }

}
