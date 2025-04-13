using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using DefaultNamespace.Models;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomChoiceManager : MonoBehaviour
{
    private List<Room_2D> _rooms;
    private ApiManager _apiManager;
    private string[] roomIds = new string[5];
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Choice");
        _apiManager = FindFirstObjectByType<ApiManager>();
        ReloadRooms();
    }

    private void ReloadRooms()
    {
        //Get rooms data
        StartCoroutine(_apiManager.SendRequest("room/all", HttpMethod.Get, (response, error) =>
        {
            if (error == null)
            {
                _rooms = JsonConvert.DeserializeObject<List<Room_2D>>(response);
                
                //Sort rooms on position
                _rooms = _rooms.OrderBy(x => x.Position).ToList();
                
                //First deactivate all the rooms
                for (int i = 0; i < 5; i++)
                {
                    var roomObject = GameObject.Find("Rooms").transform.GetChild(i).gameObject;
                    roomObject.SetActive(false);
                    roomObject.transform.Find("Created room").gameObject.SetActive(false);
                    roomObject.transform.Find("No room").gameObject.SetActive(false);
                }

                //Then systematically re-enable the right ones
                for (int i = 0; i < 5; i++)
                {
                    var roomObject = GameObject.Find("Rooms").transform.GetChild(i).gameObject;
                    //Set every found room active on its created tab
                    if (_rooms.ElementAtOrDefault(i) != null)
                    {
                        var room = _rooms[i];
                        //Tag the object with its uuid for later use
                        roomIds[i] = room.Id;

                        room.Position = i;
                        
                        //Make the room panel visible
                        roomObject.SetActive(true);
                        var roomInfo = roomObject.transform.Find("Created room").gameObject;
                        roomInfo.SetActive(true);
                        
                        //Set the room title
                        var roomTitle = roomInfo.transform.Find("Title").GetComponent<TextMeshProUGUI>();
                        roomTitle.text = room.Name;
                        
                        //Set the room properties
                        var roomProperties = roomInfo.transform.Find("Properties").GetComponent<TextMeshProUGUI>();
                        roomProperties.text = $"Width: {room.MaxLength}\nHeight: {room.MaxHeight}";
                        continue;
                    }
                    //Set the first room to not exist active on its creation tab
                    roomObject.SetActive(true);
                    roomObject.transform.Find("No room").gameObject.SetActive(true);
                    break;
                }
            }
            else
            {
                Debug.Log($"Error while fetching rooms: {error}");
            }
        }));
    }
    
    public void LoadRoom(int roomPosition)
    {
        //Loads the selected room
        MainManager.Instance.currentRoomId = _rooms[roomPosition].Id;
        SceneManager.LoadScene("Room");
    }

    public void CreateRoom(int roomPosition)
    {
        var roomObject = GameObject.Find("Rooms").transform.GetChild(roomPosition).Find("No room");
        var title = roomObject.Find("Title").GetComponent<TMP_InputField>().text;
        var width_S = roomObject.Find("Width").GetComponent<TMP_InputField>().text;
        var height_S = roomObject.Find("Height").GetComponent<TMP_InputField>().text;
        var errorText = roomObject.Find("Error text").GetComponent<TextMeshProUGUI>();

        Debug.Log(title.Length);
        //Validate the title
        if (title.Length == 0)
        {
            errorText.text = "Room must have a title";
            return;
        }
        if (title.Length > 25)
        {
            errorText.text = "Title cannot be longer than 25 characters";
            return;
        }
        
        //title must be unique
        foreach (var roomIter in _rooms)
        {
            if (title == roomIter.Name)
            {
                errorText.text = "Room name must be unique";
                return;
            }
        }
        
        //Validate if the width and height are numbers
        if (!int.TryParse(width_S, out var width))
        {
            errorText.text = "Not a valid width";
            return;
        }
        if (!int.TryParse(height_S, out var height))
        {
            errorText.text = "Not a valid height";
            return;
        }

        if (width is < 20 or > 200)
        {
            errorText.text = "Width must be between 20 and 200";
            return;
        }
        
        if (height is < 10 or > 100)
        {
            errorText.text = "Height must be between 10 and 100";
            return;
        }
        var room = new Room_2D()
        {
            Name = title,
            MaxHeight = height,
            MaxLength = width,
            Position = roomPosition
        };

        var jsonData = JsonConvert.SerializeObject(room);

        StartCoroutine(_apiManager.SendRequest("room", HttpMethod.Post, (value, error) =>
        {
            if (error == null)
            {
                //Clear the panels in case of an edge case
                roomObject.Find("Title").GetComponent<TMP_InputField>().text = "";
                roomObject.Find("Width").GetComponent<TMP_InputField>().text = "";
                roomObject.Find("Height").GetComponent<TMP_InputField>().text = "";
                //Reload the room panels
                ReloadRooms();
            }
            else
            {
                errorText.text = "Something went wrong while creating room";
            }
        }, jsonData));
    }

    public void DeleteRoom(int roomPosition)
    {
        //Deletes the room
        var roomId = roomIds[roomPosition];
        
        StartCoroutine(_apiManager.SendRequest($"room/{roomId}", HttpMethod.Delete, (value, error) =>
        {
            if (error == null)
            {
                //Reload the room panels
                ReloadRooms();
            }
        }));
    }
    
    public void Logout()
    {
        //Cant directly call the main manager because of how it persists across scenes
        MainManager.Instance.Logout();
    }
}
