using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cohort;
using UnityEngine.UI;

public class ImageCueScroller : MonoBehaviour
{
  public CHSession chSession;
  public ScrollRect scrollRect;
  public AspectRatioFitter aspectRatioFitter;
  public Image imageComponent;
  public float aspectRatioThreshold = 0.35f;

  private RectTransform imageRect;
  private RectTransform veiwportRect;

  //private RectTransform selfRect;
  public float screenAspectRatio;

  // Start is called before the first frame update
  void Start()
  {
    // Null Checks
    if (chSession == null)
    {
      Debug.LogError("The CH Session property has not been set and therefore" +
        " will be unable to receive OnImageCue events", this);
    }
    else
      chSession.onImageCue += ChSession_onImageCue;

    if (aspectRatioFitter == null)
    {
      Debug.LogError("Aspect Ratio Fitter has not been set", this);
    }
    else
    {
      aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
      aspectRatioFitter.aspectRatio = 1;
    }

    if (imageComponent == null)
    {
      // There isn't a reason to be enabled anymore
      Debug.LogError("Image Component has not been set, this component will be disabled", this);
      this.enabled = false;
      return;
    }

    if (scrollRect == null)
      Debug.LogWarning("Scroll Rect has not been set", this);

    // this assumes this component is on the veiwport GameOject
    veiwportRect = scrollRect?.viewport ?? FindObjectOfType<RectTransform>();//this.gameObject.GetComponent<RectTransform>();
    imageRect = imageComponent.gameObject.GetComponent<RectTransform>();

    // Get current Aspect Ratio and our RectTransform
    //selfRect = this.gameObject.gameObject.GetComponent<RectTransform>();
    screenAspectRatio = (float)Screen.width / Screen.height;//selfRect.rect.width / selfRect.rect.height;
    
  }

  private void ChSession_onImageCue(CueAction cueAction, Sprite sprite)
  {
    // Check if the image cue action is a GO
    if (!(cueAction == CueAction.play) && !(cueAction == CueAction.restart))
    {
      return;
    }

    // Calculate Sprite Aspect
    float aspectRatio = (float)sprite.texture.width / sprite.texture.height;


    // Check the aspect ratio
    if (aspectRatio > aspectRatioThreshold)
    {
      imageComponent.transform.localRotation = Quaternion.Euler(0, 0, 0);
      //aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
      //aspectRatioFitter.aspectRatio = aspectRatio;

      //imageRect.anchorMin = Vector2.zero;
      //imageRect.anchorMax = Vector2.one;
      imageRect.pivot = new Vector2(0.5f, 0.5f);
      imageRect.anchorMin = Vector2.zero;
      imageRect.anchorMax = Vector2.one;
      imageRect.sizeDelta = new Vector2(0, 0);
    }
    else
    {
      SetupForScrollingImage(aspectRatio, (screenAspectRatio = (float)Screen.width / Screen.height) > 1);
    }

  }

  private void SetupForScrollingImage(float aspectRatio, bool sideScroll)
  {
    if (sideScroll)
    {
      imageComponent.transform.localRotation = Quaternion.Euler(0, 0, -90);
      //aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
      //aspectRatioFitter.aspectRatio = (float)sprite.texture.height / sprite.texture.width;
      imageRect.anchorMin = new Vector2(0, 0.5f);//Vector2.up;
      imageRect.anchorMax = new Vector2(1, 0.5f);//Vector2.one;
      imageRect.sizeDelta = new Vector2((veiwportRect.sizeDelta.y - veiwportRect.sizeDelta.x), veiwportRect.rect.height / aspectRatio);
      imageRect.pivot = new Vector2(0, 0);
      imageRect.localPosition = new Vector2(-imageRect.rect.height, 0);
    }
    else
    {
      imageComponent.transform.localRotation = Quaternion.Euler(0, 0, 0);
      //aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
      //aspectRatioFitter.aspectRatio = (float)sprite.texture.height / sprite.texture.width;
      imageRect.anchorMin = new Vector2(0.5f, 0);//Vector2.up;
      imageRect.anchorMax = new Vector2(0.5f, 1);//Vector2.one;
      imageRect.sizeDelta = new Vector2(veiwportRect.rect.width, (veiwportRect.rect.width / aspectRatio) - veiwportRect.rect.height);
      imageRect.pivot = new Vector2(0, 0);
      imageRect.localPosition = new Vector2(0, -imageRect.rect.height);
    }
  }

  // Update is called once per frame
  void Update()
  {
    float currentScreenAspectRatio = (float)Screen.width / Screen.height;

    // Check if the screen size has changed
    if (currentScreenAspectRatio != screenAspectRatio)
    {
      screenAspectRatio = currentScreenAspectRatio;

      float aspectRatio;
      if (imageComponent.sprite != null && 
        aspectRatioThreshold >= (aspectRatio = (float)imageComponent.sprite.texture.width / imageComponent.sprite.texture.height))
      {
        // We are in an image cue
        SetupForScrollingImage(aspectRatio, screenAspectRatio > 1);
      }
    }
  }
}
