using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using Newtonsoft.Json;




using TMPro;


public class GeneratedQuestions : MonoBehaviour
{
    [HideInInspector]
    public string response = "";
    string prompt;
    string[] prompts;
    public string[] promptsEng;
    string previousPrompt = "";
    public string examTopic = "";
    string[] talker;
    public string[] talkerEng;
    string openAIDomain = "";
    int index = 0;
    public TextMeshProUGUI question;
    public GameObject submit;
    private void Awake()
    {


        openAIDomain = "https://api.openai.com/v1/engines/text-curie-001/completions";
        talker = talkerEng;
        prompts = promptsEng;

        prompt = prompts[0];
        updatePrompt();

    }
    private void Start()
    {
    }
    void Update()
    {
    }
    public void updatePrompt()
    {
        response = "";
        string pattern = @"\b_examTopic\b";
        //for progression
        //  prompt = prompts[index];
        prompt = prompts[0];
        if (PlayerPrefs.GetString("ExamTopic") != "")
        {
            prompt = Regex.Replace(prompt, pattern, PlayerPrefs.GetString("ExamTopic"));
        }
        else
        {
            prompt = Regex.Replace(prompt, pattern, examTopic);

        }
        //prompt += "\n\n " + previousPrompt;

    }

    public async Task GetText(string playerInput, bool generate, string base_response)
    {
        print("start generated : " + Time.time);
        string promptInput = "";


        promptInput = prompt + $"\n\n{talker[1]} {playerInput} \n{talker[0]}";
        print($"prompt input: {promptInput}");

        if (generate)
        {
            openAiData openAIdata = new openAiData();
            openAIdata.prompt = promptInput;
            openAIdata.stop = talker;
            print(PlayerPrefs.GetString("UserId"));
            openAIdata.user = PlayerPrefs.GetString("UserId");
            string json = JsonUtility.ToJson(openAIdata);
            //https://api.openai.com/v1/engines/text-curie-001/completions for English
            using (UnityWebRequest www = UnityWebRequest.Put(openAIDomain, json))
            {
                www.method = "POST";
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", "Bearer " + LoadKeys.OPEN_AI_API_KEY.ToString());

                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    openAiResponse openAIresponse = JsonUtility.FromJson<openAiResponse>(www.downloadHandler.text);

                    if (openAIresponse.choices.Length > 0)
                    {

                        response = openAIresponse.choices[0].text;

                        print(response);
                        question.text = response;
                        submit.SetActive(true);
                    }
                }

            }
           
        }
        else
        {
            response = base_response;

        }

        prompt = promptInput + response;

    }
    

    public string GetResponse()
    {
        return response;
    }

    public string GetPrompt()
    {

        return prompt;
    }

    void Disable()
    {
        response = "";

    }
    private string apiKey = "AIzaSyDZkof4zsT_LqTJ-MqDORVGF9aeJgej4_Y";
    private string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=";

    private RequestBody requestBody;
    public string promptGemini = @"{
           ""contents"": [
               {
                   ""role"": ""Student"",
                   ""parts"": [
                       {
                           ""text"": ""The following is an exam about Geography in Egypt. Create new Conversation for the exam. The exam is friendly, and eager to get to know the student knowledge and asks a lot of questions.""
                       }
                   ]
               },
               {
                   ""role"": ""Gemini"",
                   ""parts"": [
                       {
                           ""text"": ""Let's start the Exam?""
                       }
                   ]
               }
           ]
       }";
    public async Task SendGenerateContentRequest(string playerInput, bool generate)
    {
        


        requestBody = JsonConvert.DeserializeObject<RequestBody>(promptGemini);

        /*
        // Output to verify the deserialization
        print("Contents:");
        foreach (var content in requestBody.Contents)
        {
            print($"Role: {content.Role}");
            foreach (var part in content.Parts)
            {
                print($"Text: {part.Text}");
            }
        }
        */
        Part p = new Part();
        p.Text = playerInput;
        requestBody.Contents[0].Parts.Add(p);





        promptGemini = JsonConvert.SerializeObject(requestBody);

        print(promptGemini);



        string url = apiUrl + apiKey;
        if (generate)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(promptGemini);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error: " + request.error);
                }
                else
                {
                    string responseText = request.downloadHandler.text;
                    print(responseText);
                    ResponseBody responseBody = JsonConvert.DeserializeObject<ResponseBody>(responseText);
                    if (responseBody.Candidates == null)
                    {
                       await SendGenerateContentRequest("", true);

                    }
                    else { 
                    print("Text : : : : "+responseBody.Candidates[0].content.Parts[0].Text);
                    Debug.Log("Response: " + responseText);
                    Part p2 = new Part();
                    string newLine = responseBody.Candidates[0].content.Parts[0].Text.Substring("Question:".Length);
                    newLine = newLine.Replace("**Answer:**", "");
                    newLine = newLine.Replace("**Answer**:", "");
                    p2.Text = newLine;


                    requestBody.Contents[1].Parts.Add(p2);
                    promptGemini = JsonConvert.SerializeObject(requestBody);
                    question.text = newLine;
                    submit.SetActive(true);
                    print("updated prompt : " + promptGemini);
                    }
                }
            }
        }
    }

}
public class Part
{
    [JsonProperty("text")]
    public string Text { get; set; }
}

public class Content
{
    [JsonProperty("role")]
    public string Role { get; set; }

    [JsonProperty("parts")]
    public List<Part> Parts { get; set; }
}

public class RequestBody
{
    [JsonProperty("contents")]
    public List<Content> Contents { get; set; }
}
public class ResponseBody
{
    [JsonProperty("candidates")]
    public List<Candidate> Candidates { get; set; }
}
public class Candidate
{
    [JsonProperty("content")]
    public Content content { get; set; }
}
