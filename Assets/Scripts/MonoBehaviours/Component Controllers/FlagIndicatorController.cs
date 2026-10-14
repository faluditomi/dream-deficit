using System.Collections;
using UnityEngine;

public class FlagIndicatorController : MonoBehaviour
{
    RectTransform myRectTransform;
    private Coroutine raiseLowerCoroutine;

    public void Setup(FlagType flagType, float xPos)
    {
        myRectTransform = GetComponent<RectTransform>();
        myRectTransform.anchoredPosition = new Vector2(xPos - myRectTransform.rect.width / 2f, myRectTransform.rect.height);
        GetComponent<UnityEngine.UI.Image>().color = flagType.colour;
        GetComponentInChildren<TMPro.TextMeshProUGUI>().text = flagType.name;
    }

    public void SetLowered(bool lowered)
    {
        // The owning GameObject can be destroyed when its scene is unloaded (e.g. on a day/scene
        // transition) while the persistent FlagManager still holds a reference to this controller.
        // Unity's overloaded == treats destroyed objects as null, so bail out before touching them.
        if(this == null || gameObject == null) return;

        if(myRectTransform == null)
        {
            myRectTransform = GetComponent<RectTransform>();
            if(myRectTransform == null) return;
        }

        float targetY = lowered ? myRectTransform.rect.height : 0f;
     
        if(raiseLowerCoroutine != null)
        {
            StopCoroutine(raiseLowerCoroutine);
            raiseLowerCoroutine = null;
        }

        raiseLowerCoroutine = StartCoroutine(MoveFlagIndicatorBehaviour(myRectTransform, targetY));
    }

    private IEnumerator MoveFlagIndicatorBehaviour(RectTransform rectTransform, float targetY)
    {
        if(rectTransform == null) yield break;
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 targetPosition = new Vector2(startPosition.x, targetY);
        float duration = 0.25f;
        float elapsed = 0f;

        while(elapsed < duration)
        {
            // The transform can be destroyed mid-animation if its scene is unloaded.
            if(rectTransform == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        if(rectTransform == null) yield break;
        rectTransform.anchoredPosition = targetPosition;
        raiseLowerCoroutine = null;
    }
}
