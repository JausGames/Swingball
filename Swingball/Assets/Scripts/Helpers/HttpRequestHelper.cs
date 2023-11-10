using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;

public class HttpRequestHelper : MonoBehaviour
{
    const string API_URL = "http://localhost:8080/servers/find";


    public IEnumerator GetServerList()
    {
        UnityWebRequest www = UnityWebRequest.Get(API_URL);
        //www.SetRequestHeader("USERKEY", MazeUser.GetInstance().GetApiKey());
        yield return www.SendWebRequest();
        string output = null;
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("HttpRequestHelper, GetMazeList: error : " + www.error);
            yield return null;
        }
        else
        {
            output = www.downloadHandler.text;
            yield return output;
            // Show results as text
            Debug.Log("HttpRequestHelper, GetMazeList : success : " + output);

            // Or retrieve results as binary data
            byte[] results = www.downloadHandler.data;
        }
    }
}