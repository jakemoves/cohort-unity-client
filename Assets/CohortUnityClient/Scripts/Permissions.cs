using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Permissions : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
		if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
		{
			Application.RequestUserAuthorization(UserAuthorization.WebCam);
		}
	}

  
}
