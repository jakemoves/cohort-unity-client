using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CreateBackingForCaptions : MonoBehaviour
{
  //If set to true in the inspector, this script creates a backing for captions by instatiating the
  //captions object again with a style tag created in the TMP style sheets.

	public bool IWantABackingForCaptions;

	public GameObject captions;
	private Transform parentOfCaptions;
  private GameObject backingForCaptions;
  private TMP_Text tmp;
  private TMP_Text thisTmp;

  private void Start()
  {
		if (IWantABackingForCaptions)
		{
			//grab parent of current gameObject
			parentOfCaptions = gameObject.transform.parent;
			//
			backingForCaptions = Instantiate(captions, parentOfCaptions);
			backingForCaptions.transform.SetSiblingIndex(0);

			tmp = captions.GetComponent<TMP_Text>();
			thisTmp = backingForCaptions.GetComponent<TMP_Text>();
		}
  }

  // Update is called once per frame
  void Update()
  {
		if (IWantABackingForCaptions)
		{

			if (tmp.text != null || tmp.text != "")
			{
				thisTmp.text = "<style=textCue>" + tmp.text;
			}
		}
	}


}
