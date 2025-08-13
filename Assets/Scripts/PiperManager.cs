using System;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using Unity.InferenceEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

[RequireComponent(typeof(AudioSource))]
public class PiperManager : MonoBehaviour
{
    public ModelAsset modelAsset;
    public ESpeakTokenizer tokenizer;

    public Text voiceNameText;

    private Worker engine;
    private AudioSource audioSource;
    private bool isInitialized = false;

    private bool hasSidKey = false;


    [Range(0.0f, 1.0f)] public float commaDelay = 0.1f;
    [Range(0.0f, 1.0f)] public float periodDelay = 0.5f;
    [Range(0.0f, 1.0f)] public float questionExclamationDelay = 0.6f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component not found! It will be added automatically.");
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        StartCoroutine(InitializePiper());
    }

    private IEnumerator InitializePiper()
    {
        string espeakDataPath;

        #if UNITY_ANDROID && !UNITY_EDITOR
            espeakDataPath = Path.Combine(Application.persistentDataPath, "espeak-ng-data");

            if (!Directory.Exists(espeakDataPath))
            {
                Debug.Log("Android: eSpeak data not found in persistentDataPath. Starting copy process...");

                string zipSourcePath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data.zip");
                string zipDestPath = Path.Combine(Application.persistentDataPath, "espeak-ng-data.zip");

                using (UnityWebRequest www = UnityWebRequest.Get(zipSourcePath))
                {
                    yield return www.SendWebRequest();

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to load espeak-ng-data.zip from StreamingAssets: {www.error}");
                        yield break;
                    }

                    File.WriteAllBytes(zipDestPath, www.downloadHandler.data);

                    try
                    {
                        ZipFile.ExtractToDirectory(zipDestPath, Application.persistentDataPath);
                        Debug.Log("eSpeak data successfully unzipped to persistentDataPath.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error unzipping eSpeak data: {e.Message}");
                        yield break;
                    }
                    finally
                    {
                        if (File.Exists(zipDestPath))
                        {
                            File.Delete(zipDestPath);
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Android: eSpeak data already exists in persistentDataPath.");
            }
        #else
            espeakDataPath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data");
            Debug.Log($"Editor/Standalone: Using eSpeak data directly from StreamingAssets: {espeakDataPath}");
            yield return null;
        #endif

        InitializeESpeak(espeakDataPath);

        var model = ModelLoader.Load(modelAsset);
        engine = new Worker(model, BackendType.CPU);

        if (model.inputs.Count == 4 && model.inputs[3].name == "sid")
        {
            hasSidKey = true;
        }

        voiceNameText.text = $"Model: {modelAsset.name}";

        Debug.Log("Piper Manager initialized.");
        isInitialized = true;

        _WarmupModel();
        Debug.Log("Finished warmup.");
    }

    private void InitializeESpeak(string dataPath)
    {
        int initResult = ESpeakNG.espeak_Initialize(0, 0, dataPath, 0);

        if (initResult > 0)
        {
            Debug.Log($"[PiperManager] eSpeak-ng Initialization SUCCEEDED. Data path: {dataPath}");

            if (tokenizer == null || string.IsNullOrEmpty(tokenizer.Voice))
            {
                Debug.LogError("[PiperManager] Tokenizer is not assigned or has no voice name.");
                return;
            }

            string voiceName = tokenizer.Voice;
            int voiceResult = ESpeakNG.espeak_SetVoiceByName(voiceName);

            if (voiceResult == 0)
                Debug.Log($"[PiperManager] Set voice to '{voiceName}' SUCCEEDED.");
            else
                Debug.LogError($"[PiperManager] Set voice to '{voiceName}' FAILED. Error code: {voiceResult}");
        }
        else
        {
            Debug.LogError($"[PiperManager] eSpeak-ng Initialization FAILED. Error code: {initResult}");
        }
    }

    void OnDestroy()
    {
        engine?.Dispose();
    }

    public void OnSubmitText(Text textField)
    {
        if (string.IsNullOrEmpty(textField.text))
        {
            Debug.LogError("Input text is empty. Please enter some text.");
            return;
        }

        Debug.Log($"Input text: {textField.text}");
        SynthesizeAndPlay(textField.text);
    }

    public void SynthesizeAndPlay(string text)
    {
        if (!isInitialized)
        {
            Debug.LogError("Piper Manager is not initialized.");
            return;
        }
        StartCoroutine(SynthesizeAndPlayCoroutine(text));
    }

    private IEnumerator SynthesizeAndPlayCoroutine(string text)
    {
        string delayPunctuationPattern = @"([,.?!;:])";
        string nonDelayPunctuationPattern = @"[^\w\s,.?!;:]";

        string[] parts = Regex.Split(text, delayPunctuationPattern);

        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part.Trim()))
            {
                continue;
            }

            bool isDelayPunctuation = Regex.IsMatch(part, "^" + delayPunctuationPattern + "$");

            if (isDelayPunctuation)
            {
                float delay = 0f;
                switch (part)
                {
                    case ",":
                    case ";":
                    case ":":
                        delay = commaDelay;
                        break;
                    case ".":
                        delay = periodDelay;
                        break;
                    case "?":
                    case "!":
                        delay = questionExclamationDelay;
                        break;
                }
                if (delay > 0)
                {
                    Debug.Log($"Pausing for '{part}' for {delay} seconds.");
                    yield return new WaitForSeconds(delay);
                }
            }
            else
            {
                string cleanedChunk = Regex.Replace(part, nonDelayPunctuationPattern, " ");
                cleanedChunk = cleanedChunk.Trim();

                if (!string.IsNullOrEmpty(cleanedChunk))
                {
                    Debug.Log($"Processing text chunk: \"{cleanedChunk}\"");
                    _SynthesizeAndPlayChunk(cleanedChunk);
                    yield return new WaitWhile(() => audioSource.isPlaying);
                }
            }
        }
        Debug.Log("Finished playing all chunks.");
    }

    private void _SynthesizeAndPlayChunk(string textChunk)
    {
        string phonemeStr = Phonemize(textChunk);
        if (string.IsNullOrEmpty(phonemeStr))
        {
            Debug.LogError($"Phoneme conversion failed for chunk: \"{textChunk}\"");
            return;
        }

        string[] phonemeArray = phonemeStr.Trim().Select(c => c.ToString()).ToArray();
        int[] phonemeTokens = tokenizer.Tokenize(phonemeArray);

        float[] scales = tokenizer.GetInferenceParams();
        int[] inputLength = { phonemeTokens.Length };

        Debug.Log($"Model inputs prepared. Token count: {inputLength[0]}, Scales: [{string.Join(", ", scales)}]");

        using var phonemesTensor = new Tensor<int>(new TensorShape(1, phonemeTokens.Length), phonemeTokens);
        using var lengthTensor = new Tensor<int>(new TensorShape(1), inputLength);
        using var scalesTensor = new Tensor<float>(new TensorShape(3), scales);

        engine.SetInput("input", phonemesTensor);
        engine.SetInput("input_lengths", lengthTensor);
        engine.SetInput("scales", scalesTensor);
        if (hasSidKey)
        {
            engine.SetInput("sid", new Tensor<int>(new TensorShape(1), new int[] { 0 }));
        }     

        engine.Schedule();

        using var outputTensor = (engine.PeekOutput() as Tensor<float>).ReadbackAndClone();
        float[] audioData = outputTensor.DownloadToArray();

        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError("Failed to generate audio data or the data is empty.");
            return;
        }
        Debug.Log($"Generated audio data length: {audioData.Length}");

        int sampleRate = tokenizer.SampleRate;
        AudioClip clip = AudioClip.Create("GeneratedSpeech", audioData.Length, 1, sampleRate, false);
        clip.SetData(audioData, 0);

        Debug.Log($"Speech generated! AudioClip length: {clip.length:F2}s. Playing.");
        audioSource.PlayOneShot(clip);
    }

    private void _WarmupModel()
    {
        Debug.Log("Warming up the model with a dummy run...");
        string warmupText = "hello";

        string phonemeStr = Phonemize(warmupText);
        if (string.IsNullOrEmpty(phonemeStr))
        {
            Debug.LogError("Warmup failed: Phoneme conversion failed.");
            return;
        }

        string[] phonemeArray = phonemeStr.Trim().Select(c => c.ToString()).ToArray();
        int[] phonemeTokens = tokenizer.Tokenize(phonemeArray);

        float[] scales = tokenizer.GetInferenceParams();
        int[] inputLength = { phonemeTokens.Length };

        using var phonemesTensor = new Tensor<int>(new TensorShape(1, phonemeTokens.Length), phonemeTokens);
        using var lengthTensor = new Tensor<int>(new TensorShape(1), inputLength);
        using var scalesTensor = new Tensor<float>(new TensorShape(3), scales);

        engine.SetInput("input", phonemesTensor);
        engine.SetInput("input_lengths", lengthTensor);
        engine.SetInput("scales", scalesTensor);
        if (hasSidKey)
        {
            engine.SetInput("sid", new Tensor<int>(new TensorShape(1), new int[] { 0 }));
        }


        engine.Schedule();
        
        using var outputTensor = (engine.PeekOutput() as Tensor<float>).ReadbackAndClone();
        
        if (outputTensor.shape[0] > 0)
        {
            Debug.Log($"Model warmup successful. Generated dummy audio data length: {outputTensor.shape[0]}.");
        }
        else
        {
            Debug.LogError("Model warmup failed: Generated output data is empty.");
        }
    }
    
    private string Phonemize(string text)
    {
        Debug.Log($"Phonemizing text: \"{text}\"");
        IntPtr textPtr = IntPtr.Zero;
        try
        {
            Debug.Log($"[PiperManager] Cleaned text for phonemization: \"{text}\"");
            byte[] textBytes = Encoding.UTF8.GetBytes(text + "\0");
            textPtr = Marshal.AllocHGlobal(textBytes.Length);
            Marshal.Copy(textBytes, 0, textPtr, textBytes.Length);
            
            IntPtr pointerToText = textPtr;

            int textMode = 0; // espeakCHARS_AUTO=0
            int phonemeMode = 2; // bit 1: 0=eSpeak's ascii phoneme names, 1= International Phonetic Alphabet (as UTF-8 characters).

            IntPtr resultPtr = ESpeakNG.espeak_TextToPhonemes(ref pointerToText, textMode, phonemeMode);

            if (resultPtr != IntPtr.Zero)
            {
                string phonemeString = PtrToUtf8String(resultPtr);
                Debug.Log($"[PHONEMES] {phonemeString}");
                return phonemeString;
            }
            else
            {
                Debug.LogError("[PiperManager] Phonemize FAILED. The function returned a null pointer.");
                return null;
            }
        }
        finally
        {
            if (textPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(textPtr);
            }
        }
    }
    
    private static string PtrToUtf8String(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return "";
        var byteList = new List<byte>();
        for (int offset = 0; ; offset++)
        {
            byte b = Marshal.ReadByte(ptr, offset);
            if (b == 0) break;
            byteList.Add(b);
        }
        return Encoding.UTF8.GetString(byteList.ToArray());
    }
}