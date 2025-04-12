using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.Models;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Tile = UnityEngine.WSA.Tile;

public class RoomManager : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase backgroundTile;
    public new Camera camera;
    public TextMeshProUGUI statusText;
    public static Room_2D Room;
    
    private int _roomWidth;
    private int _roomHeight;
    private ApiManager _apiManager;
    private List<Object_2D> _objects;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _apiManager = FindFirstObjectByType<ApiManager>();

        StartCoroutine(_apiManager.SendRequest($"room/{MainManager.Instance.currentRoomId}",
            HttpMethod.Get, (response, error) =>
            {
                if (error == null)
                {
                    Room = JsonConvert.DeserializeObject<Room_2D>(response);
                    _roomWidth = (int)Room.MaxLength;
                    _roomHeight = (int)Room.MaxHeight;
                    
                    //Clear the tilemap
                    tilemap.ClearAllTiles();
        
                    int posX = (int)tilemap.transform.position.x;
                    int posY = (int)tilemap.transform.position.y;

                    //Fill the tiles
                    for (int x = 0; x < _roomWidth; x++)
                    {
                        for (int y = 0; y < _roomHeight; y++)
                        {
                            //Get the current tile position + offset
                            var position = new Vector3Int(posX + x, posY + y, 0);
                            
                            //Set background tile
                            tilemap.SetTile(position, backgroundTile);
                        }
                    }
                    
                    //put the objects in the scene
                    //Check if object is in this place
                    foreach (var object2D in Room.objects)
                    {
                        //Load the prop from prefab name
                        var gObject = Resources.Load<GameObject>(object2D.Prefab_Id);
                        var propcontainer = GameObject.Find("Prop container");
                        var prop = Instantiate(gObject, propcontainer.transform, true);
                        var draggabable=prop.GetComponent<Draggable>();
                        draggabable.Prefab = gObject;
                        
                        var pos = prop.transform.position;
                        pos.x = object2D.PositionX;
                        pos.y = object2D.PositionY;
                        pos.z = (pos.y -draggabable.ZOffset) / 10; //Layering of depth
                        prop.transform.position = pos;
                    }

                    camera.orthographicSize = ((_roomWidth + _roomHeight) / 2f) / 2.5f;
                    CameraMover.MaxCamSize = camera.orthographicSize;
                    camera.transform.position = new Vector3()
                    {
                        x = posX + (_roomWidth / 2f),
                        y = posY + (_roomHeight / 2f),
                        z = -10
                    };
                }
                else
                {
                    //Honestly idk what to do if the loading of the rooms fail
                    SceneManager.LoadScene("Choice");
                }
            }));
    }

    private void Update()
    {
        Vector3 movement = new();
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            movement.y += .1f;
        }        
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            movement.x -= .1f;
        }        
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            movement.x += .1f;
        }        
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            movement.y -= .1f;
        }

        camera.transform.position += movement.normalized;
    }

    public void Save()
    {
        List<Object_2D> props = new();
        
        //Get all props from the scene
        foreach (Transform child in GameObject.Find("Prop container").transform)
        {
            
            props.Add(new()
            {
                Prefab_Id = child.GetComponent<Draggable>().Prefab.name,
                PositionX = child.transform.position.x,
                PositionY = child.transform.position.y,
                ScaleX = 1,
                ScaleY = 1,
                RotationZ = 0,
                Room2D_Id = Room.Id
            });
        }
        Room.objects = props;
        var data = JsonConvert.SerializeObject(props);
        
        //Set status text
        statusText.color = Color.white;
        statusText.text = "Saving...";
        
        //Save room data
        StartCoroutine(_apiManager.SendRequest($"Object/multiple/{MainManager.Instance.currentRoomId}",
            HttpMethod.Post, (response, error) =>
            {
                if (error == null)
                {
                    statusText.color = Color.green;
                    statusText.text = "Saved successfully!";
                }
                else
                {
                    //Honestly idk what to do if the loading of the rooms fail
                    statusText.color = Color.red;
                    statusText.text = "Something went wrong...";
                }
            }, data));
    }

    public void Quit()
    {
        SceneManager.LoadScene("Choice");
    }
    
    public void SaveAndQuit()
    {
        Save();
        StartCoroutine(Wait(1)); //Silly unity
        Quit();
    }

    private IEnumerator Wait(int seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
}
