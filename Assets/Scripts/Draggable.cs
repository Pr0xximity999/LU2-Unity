using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

/*
* The GameObject also needs a collider otherwise OnMouseUpAsButton() can not be detected.
*/
public class Draggable: MonoBehaviour
{
    public Transform trans;
    private Vector3 _offset;
    public static bool DraggingObject = false;
    protected bool _draggingObject = false;

    
    public void Update()
    {
        DraggingObject = _draggingObject;
        if (_draggingObject)
        {
            //Drags while holding down, adds the offset to not let the item's middle snap to the cursor
            DragObject(trans, GetMousePosition() ,_offset);
        }
    }

    protected void DragObject(Transform trans, Vector3 targetPosition, Vector3 offset)
    {
        var realPositon = targetPosition + offset;
        
        //Snaps the location to the grid
        realPositon.x = (float)Math.Round(realPositon.x, 0);
        realPositon.y = (float)Math.Round(realPositon.y, 0);
        realPositon.z = (float)Math.Round(realPositon.z, 0);
        var gridposition = realPositon;
        trans.position = gridposition;
    }

    private void OnMouseDown()
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
