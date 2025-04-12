using DefaultNamespace;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class MainManager : MonoBehaviour
{
    public static MainManager Instance;
    public LoginSaveFile LoginResponse; 
    public string NavigationScene; // the scene to go back to after login
    public string LoginDataSaveLocation = "UserSettings/playerLogin.json";
    public string currentRoomId;
    public string userId;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    
    public void SetLoginCredentials(LoginSaveFile loginResponse)
    {
        MainManager.Instance.LoginResponse = loginResponse;
    }

    private void Start()
    {
        string filePath = LoginDataSaveLocation;
        if (System.IO.File.Exists(filePath))
        {
            string jsonString = System.IO.File.ReadAllText(filePath);
            LoginResponse = JsonConvert.DeserializeObject<LoginSaveFile>(jsonString);
        }
        else
        {
            Debug.Log("No login file data found.");
        }
    }

    public void Logout()
    {
        LoginResponse = null;
        
        if (System.IO.File.Exists(LoginDataSaveLocation))
        {
            System.IO.File.Delete(LoginDataSaveLocation);
        }
        NavigationScene = null;
        SceneManager.LoadScene("Login");
        
    }
}