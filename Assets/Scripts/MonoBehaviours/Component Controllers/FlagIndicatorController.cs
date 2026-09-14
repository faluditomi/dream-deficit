using System.Collections;
using UnityEngine;

public class FlagIndicatorController : MonoBehaviour
{
    RectTransform myRectTransform;
    private Coroutine raiseLowerCoroutine;

    public void Setup(FlagType flagType, float xPos)
    {
        myRectTransform = GetComponent<RectTransform>();
        myRectTransform.anchorMin = new Vector2(0f, 0f);
        myRectTransform.anchorMax = new Vector2(0f, 0f);
        myRectTransform.pivot = new Vector2(0f, 0f);
        myRectTransform.anchoredPosition = new Vector2(xPos, -myRectTransform.rect.height);
        GetComponent<UnityEngine.UI.Image>().color = flagType.colour;
        GetComponentInChildren<TMPro.TextMeshProUGUI>().text = flagType.name;
    }

    public void SetRaised(bool raised)
    {
        float targetY = raised ? 0f : -myRectTransform.rect.height;
     
        if(raiseLowerCoroutine != null)
        {
            StopCoroutine(raiseLowerCoroutine);
            raiseLowerCoroutine = null;
        }

        raiseLowerCoroutine = StartCoroutine(MoveFlagIndicatorBehaviour(myRectTransform, targetY));
    }

    private IEnumerator MoveFlagIndicatorBehaviour(RectTransform rectTransform, float targetY)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 targetPosition = new Vector2(startPosition.x, targetY);
        float duration = 0.25f;
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        raiseLowerCoroutine = null;
    }
}
