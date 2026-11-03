using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableAction : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform originalParent;
    [HideInInspector] public CanvasGroup canvasGroup;

    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }



    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        if (transform.parent == canvas.transform)
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}