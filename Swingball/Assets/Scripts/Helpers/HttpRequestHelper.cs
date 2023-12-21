using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;

public class HttpRequestHelper : MonoBehaviour
{
    const string API_URL = "https://swingball-mmapp.azurewebsites.net/api/findserver";


    public IEnumerator GetServer(PlayerGameInfo playerGameInfo)
    {    
        // Serialize the playerGameInfo object to JSON
        string jsonBody = JsonUtility.ToJson(playerGameInfo);

        // Create a UnityWebRequest for POST, with the JSON body
        UnityWebRequest www = new UnityWebRequest(API_URL, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonBody);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        // Set the content type header to application/json
        www.SetRequestHeader("Content-Type", "application/json");
        //www.SetRequestHeader("USERKEY", MazeUser.GetInstance().GetApiKey()); // Use if you need to set a custom header

        //www.SetRequestHeader("USERKEY", MazeUser.GetInstance().GetApiKey());
        yield return www.SendWebRequest();
        string output = null;
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("HttpRequestHelper, GetServer: error : " + www.error);
            yield return null;
        }
        else
        {
            output = www.downloadHandler.text;
            yield return output;
            // Show results as text
            Debug.Log("HttpRequestHelper, GetServer : success : " + output);

            // Or retrieve results as binary data
            byte[] results = www.downloadHandler.data;
        }
    }
}