using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;

using NativeWebSocket;

[System.Serializable]
public class DeepgramResponse
{
    public int[] channel_index;
    public bool is_final;
    public Channel channel;
}

[System.Serializable]
public class Channel
{
    public Alternative[] alternatives;
}

[System.Serializable]
public class Alternative
{
    public string transcript;
}

public class DeepgramInstance : MonoBehaviour
{
    WebSocket websocket;
    public TextMeshProUGUI answer;

    private async void ConnectToWebSocket()
    {
        Debug.Log("Connection starting!");

        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Token f1da870794345fb2221e23a3b73b46dc1bc6376e" }
        };
        websocket = new WebSocket("wss://api.deepgram.com/v1/listen?encoding=linear16&sample_rate=" + AudioSettings.outputSampleRate.ToString(), headers);

        websocket.OnOpen += () =>
        {
            Debug.Log("Connected to Deepgram!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
            Task.Delay(500).ContinueWith(t => ConnectToWebSocket()); // Attempt to reconnect after 5 seconds

        };

        websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("OnMessage: " + message);

            DeepgramResponse deepgramResponse = new DeepgramResponse();
            object boxedDeepgramResponse = deepgramResponse;
            EditorJsonUtility.FromJsonOverwrite(message, boxedDeepgramResponse);
            deepgramResponse = (DeepgramResponse)boxedDeepgramResponse;
            if (deepgramResponse.is_final)
            {
                var transcript = deepgramResponse.channel.alternatives[0].transcript;
                Debug.Log(transcript);
                answer.text += "  "+transcript;
            }
        };

        await Task.Run(() => websocket.Connect());
    }
    async void Start()
    {
        ConnectToWebSocket();

    }
    void Update()
    {
    #if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
    #endif
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    public async void ProcessAudio(byte[] audio)
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.Send(audio);
        }
        
    }
}

