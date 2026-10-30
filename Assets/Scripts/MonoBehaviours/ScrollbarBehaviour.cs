using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollbarBehaviour : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    private ScrollRect scrollRect;
    private ChatLogController chatLogController;
    private Coroutine newContentCoroutine;
    private Vector2 lastPosition;
    private bool isDragging;
    private bool hasMoved;
    private bool isAtBottom = true;
    private int stillFrames;
    private float positionEpsilon = 0.001f;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>() ?? GetComponentInChildren<ScrollRect>(true);

        if(TryGetComponent<ChatLogController>(out chatLogController))
        {
            chatLogController.OnNewMessageEvent += _ => OnNewContentBehaviour();
            chatLogController.OnNewChoiceEvent += OnNewContentBehaviour;
        }

        if(scrollRect == null || scrollRect.content == null || chatLogController == null)
        {
            Destroy(this);
            return;
        }

        lastPosition = scrollRect.content.anchoredPosition;
    }

    private void LateUpdate()
    {
        if(scrollRect == null || scrollRect.content == null || chatLogController == null) return;

        Vector2 currentPosition = scrollRect.content.anchoredPosition;
        bool movedThisFrame = (currentPosition - lastPosition).sqrMagnitude > positionEpsilon * positionEpsilon;
        lastPosition = currentPosition;
        
        if(movedThisFrame)
        {
            hasMoved = true;
            stillFrames = 0;
            return;
        }

        if(!isDragging && hasMoved && ++stillFrames >= 2)
        {
            hasMoved = false;
            stillFrames = 0;
            OnScrollStopped();
        }
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
    
    private void OnScrollStopped()
    {
        isAtBottom = scrollRect.verticalNormalizedPosition <= 0.1f;
    }

    private void OnNewContentBehaviour()
    {
        Debug.Log(isAtBottom);
        if(isAtBottom && newContentCoroutine == null) newContentCoroutine = StartCoroutine(ScrollToBottomBehaviour());
    }

    private IEnumerator ScrollToBottomBehaviour()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        newContentCoroutine = null;
    }
}
