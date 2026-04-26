using UnityEngine;

public class SetRectTransformToRef : MonoBehaviour
{
    [SerializeField] private RectTransform refRectTransform;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        //stick this rect to ref rect
        rectTransform.anchoredPosition = refRectTransform.anchoredPosition;
        rectTransform.anchorMax = refRectTransform.anchorMax;
        rectTransform.anchorMin = refRectTransform.anchorMin;
    }
}
