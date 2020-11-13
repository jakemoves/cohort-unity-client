using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleActiveState : MonoBehaviour
{
  private bool state;

  public void Toggle()
  {
    state = !state;
    gameObject.SetActive(state);
  }
}
