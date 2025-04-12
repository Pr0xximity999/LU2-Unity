using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Tile = UnityEngine.WSA.Tile;

public class RoomManager : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase backgroundTile;
    public new Camera camera;
    
    private int _roomWidth;
    private int _roomHeight;
    private ApiManager _apiManager;
    private Room_2D _room;
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
                    _room = JsonConvert.DeserializeObject<Room_2D>(response);
                    _roomWidth = (int)_room.MaxLength;
                    _roomHeight = (int)_room.MaxHeight;
                    
                    //Clear the tilemap
                    tilemap.ClearAllTiles();
        
                    int posX = (int)tilemap.transform.position.x;
                    int posY = (int)tilemap.transform.position.y;

                    //Fill the background
                    for (int x = 0; x < _roomWidth + 1; x++)
                    {
                        for (int y = 0; y < _roomHeight + 1; y++)
                        {
                            //Get the current tile position + offset
                            var position = new Vector3Int(posX + x, posY + y, 0);
                            tilemap.SetTile(position, backgroundTile);
                        }
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

    public void ReturnToSelection()
    {
        SceneManager.LoadScene("Choice");
    }
}
