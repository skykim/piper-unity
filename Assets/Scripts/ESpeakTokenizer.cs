using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

public class AudioConfig
{
    public int sample_rate { get; set; }
    public string quality { get; set; }
}

public class ESpeakConfig
{
    public string voice { get; set; }
}

public class InferenceConfig
{
    public float noise_scale { get; set; }
    public float length_scale { get; set; }
    public float noise_w { get; set; }
}

public class PiperConfig
{
    public AudioConfig audio { get; set; }
    public ESpeakConfig espeak { get; set; }
    public InferenceConfig inference { get; set; }
    public string phoneme_type { get; set; }

    [JsonProperty("phoneme_id_map")]
    public Dictionary<string, int[]> PhonemeIdMap { get; set; }
}

public class ESpeakTokenizer : MonoBehaviour
{
    public TextAsset jsonFile;

    public int SampleRate { get; private set; }
    public string Quality { get; private set; }
    public string Voice { get; private set; }
    public string PhonemeType { get; private set; }

    private PiperConfig config;
    private float[] inferenceParams;
    private bool isInitialized = false;

    void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (jsonFile == null)
        {
            Debug.LogError("JSON file is not assigned. Please assign it in the Inspector.");
            return;
        }

        try
        {
            config = JsonConvert.DeserializeObject<PiperConfig>(jsonFile.text);
            if (config == null || config.audio == null || config.espeak == null || config.inference == null || config.PhonemeIdMap == null)
            {
                Debug.LogError("JSON data is missing required fields or is invalid. Deserialization resulted in a partially/fully null object.");
                config = null;
                return;
            }
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Failed to parse JSON file. Error: {ex.Message}");
            return;
        }

        inferenceParams = new float[3]
        {
            config.inference.noise_scale,
            config.inference.length_scale,
            config.inference.noise_w
        };

        this.SampleRate = config.audio.sample_rate;
        this.Quality = config.audio.quality;
        this.Voice = config.espeak.voice;
        this.PhonemeType = config.phoneme_type;
        
        isInitialized = true;
        Debug.Log("JSON parsing and setup completed successfully.");
        Debug.Log($"Extracted Settings: SampleRate={SampleRate}, Quality='{Quality}', Voice='{Voice}', PhonemeType='{PhonemeType}'");
    }

    public int[] Tokenize(string[] phonemes)
    {
        if (!isInitialized)
        {
            Debug.LogError("Tokenizer is not initialized. Check for errors during Awake().");
            return null;
        }

        int estimatedCapacity = (phonemes != null ? phonemes.Length * 2 : 0) + 3;
        var tokenizedList = new List<int>(estimatedCapacity) { 1, 0 };

        if (phonemes != null && phonemes.Length > 0)
        {
            foreach (string phoneme in phonemes)
            {
                if (config.PhonemeIdMap.TryGetValue(phoneme, out int[] ids) && ids.Length > 0)
                {
                    tokenizedList.Add(ids[0]);
                    tokenizedList.Add(0);
                }
                else
                {
                    Debug.LogWarning($"Token not found for phoneme: '{phoneme}'. It will be skipped.");
                }
            }
        }

        tokenizedList.Add(2);

        return tokenizedList.ToArray();
    }

    public float[] GetInferenceParams()
    {
        if (!isInitialized)
        {
            Debug.LogError("Tokenizer is not initialized. Cannot get inference parameters.");
            return null;
        }
        return (float[])inferenceParams.Clone();
    }
}