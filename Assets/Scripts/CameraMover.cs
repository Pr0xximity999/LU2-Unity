using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public Camera camera;
    public List<GameObject> interactBlacklist;

    public bool isDragging = false;
    public float scrollSensitivty = 5f;
    public static float MaxCamSize = 25;

    private bool locked = false;
    private Vector3 cameraStartPositon;
    private Vector3 mouseStartPosition;
    
    void Update()
    {
        //No moving around while holding an object >:(
        if (Draggable.DraggingObject) return;

        //Keep dragging while hovering over the blacklisted objects
        if (!isDragging)
        {
            //Check for objects that are blacklisted to interact with
            foreach (var gameObject in interactBlacklist)
            {
                //If the mouse hovers over a blacklisted item, stop doing the scroll and move
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                if (rectTransform != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, null))
                {
                    locked = true;
                    return;
                }
                else if(!Input.GetMouseButton(0))
                {
                    locked = false;
                }
            }
        }
        
        
        //Scrolling - zooming
        float scrolldelta = Input.GetAxis("Mouse ScrollWheel");
        if (!locked && math.abs(scrolldelta) > 0)
        {     
            //Clamps are there to not let the user zoom in too far or too wide
            camera.orthographicSize = math.clamp(camera.orthographicSize - scrolldelta * scrollSensitivty, 1, MaxCamSize);
        }
    
        //Initial set
        if (!locked && Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            cameraStartPositon = camera.transform.position;
            mouseStartPosition = camera.ScreenToViewportPoint(Input.mousePosition);
        }

        //Use the initial data to move the camera accordingly
        if (!locked && Input.GetMouseButton(0))
        {
            //Calculate offset from starting to current mouse position
            Vector3 mouseCurrentPosition = camera.ScreenToViewportPoint(Input.mousePosition);
            Vector3 offset = mouseCurrentPosition - mouseStartPosition;

            //Horizontal offset needs to a bit higher due to the screen's aspect ratio
            offset.x *= camera.aspect;
        
            //Move camera to new position
            camera.transform.position = cameraStartPositon - offset * (camera.orthographicSize * 2);
        
            //Sometimes the camera likes to transend dimensions so i gotta reset it to z = -10
            camera.transform.position = new()
            {
                x = camera.transform.position.x,
                y = camera.transform.position.y,
                z = -10f
            };
        }
        else
        {
            isDragging = false;
        }
    }
}
