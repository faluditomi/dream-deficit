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

    private bool isSetUp = false;

    public async void Setup(ChatLog chatLog)
    {
        // TODO: have a safety check -> return and self-destruct if there is already an instance of ChatLog open
        if(isSetUp || !FindElements()) return;
        myChatLog = chatLog;
        messages = chatLog.messages;
        bubbleContainer = GetComponentInChildren<ContentSizeFitter>().transform;
        chatBubblePrefab = await AddressableManager.Instance.RetrieveAddressable<GameObject>(
            Constants.AddressablePaths.ChatBubblePrefab);
        SetupTopBar();

        foreach(ChatBubble chatBubble in messages)
        {
            ChatBubbleController chatBubbleInstance = Instantiate(chatBubblePrefab, bubbleContainer)
                .GetComponent<ChatBubbleController>();
            chatBubbleInstance.Setup(chatBubble, myChatLog);
        }
        
        isSetUp = true;
        // TODO: remove this
        RunBubbleSequence(await AddressableManager.Instance.RetrieveAddressable<ChatBubbleSequence>(
                Constants.AddressablePaths.ChatBubbleSequence
                + Constants.ChatBubbleSequenceCodes.CrypticSequence));
    }

    private bool FindElements()
    {
        typingIdicator = transform.Find(Constants.GameObjectNames.TypingIndicator).gameObject;
        return true;
    }

    private void OnDestroy()
    {
        // NOTE: could call marker aggregator from here
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

    private IEnumerator RunBubbleSequenceBehaviour(ChatBubbleSequence chatBubbleSequence)
    {
        foreach(ChatBubble chatBubble in chatBubbleSequence.messages)
        {
            yield return new WaitForSeconds(chatBubble.delayLength);

            typingIdicator.SetActive(true);

            yield return new WaitForSeconds(chatBubble.typingFlagLength);

            typingIdicator.SetActive(false);
            ChatBubbleController chatBubbleInstance = Instantiate(chatBubblePrefab, bubbleContainer)
                .GetComponent<ChatBubbleController>();
            chatBubbleInstance.Setup(chatBubble, myChatLog);
        }
    }
}
