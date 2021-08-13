using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.Linq;

//using UnityEngine.Events;
//[Serializable]
//public class MyEvent : UnityEvent<float, float, float> {}

public class SampleAudioManipulation : MonoBehaviour
{
    //public MyEvent OnEvent;

    // Start is called before the first frame update
    public AudioSource source;
    public bool enableSmoothing = false;
    public int smoothingSamples = 15;
    public Dropdown effectSel;
    public Dropdown sensorSel;

    public Text strOutput;
    public Text samplesText;

    Smoother smoother;
    //public SensorManager sensorManager;

    Dictionary<string, Func<float>> sensorFuncs = new Dictionary<string, Func<float>>
    {
        {"Rotation", () => Input.gyro.rotationRateUnbiased.magnitude},
        {"Acceleration", () => Input.gyro.userAcceleration.magnitude},
        {"Gravity (Face Up / Face Down)", () => Input.gyro.gravity.z},
        //{"Gravity Y", () => Input.gyro.gravity.y},
        //{"Gravity X", () => Input.gyro.gravity.x}
    };

    Dictionary<string, Action<float>> effectActions;

    void Awake() 
    {
        if (effectSel == null || sensorSel == null || source == null) 
        {
            this.enabled = false;
            Debug.LogError("ERROR: Missing Attatchments");
            return;
        }

        effectActions = new Dictionary<string, Action<float>>
        {
            {"Volume", n => {
                //TODO
                Mathf.Clamp(n, 0, 1);
                source.volume = (n/2) + 0.5f; 
            }},
            {"Pitch", n => {
                if (n > 0) source.pitch = n + 1f;
                else if (n < 0) source.pitch = 1f/(-n + 1f);
                else source.pitch = 1;
            }},
            {"PitchUp", n => {
                
                source.pitch = Mathf.Abs(n) + 1;
            }},
            {"PitchDown", n => {
                source.pitch = 1/(Mathf.Abs(n) + 1);
            }}
        };
    }
    
    void Start()
    {
        smoother = new Smoother(smoothingSamples);

        sensorSel.ClearOptions();
        effectSel.ClearOptions();

        sensorSel.AddOptions(new List<string>(sensorFuncs.Keys));
        effectSel.AddOptions(new List<string>(effectActions.Keys));
    }

    // Update is called once per frame
    void Update()
    {
        // Manipulate Audio
        //Debug.Log(sensorSel.captionText.text);
        var value = sensorFuncs[sensorSel.captionText.text]();
        if (enableSmoothing) value = smoother.NewSample(value);
        effectActions[effectSel.captionText.text](value);
        //Debug.Log(effectSel.captionText.text);
        if (strOutput != null) strOutput.text = value.ToString("N4");
        if (samplesText != null) samplesText.text = $"Samples: {smoothingSamples}";
    }

    public void ToggleSmoothing() => enableSmoothing = !enableSmoothing;
    public void ChangeNSamples(int samples) 
    {
        smoother = new Smoother(samples, smoother);
        smoothingSamples = samples;
    }

    public void ChangeNSamples(float samples) => ChangeNSamples((int) samples);
}

public class Smoother : IEnumerable<float>
{
    private Queue<float> buffer;
    int nsamples;
    float total = 0;
    public Smoother(int samples) {
        buffer = new Queue<float>(Enumerable.Repeat<float>(0f, samples));
        nsamples = samples;
    }

    public Smoother(int samples, IEnumerable<float> source)
    {
        int sourceCount = source.Count();
        if (sourceCount == samples)
            buffer = new Queue<float>(source);
        else if (sourceCount > samples)
            buffer = new Queue<float>(source.Take(samples));
        else
            buffer = new Queue<float>(source.Concat(Enumerable.Repeat(0f, samples - sourceCount)));

        nsamples = samples;
        total = buffer.Sum();
    } 

    public IEnumerator<float> GetEnumerator()
    {
        return buffer.GetEnumerator();
    }

    public float NewSample(float value)
    {
        buffer.Enqueue(value);
        total += value - buffer.Dequeue();
        return total/nsamples;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return buffer.GetEnumerator();
    }

    //public static Smoother FromSource(int samples, IEnumerable<float> source) 

    public static implicit operator float(Smoother s) => s.total / s.nsamples;
}