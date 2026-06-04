using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// TODO: maybe it would be a good idea to have a central cache for the existing ChatLogControllers in the scene,
//       so that other scripts can simply query a chat log by logName (or smth more safe) and send sequences to it
public class ChatLogController : BaseWindowController
{
    private ChatLog myChatLog;
    private GameObject chatBubblePrefab;
    private Transform bubbleContainer;
    private GameObject typingIdicator;
    private Coroutine sequenceCoroutine;

    public List<ChatBubble> messages;

    public void Setup(ChatLog chatLog)
    {
        // TODO: have a safety check -> return and self-destruct if there is already an instance of ChatLog open
        typingIdicator = transform.Find(Constants.GameObjectNames.TypingIndicator).gameObject;
        myChatLog = chatLog;
        messages = chatLog.messages;
        bubbleContainer = GetComponentInChildren<ContentSizeFitter>().transform;
        chatBubblePrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(
            Constants.AddressablePaths.ChatBubblePrefab);
        SetupTopBar();

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

    public void RunBubbleSequence(ChatBubbleSequence chatBubbleSequence, ChatBubbleSequenceType bubbleSequenceType)
    {
        // NOTE: right now, if a new sequence comes in while another is being processed, the previous gets cut short
        if(sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        sequenceCoroutine = StartCoroutine(RunBubbleSequenceBehaviour(chatBubbleSequence, bubbleSequenceType));
    }

    private IEnumerator RunBubbleSequenceBehaviour(ChatBubbleSequence chatBubbleSequence, ChatBubbleSequenceType bubbleSequenceType)
    {
        foreach(ChatBubble chatBubble in chatBubbleSequence.messages)
        {
            yield return new WaitForSeconds(chatBubble.delayLength);

            if(isOpen) typingIdicator.SetActive(true);

            yield return new WaitForSeconds(chatBubble.typingFlagLength);

            typingIdicator.SetActive(false);
            ChatBubbleController chatBubbleInstance = Instantiate(chatBubblePrefab, bubbleContainer)
                .GetComponent<ChatBubbleController>();
            chatBubbleInstance.Setup(chatBubble, myChatLog);
        }

        GameManager.Instance.TriggerChatBubbleSequence(bubbleSequenceType);
    }
}
