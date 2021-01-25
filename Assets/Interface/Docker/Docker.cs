using UnityEngine;
using UnityEngine.UI;

public class Docker : MonoBehaviour
{
    public RectTransform content;
    public Sprite collapse;
    public Sprite expand;
    
    public Button button;
    public Image dock;
    
    [SerializeField] public bool expanded;
    
    private void Start()
    {
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        if (expanded)
        {
            transform.localPosition -= new Vector3(content.sizeDelta.x, 0, 0);
            dock.sprite = expand;
        }
        else
        {
            transform.localPosition += new Vector3(content.sizeDelta.x, 0, 0);
            dock.sprite = collapse;
        }

        expanded = !expanded;
    }
}
