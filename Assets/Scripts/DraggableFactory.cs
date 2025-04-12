using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

/*
* The GameObject also needs a collider otherwise OnMouseUpAsButton() can not be detected.
*/
public class DraggableFactory: Draggable
{
    [SerializeField]
    public GameObject spawnObject;
    
    private Transform _trans;
    private Vector3 _offset;
    
    public new void Update()
    {
        if (_draggingObject)
        {
            //Drags while holding down, adds the offset to not let the item's middle snap to the cursor
            DragObject(_trans, GetMousePosition(), new());
        }
    }
    
    public void OnMouseDown()
    {
        _draggingObject = true;
        DraggingObject = true;
        
        //Spawns an object set by the serialized input field
        var spawnedObject = Instantiate(spawnObject);
        
        //Set the transform used when dragging to the spawned object
        _trans = spawnedObject.transform;
    }
}
