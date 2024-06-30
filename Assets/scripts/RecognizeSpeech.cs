//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using UnityEngine;
using UnityEngine.UI;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;
using System.Linq;
using UnityEngine.Animations.Rigging;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
#if PLATFORM_IOS
using UnityEngine.iOS;
using System.Collections;
#endif
using TMPro;
public class RecognizeSpeech : MonoBehaviour
{
    public static RecognizeSpeech instance;
    public TextMeshProUGUI answer;
    private void Awake()
    {

        LoadKeys.Load(keys);
      /*
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
      */
    }
    // create instance
   public bool active = false;
    private bool micPermissionGranted = false;
    //public IntroController introControllerRecepit;

    private object threadLocker = new object();
    private bool recognitionStarted = false;
    private bool recognitionStopped = false;
    private string message;
    int lastSample = 0;
    int message_length = 0;
    AudioSource audioSource;
    public string language = "en-US";

    [HideInInspector]
    public int index = 0;

#if PLATFORM_ANDROID || PLATFORM_IOS
    // Required to manifest microphone permission, cf.
    // https://docs.unity3d.com/Manual/android-manifest.html
    private Microphone mic;
#endif

    private byte[] ConvertAudioClipDataToInt16ByteArray(float[] data)
    {
        MemoryStream dataStream = new MemoryStream();
        int x = sizeof(Int16);
        Int16 maxValue = Int16.MaxValue;
        int i = 0;
        while (i < data.Length)
        {
            dataStream.Write(BitConverter.GetBytes(Convert.ToInt16(data[i] * maxValue)), 0, x);
            ++i;
        }
        byte[] bytes = dataStream.ToArray();
        dataStream.Dispose();
        return bytes;
    }

  

    public bool FindWord(string word)
    {
        return message.Contains(word.ToLower());
    }

    public string getMessage()
    {
        return message;
    }

    public int Speaking()
    {
        message_length = message.Length;
        message = "";
        return message_length;
    }
   
    public keysController keys;


    public void configure()
    {
       

        {
            // Continue with normal initialization, Text and Button objects are present.
#if PLATFORM_ANDROID
            // Request to use the microphone, cf.
            // https://docs.unity3d.com/Manual/android-RequestingPermissions.html
            message = "";
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#elif PLATFORM_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }
#else
            micPermissionGranted = true;
            message = "";
#endif
            


           
            audioSource = GameObject.Find("MyAudioSource").GetComponent<AudioSource>();
            if (Microphone.devices.Length > 0)
            {
                audioSource.clip = Microphone.Start(null, true, 10, AudioSettings.outputSampleRate);
            }
            else
            {
                Debug.Log("This will crash!");
            }

            audioSource.Play();
           //   if(introControllerRecepit!=null)
           // introControllerRecepit.enabled = true;
        }
    }
    void Start()
    {
        configure();
    }

    void Disable()
    {
        message="";
        
    }

    void OnEnable()
    {
        message="";
    }
    int lastPosition, currentPosition;
    public DeepgramInstance _deepgramInstance;
    void Update()
    {
       
#if PLATFORM_ANDROID
        if (!micPermissionGranted && Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            micPermissionGranted = true;
            message = "";
        }
#elif PLATFORM_IOS
        if (!micPermissionGranted && Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            micPermissionGranted = true;
            message = "";
        }
#endif
        if (active)
        {
            if (!Microphone.IsRecording(Microphone.devices[0]))
            {

                audioSource.clip = Microphone.Start(Microphone.devices[0], true, 10, AudioSettings.outputSampleRate);
            }

            if (Microphone.IsRecording(Microphone.devices[0]))
            {
                if ((currentPosition = Microphone.GetPosition(null)) > 0)
                {
                    if (lastPosition > currentPosition)
                        lastPosition = 0;

                    if (currentPosition - lastPosition > 0)
                    {
                        float[] samples = new float[(currentPosition - lastPosition) * audioSource.clip.channels];
                        audioSource.clip.GetData(samples, lastPosition);

                        short[] samplesAsShorts = new short[(currentPosition - lastPosition) * audioSource.clip.channels];
                        for (int i = 0; i < samples.Length; i++)
                        {
                            samplesAsShorts[i] = f32_to_i16(samples[i]);
                        }

                        var samplesAsBytes = new byte[samplesAsShorts.Length * 2];
                        System.Buffer.BlockCopy(samplesAsShorts, 0, samplesAsBytes, 0, samplesAsBytes.Length);
                        _deepgramInstance.ProcessAudio(samplesAsBytes);

                        lastPosition = currentPosition;
                    }
                }
            }



            /*
            if (!Microphone.IsRecording(Microphone.devices[0]))
            {
                audioSource.clip = Microphone.Start(Microphone.devices[0], true, 200, 16000);
            }

            if (Microphone.IsRecording(Microphone.devices[0]) && recognitionStarted)
            {

                int pos = Microphone.GetPosition(Microphone.devices[0]);
                int diff = pos - lastSample;

                if (diff > 0)
                {
                    float[] samples = new float[diff * audioSource.clip.channels];

                    audioSource.clip.GetData(samples, lastSample);

                    byte[] ba = ConvertAudioClipDataToInt16ByteArray(samples);
                    if (ba.Length != 0)
                    {
                         //Debug.Log("pushStream.Write pos:" + Microphone.GetPosition(Microphone.devices[0]).ToString() + " length: " + ba.Length.ToString());
                        pushStream.Write(ba);
                    }
                }
                lastSample = pos;
            }
            else if (!Microphone.IsRecording(Microphone.devices[0]) && !recognitionStarted)
            {
                //  GameObject.Find("MyButton").GetComponentInChildren<Text>().text = "Start";
            }
            if (previousMessage != ans)
            {
                if (answer != null) { 
                answer.text += ans;
                previousMessage = ans;
                }
            }
           */
        }
        else
        {
          
            Microphone.End(null);
            lastSample = 0;
            if (answer != null)
            {
                answer.text = "";
            }
        }
       

        
    }
    string previousMessage = "";
    short f32_to_i16(float sample)
    {
        sample = sample * 32768;
        if (sample > 32767)
        {
            return 32767;
        }
        if (sample < -32768)
        {
            return -32768;
        }
        return (short)sample;
    }

}