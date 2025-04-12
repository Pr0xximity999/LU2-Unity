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
            try
            {
                //Drags while holding down, adds the offset to not let the item's middle snap to the cursor
                DragObject(_trans, GetMousePosition(), new());
            }
            catch
            {
                // ignored
            }
        }
    }
    
    public void OnMouseDown()
    {
        _draggingObject = true;
        DraggingObject = true;
        
        //Spawns an object at the cursor
        var propcontainer = GameObject.Find("Prop container");
        var spawnedObject = Instantiate(spawnObject, propcontainer.transform, true);
        _deletedSelf = spawnedObject;
        
        //Set prefab data in the prop
        var draggable = spawnedObject.GetComponent<Draggable>();
        draggable.Prefab = spawnObject;
        draggable.tilemap = tilemap;

        
        //Set the transform used when dragging to the spawned object
        _trans = spawnedObject.transform;
    }
}
