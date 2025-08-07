using UnityEngine;

[RequireComponent(typeof(LineRenderer))] 
public class FrequencyBandAnalyser : MonoBehaviour
{
    public enum Bands
    {
        Eight = 8,
        SixtyFour = 64,
    }

    AudioSource _AudioSource;
    LineRenderer _lineRenderer;

    int _FrequencyBins = 512;

    float[] _Samples;
    float[] _SampleBuffer;

    [Header("Visualization Settings")]
    public bool _enableVisualization = true;
    public float _Scalar = 100;
    public float _visualizerWidth = 10f;
    public Color _lineColor = Color.white;
    public float _lineWidth = 0.05f;

    [Header("Audio Processing")]
    public float _SmoothDownRate = 10;

    float[] _FreqBands8;
    [HideInInspector]
    float[] _FreqBands64;

    void Start()
    {
        _AudioSource = GetComponent<AudioSource>();
        _lineRenderer = GetComponent<LineRenderer>();

        _FreqBands8 = new float[8];
        _FreqBands64 = new float[64];
        _Samples = new float[_FrequencyBins];
        _SampleBuffer = new float[_FrequencyBins];

        SetupLineRenderer();
    }

    void SetupLineRenderer()
    {
        _lineRenderer.positionCount = _FreqBands64.Length;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.startColor = _lineColor;
        _lineRenderer.endColor = _lineColor;
        _lineRenderer.useWorldSpace = false;
    }

    void Update()
    {
        _AudioSource.GetSpectrumData(_SampleBuffer, 0, FFTWindow.BlackmanHarris);

        for (int i = 0; i < _Samples.Length; i++)
        {
            if (_SampleBuffer[i] > _Samples[i])
                _Samples[i] = _SampleBuffer[i];
            else
                _Samples[i] = Mathf.Lerp(_Samples[i], _SampleBuffer[i], Time.deltaTime * _SmoothDownRate);
        }

        UpdateFreqBands8();
        UpdateFreqBands64();

        if (_enableVisualization)
        {
            _lineRenderer.enabled = true;
            DrawVisualizer();
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }

    void DrawVisualizer()
    {
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.startColor = _lineColor;
        _lineRenderer.endColor = _lineColor;

        Vector3[] positions = new Vector3[_FreqBands64.Length];
        for (int i = 0; i < _FreqBands64.Length; i++)
        {
            float xPos = (float)i / (_FreqBands64.Length - 1) * _visualizerWidth;
            float yPos = _FreqBands64[i] * _Scalar;
            positions[i] = new Vector3(xPos, yPos, 0);
        }
        _lineRenderer.SetPositions(positions);
    }
    void UpdateFreqBands8()
    {
        int count = 0;
        for (int i = 0; i < 8; i++)
        {
            float average = 0;
            int sampleCount = (int)Mathf.Pow(2, i) * 2;

            if(i == 7)
            {
                sampleCount += 2;
            }

            for (int j = 0; j < sampleCount; j++)
            {
                average += _Samples[count] * (count + 1);
                count++;
            }

            average /= count;
            _FreqBands8[i] = average;
        }
    }

    void UpdateFreqBands64()
    {
        int count = 0;
        int sampleCount = 1;
        int power = 0;

        for (int i = 0; i < 64; i++)
        {
            float average = 0;

            if (i == 16 || i == 32 || i == 40 || i == 48 || i == 56)
            {
                power++;
                sampleCount = (int)Mathf.Pow(2, power);
                if (power == 3)
                    sampleCount -= 2;
            }

            for (int j = 0; j < sampleCount; j++)
            {
                average += _Samples[count] * (count + 1);
                count++;
            }

            average /= count;
            _FreqBands64[i] = average;
        }
    }

    public float GetBandValue(int index, Bands bands)
    {
        if(bands == Bands.Eight)
        {
            return _FreqBands8[index];
        }
        else
        {
            return _FreqBands64[index];
        }
    }
}