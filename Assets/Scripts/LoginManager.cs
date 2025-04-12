using System.Collections;
using System.Text.RegularExpressions;
using DefaultNamespace;
using DefaultNamespace.Models;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    private string _activePanel = "Login";
    public GameObject loginPanel;
    public GameObject registerPanel;

    public TextMeshProUGUI registerEmailErrorText;
    public TMP_InputField registerEmailField;
    
    public TextMeshProUGUI registerPasswordErrorText;
    public TMP_InputField registerPasswordField;
    
    public TextMeshProUGUI registerSecondPasswordErrorText;
    public TMP_InputField registerSecondPasswordField;

    public TextMeshProUGUI registerStatusText;
    public TextMeshProUGUI loginStatusText;
    
    public TMP_InputField loginEmailField;
    public TMP_InputField loginPasswordField;

    private ApiManager _apiManager;

    private void Start()
    {
        _apiManager = FindFirstObjectByType<ApiManager>();
        
        if (SceneManager.GetActiveScene().name != "Login")
        {
            MainManager.Instance.NavigationScene = SceneManager.GetActiveScene().name;
        }
        StartCoroutine(DelayedRefreshSessionToken());
    }

    public void SwitchPanel()
    {
        //Switches the active login/register panel
        if (_activePanel == "Login")
        {
            loginPanel.SetActive(false);
            registerPanel.SetActive(true);
            _activePanel = "Register";
        }
        else if (_activePanel == "Register")
        {
            loginPanel.SetActive(true);
            registerPanel.SetActive(false);
            _activePanel = "Login";
        }
        Debug.Log(_activePanel);
    }

    public void Register()
    {
        Debug.Log("Registering...");
        
        //Clear error fields
        registerEmailErrorText.text = "";
        registerPasswordErrorText.text = "";
        registerSecondPasswordErrorText.text = "";
        
        //Ease of access
        var registerEmail = registerEmailField.text;
        var registerPassword = registerPasswordField.text;
        var registerPasswordRepeat = registerSecondPasswordField.text;
        
        //Validates email
        if (!Validator.IsEmailValid(registerEmail))
        {
            registerEmailErrorText.text = "Invalid email";
            return;
        }

        //Validates password
        if (!Validator.IsPasswordValid(registerPassword))
        {
            if (registerPassword.Length < 10)
            {
                registerPasswordErrorText.text = "Password must be 10+ characters";
                return;
            }
            else
            {
                registerPasswordErrorText.text = "Password must have 1 lowercase, 1 uppercase, 1 number and 1 special character";
                return;
            }
        }

        //Checks if repeat password is the same
        if (registerPassword != registerPasswordRepeat)
        {
            registerSecondPasswordErrorText.text = "Passwords do not match";
            return;
        }
        
        //Serialize the data into a json
        var data = JsonConvert.SerializeObject(new
        {
            email = registerEmail,
            password = registerPassword
        });
        //Send the register request
        StartCoroutine(_apiManager.SendRequest("account/register", HttpMethod.Post, (response, error) =>
        {
            registerStatusText.text = "Connecting...";
            if (error == null)
            {
                Debug.Log(response);
                //If all went well, log in the user
                Login(registerEmail, registerPassword);
            }
            else
            {
                registerStatusText.color = Color.red;
                registerStatusText.text = error;
            }
        }, data, false));
    }

    public void Login() //Unity buttons cannot call methods with optionals or something
    {
        Login(null, null);
    }
    public void Login(string email=null, string password=null) 
    {
        Debug.Log("Loggin in..."); 
        loginStatusText.color = Color.white;
        loginStatusText.text = "Connecting...";

        var loginEmail = email ?? loginEmailField.text;
        var loginPassword = password ?? loginPasswordField.text;
        
        //Serialize the data into a json
        var data = JsonConvert.SerializeObject(new
        {
            email = loginEmail,
            password = loginPassword
        });

        StartCoroutine(_apiManager.SendRequest("account/login", HttpMethod.Post, (response, error) =>
        {
            if (error == null)
            {
                //load next scene
                SceneManager.LoadScene("Choice");
                
                //Save the session token
                LoginResponse decodedResponse = JsonConvert.DeserializeObject<LoginResponse>(response);
                LoginSaveFile values = new LoginSaveFile(decodedResponse);
                MainManager.Instance.SetLoginCredentials(values);
                response = JsonConvert.SerializeObject(values);
                System.IO.File.WriteAllText(MainManager.Instance.LoginDataSaveLocation, response);
                
                //Set the user's id guid
                 _apiManager.SendRequest("account/id", HttpMethod.Get, (response2, error2) =>
                 {
                     if (error2 == null)
                     {
                         MainManager.Instance.userId = response2;
                     }
                     else
                     {
                         loginStatusText.color = Color.red;
                         loginStatusText.text = "Something went wrong, please try again later";
                     }
                 });
            }
            else if(_activePanel == "Login")
            {
                loginStatusText.color = Color.red;
                loginStatusText.text = "Username or password are incorrect";
            }
        }, data, false));
    }
    
    IEnumerator DelayedRefreshSessionToken()
    {
        yield return new WaitForSeconds(1f); 
        RefreshSessionToken();
    }
    
    private void RefreshSessionToken()
    {       

        StartCoroutine(_apiManager.SendRequest("account/checkAccessToken", HttpMethod.Get, (response, error) =>
        {
            if (error == null)
            {
                if (!string.IsNullOrEmpty(MainManager.Instance.NavigationScene))
                {
                    SceneManager.LoadScene(MainManager.Instance.NavigationScene);
                }
                else
                {
                    SceneManager.LoadScene("Choice");
                }
            }
            else
            {
                if (MainManager.Instance.LoginResponse != null)
                {
                    StartCoroutine(_apiManager.SendRequest("account/refresh", HttpMethod.Post, (string response, string error) =>
                    {
                        if (error == null)
                        {
                            Debug.Log($"Trying to use new token: {response}");
                            LoginResponse decodedResponse = JsonConvert.DeserializeObject<LoginResponse>(response);
                            LoginSaveFile values = new LoginSaveFile(decodedResponse);
                            MainManager.Instance.SetLoginCredentials(values);
                            response = JsonConvert.SerializeObject(values);
                            System.IO.File.WriteAllText(MainManager.Instance.LoginDataSaveLocation, response);
                            if (!string.IsNullOrEmpty(MainManager.Instance.NavigationScene))
                            {
                                SceneManager.LoadScene(MainManager.Instance.NavigationScene);
                            }
                            else
                            {
                                SceneManager.LoadScene("Choice");
                            }
                        }
                        else
                        {
                            string filePath = MainManager.Instance.LoginDataSaveLocation;
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                    },
                    JsonConvert.SerializeObject(new { refreshToken = MainManager.Instance.LoginResponse.refreshToken }),
                    false));
                }
                else
                {
                    string filePath = MainManager.Instance.LoginDataSaveLocation;
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

            }
        }, MainManager.Instance.LoginResponse?.refreshToken, true, false));
    }
}

public class LoginResponse
{
    public string tokenType { get; set; }
    public string accessToken { get; set; }
    public int expiresIn { get; set; }
    public string refreshToken { get; set; }
}

public class LoginSaveFile: LoginResponse
{
    public LoginSaveFile(LoginResponse response)
    {
        this.tokenType = response.tokenType;
        this.accessToken = response.accessToken;
        this.expiresIn = response.expiresIn;
        this.refreshToken = response.refreshToken;
    }

    public LoginSaveFile()
    {
        
    }
}

public class Validator
{
    /// <summary>
    /// Validates a password based on a minimum 10 chars, at least one lowercase, uppercase, digit, and special character
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    public static bool IsPasswordValid(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{10,}$";
        return Regex.IsMatch(password, pattern);
    }

    
    /// <summary>
    /// Validates that the mail conforms to this cool regex i found
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public static bool IsEmailValid(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }
}
