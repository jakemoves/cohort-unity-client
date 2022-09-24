using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleBackgroundByGroup : MonoBehaviour
{
    public Dropdown GroupDropdown;
    public Image Background;

    [field: Header("Groups")]
    [field: SerializeField] public Sprite Qas { get; set; }
    [field: SerializeField] public Sprite Gemma { get; set; }
    [field: SerializeField] public Sprite Roslyn { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        if (GroupDropdown != null)
            GroupDropdown.onValueChanged
                .AddListener((i) => SetGroup(GroupDropdown.options[i].text));

        SetGroup(GroupDropdown.options[GroupDropdown.value].text);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetGroup(string group)
    {
        Background.color = Color.HSVToRGB(0, 0, 1);

        Sprite sprite;
        switch (group)
        {
            case nameof(Qas):
                sprite = Qas;
                break;
            case nameof(Gemma):
                sprite = Gemma;
                break;
            case nameof(Roslyn):
                sprite = Roslyn;
                break;
            default:
                Background.color = Color.HSVToRGB(0, 0, 0.27f);
                sprite = null;
                break;
        }

        if (Background != null)
            Background.sprite = sprite;
    }
}
