using UnityEngine;
public class NeverDim : MonoBehaviour
{
	void Start()
	{
		// Disable screen dimming
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
	}
}