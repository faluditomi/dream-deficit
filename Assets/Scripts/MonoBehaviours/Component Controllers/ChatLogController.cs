using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogController : BaseWindowController
{
    private ChatLog myChatLog;
    private GameObject chatBubblePrefab;
    private Transform bubbleContainer;
    private GameObject typingIdicator;
    private Coroutine sequenceCoroutine;
    [HideInInspector] public List<ChatBubble> messages;
    public event System.Action<string> OnNewMessageEvent;
    public event System.Action OnDestroyEvent;

    // The chat log window can be initialised without a chat log and a top bar. This is useful for the ChatClientController. 
    public void Setup(ChatLog chatLog, bool needsTopBar = true)
    {
        typingIdicator = transform.Find(Constants.GameObjectNames.TypingIndicator).gameObject;
        bubbleContainer = GetComponentInChildren<ContentSizeFitter>().transform;
        chatBubblePrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.ChatBubble);
        myChatLog = chatLog;
        messages = chatLog.messages;
        PopulateChatLog();
        SetupBaseWindow(chatLog.logName, needsTopBar);
    }

    private void PopulateChatLog()
    {
        foreach(Transform child in bubbleContainer)
        {
            Destroy(child.gameObject);
        }

        foreach(ChatBubble chatBubble in messages)
        {
            ChatBubbleController chatBubbleInstance = Instantiate(chatBubblePrefab, bubbleContainer)
                .GetComponent<ChatBubbleController>();
            chatBubbleInstance.Setup(chatBubble, myChatLog);
        }

        foreach(ChatBubble chatBubble in SaveManager.Instance.GetSequencedChatBubblesForChatLog(myChatLog))
        {
            ChatBubbleController chatBubbleInstance = Instantiate(chatBubblePrefab, bubbleContainer)
                .GetComponent<ChatBubbleController>();
            chatBubbleInstance.Setup(chatBubble, myChatLog);
        }

        List<MarkerData> savedMarkers = SaveManager.Instance.GetSavedMarkersForChatLog(myChatLog);
        if(savedMarkers.Count > 0) MarkerManager.Instance.AddMarkersInstantly(savedMarkers);

        foreach(HighlightHandler highlightHandler in GetComponentsInChildren<HighlightHandler>())
        {
            highlightHandler.Rebuild(Color.clear);
        }
    }

    public void RunBubbleSequence(ChatBubbleSequence chatBubbleSequence, Constants.ChatBubbleSequenceType bubbleSequenceType)
    {
        DayData currentDayData = SaveManager.Instance != null
            ? SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber)
            : null;
        // NOTE: locked logs receive nothing — the sequence is dropped
        if(currentDayData != null && currentDayData.IsLogLocked(myChatLog.logName)) return;

        // NOTE: right now, if a new sequence comes in while another is being processed, the previous gets cut short
        if(sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        sequenceCoroutine = ChatLogManager.Instance.StartCoroutine(RunBubbleSequenceBehaviour(chatBubbleSequence, bubbleSequenceType));
    }

    private IEnumerator RunBubbleSequenceBehaviour(ChatBubbleSequence chatBubbleSequence, Constants.ChatBubbleSequenceType bubbleSequenceType)
    {
        foreach(ChatBubble chatBubble in chatBubbleSequence.messages)
        {
            yield return new WaitForSeconds(chatBubble.delayLength);

            if(isOpen) typingIdicator.SetActive(true);

            yield return new WaitForSeconds(chatBubble.typingFlagLength);

            typingIdicator.SetActive(false);
            ChatBubbleController chatBubbleInstance = Instantiate(chatBubblePrefab, bubbleContainer).GetComponent<ChatBubbleController>();
            chatBubbleInstance.Setup(chatBubble, myChatLog);
            OnNewMessageEvent?.Invoke(chatBubble.message);
        }

        GameManager.Instance.TriggerChatBubbleSequence(bubbleSequenceType);
    }

    private void OnDestroy()
    {
        OnDestroyEvent?.Invoke();
    }
}
