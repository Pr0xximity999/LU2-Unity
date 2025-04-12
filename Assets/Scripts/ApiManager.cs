using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using HttpMethod = DefaultNamespace.Models.HttpMethod;
using Newtonsoft.Json.Linq;

namespace DefaultNamespace
{
    public class ApiManager : MonoBehaviour
    {
        public string baseUrl;
        public string defaultLoginScene = "Login";

        public IEnumerator SendRequest(string location, HttpMethod method, Action<string, string> callback,
            string data = null, bool authorized = true, bool autoLogin = true)
        {
            using (UnityWebRequest req = new UnityWebRequest(baseUrl + '/' + location, method.ToString().ToUpper()))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                if (data != null)
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(data));
                    req.SetRequestHeader("Content-Type", "application/json");
                }

                yield return ManageRequest(req, callback, authorized, autoLogin);
            }
        }

        private IEnumerator ManageRequest(UnityWebRequest req, Action<string, string> callback, bool authorized, bool autoLogin)
        {
            if (authorized)
            {
                req.SetRequestHeader("Authorization", $"Bearer {MainManager.Instance.LoginResponse?.accessToken}");
            }

            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                if (req.downloadHandler == null)
                {
                    callback?.Invoke("", null);
                }
                else
                {
                    callback?.Invoke(req.downloadHandler.text, null);
                }
            }
            else
            {
                if (authorized && autoLogin)
                {
                    if (!HandleLoginError(req.downloadHandler.text, GetErrorRequest(req, callback), true))
                    {
                        Debug.LogError(GetErrorRequest(req, callback));
                    }
                    else
                    {
                        Debug.LogError(GetErrorRequest(req, callback));
                        callback?.Invoke(null, GetErrorRequest(req, callback));
                    }
                }
                else
                {
                    Debug.LogError(GetErrorRequest(req, callback));
                    callback?.Invoke(null, GetErrorRequest(req, callback));
                }

            }
        }
        
        public string GetErrorRequest(UnityWebRequest request, Action<string, string> callback)
        {
            if (request.downloadHandler != null && request.downloadHandler.text != null)
            {
                try
                {
                    var jsonObject = JObject.Parse(request.downloadHandler.text);
                    if (jsonObject["errors"] != null)
                    {
                        var firstError = jsonObject["errors"]
                            .First
                            .First[0]
                            .ToString();

                        return firstError;
                    }
                    else
                    {
                        return request.downloadHandler.text;
                    }
                }
                catch
                {
                    if (request.downloadHandler.text != "")
                    {
                        return request.downloadHandler.text;
                    } else
                    {
                        return request.error;
                    }
                }
            }
            else
            {
                return request.error;
            }
        }
        
        
        private bool HandleLoginError(string response, string error, bool autoLogin)
        {
            if (error == "HTTP/1.1 401 Unauthorized" || error == "Not logged in")
            {
                Debug.LogWarning("Login Session Illegal/Expired");
                if (autoLogin && error == "HTTP/1.1 401 Unauthorized" && MainManager.Instance.LoginResponse != null)
                {
                    Debug.Log("Trying to refresh token");
                    StartCoroutine(SendRequest("/account/refresh", HttpMethod.Post, (string response, string error) =>
                        {
                            if (error == null)
                            {
                                Debug.Log($"Trying to use new token: {response}");
                                LoginResponse decodedResponse = JsonUtility.FromJson<LoginResponse>(response);
                                LoginSaveFile values = new LoginSaveFile(decodedResponse);
                                MainManager.Instance.SetLoginCredentials(values);
                                response = JsonUtility.ToJson(values);
                                System.IO.File.WriteAllText(MainManager.Instance.LoginDataSaveLocation, response);
                                SceneManager.LoadScene(defaultLoginScene);
                            }
                            else
                            {
                                Debug.LogError($"No new sessiontoken: {error}");
                                SceneManager.LoadScene(defaultLoginScene);
                            }
                        }, JsonUtility.ToJson(new { refreshToken = MainManager.Instance.LoginResponse.refreshToken }), 
                        false));
                    return true;
                }
                else
                {
                    SceneManager.LoadScene(defaultLoginScene);
                    return true;
                }

            }
            return false;
        }
    }
}