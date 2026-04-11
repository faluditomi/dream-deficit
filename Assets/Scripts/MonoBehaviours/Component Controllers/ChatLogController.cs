using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// TODO: maybe it would be a good idea to have a central cache for the existing ChatLogControllers in the scene,
//       so that other scripts can simply query a chat log by logName (or smth more safe) and send sequences to it
public class ChatLogController : MonoBehaviour
{
    private GameObject chatBubblePrefab;
    private Transform bubbleContainer;
    private GameObject typingIdicator;
    private PointerHandler closePointerHandler;
    private Coroutine sequenceCoroutine;

    public string logName;
    public List<ChatBubble> messages;
    public bool isOpen = false;

    private bool isSetUp = false;

    // NOTE: marker data could be collected here at runtime

    #region Setup
    // TODO: have a safety check -> return and self-destruct if there is already an instance of ChatLog open
    public async void Setup(ChatLog chatLog)
    {
        if(isSetUp || !FindElements()) return;
        
        PopulateChatLogProperties(chatLog);

        bubbleContainer = GetComponentInChildren<ContentSizeFitter>().transform;
        chatBubblePrefab = await AddressableManager.Instance().RetrieveAddressable<GameObject>(Constants.AddressablePaths.ChatBubble);

        foreach(ChatBubble chatBubble in messages)
        {
            ChatBubbleController chatBubbleInstance = Instantiate(chatBubblePrefab, bubbleContainer).GetComponent<ChatBubbleController>();
            chatBubbleInstance.Setup(chatBubble);
        }

        closePointerHandler.OnPointerClickEvent += Close;
        
        isSetUp = true;

        // TODO: remove this
        RunBubbleSequence(await AddressableManager.Instance().RetrieveAddressable<ChatBubbleSequence>(Constants.AddressablePaths.ChatBubbleSequence + Constants.ChatBubbleSequenceCodes.CrypticSequence));
    }

    private bool FindElements()
    {
        typingIdicator = transform.Find(Constants.GameObjectNames.TypingIndicator).gameObject;
        Transform topBar = transform.Find(Constants.GameObjectNames.TopBar);
        topBar.AddComponent<DragHandler>().objectToDrag = transform;
        closePointerHandler = topBar.Find(Constants.GameObjectNames.CloseButton).AddComponent<PointerHandler>();

        return true;
    }

    private void PopulateChatLogProperties(ChatLog chatLog)
    {
        logName = chatLog.logName;
        messages = chatLog.messages;
    }
    #endregion

    public void Open()
    {
        isOpen = true;
        gameObject.SetActive(true);
    }

    public void Close(PointerEventData eventData)
    {
        isOpen = false;
        gameObject.SetActive(false);
    }

    public void RunBubbleSequence(ChatBubbleSequence chatBubbleSequence)
    {
        // NOTE: right now, if a new sequence comes in while another is being processed, the previous gets cut short
        if(sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        sequenceCoroutine = StartCoroutine(RunBubbleSequenceBehaviour(chatBubbleSequence));
    }

    private void OnDestroy()
    {
        // NOTE: could call marker aggregator from here
        if(!closePointerHandler)
        {
            closePointerHandler.OnPointerUpEvent -= Close;
        }
    }

    private IEnumerator RunBubbleSequenceBehaviour(ChatBubbleSequence chatBubbleSequence)
    {
        foreach(ChatBubble chatBubble in chatBubbleSequence.messages)
        {
            yield return new WaitForSeconds(chatBubble.delayLength);

            typingIdicator.SetActive(true);

            yield return new WaitForSeconds(chatBubble.typingFlagLength);

            typingIdicator.SetActive(false);
            ChatBubbleController chatBubbleInstance = Instantiate(chatBubblePrefab, bubbleContainer).GetComponent<ChatBubbleController>();
            chatBubbleInstance.Setup(chatBubble);
        }
    }
}
