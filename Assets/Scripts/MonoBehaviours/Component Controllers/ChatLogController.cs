using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogController : BaseWindowController
{
    private ChatLog myChatLog;
    private GameObject chatBubblePrefab;
    private GameObject UserChatResponseOptionPrefab;
    private Transform bubbleContainer;
    private GameObject typingIdicator;
    private ConversationRunner runner;
    private readonly List<UserChatResponseOptionController> draftInstances = new List<UserChatResponseOptionController>();
    public event System.Action<string> OnNewMessageEvent;
    public event System.Action OnDestroyEvent;
    public int unreadMessages = 0;

    // The chat log window can be initialised without a chat log and a top bar. This is useful for the ChatClientController. 
    public void Setup(ChatLog chatLog, bool needsTopBar = true)
    {
        typingIdicator = transform.Find(Constants.GameObjectNames.TypingIndicator).gameObject;
        bubbleContainer = GetComponentInChildren<ContentSizeFitter>().transform;
        chatBubblePrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.ChatBubble);
        UserChatResponseOptionPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.UserChatResponseOption);
        myChatLog = chatLog;
        // NOTE: the runner owns playback and history — this window is a view over seed + history + pending choice
        runner = ConversationManager.Instance.GetRunnerForLog(chatLog);
        runner.OnBubbleStarted += OnRunnerBubbleStarted;
        runner.OnBubblePlayed += OnRunnerBubblePlayed;
        runner.OnChoicePresented += OnRunnerChoicePresented;
        runner.OnChoiceResolved += OnRunnerChoiceResolved;
        PopulateChatLog();
        SetupBaseWindow(chatLog.logName, needsTopBar);
        OnGainedFocusEvent += (focusedWindow, unreadMessages) => this.unreadMessages = 0;
    }

    private void PopulateChatLog()
    {
        ClearDrafts();
        foreach(Transform child in bubbleContainer) Destroy(child.gameObject);

        // seed bubbles: every Bubble node of every seed graph, in list order, static render (no timing, no history)
        if(myChatLog.Graphs != null)
        {
            foreach(ConversationGraph graph in myChatLog.Graphs)
            {
                if(graph == null || !graph.isSeed || graph.nodes == null) continue;

                foreach(ConversationNodeData node in graph.nodes)
                {
                    if(node == null || node.kind != ConversationNodeKind.Bubble || node.bubble == null) continue;
                    InstantiateBubble(node.bubble, node.guid);
                }
            }
        }

        // history: played bubbles resolved through the graph asset
        if(runner != null && runner.history != null)
        {
            foreach(PlayedBubbleRecord record in runner.history)
            {
                ChatBubble bubble = myChatLog.ResolvePlayedBubble(record);
                if(bubble == null)
                {
                    Debug.LogWarning($"ChatLogController: could not resolve played bubble '{record.graphName}/{record.nodeGuid}' in log '{myChatLog.logName}'. Skipping.");
                    continue;
                }
                InstantiateBubble(bubble, record.nodeGuid);
            }
        }

        List<MarkerData> savedMarkers = SaveManager.Instance.GetSavedMarkersForChatLog(myChatLog);
        if(savedMarkers.Count > 0) MarkerManager.Instance.AddMarkersInstantly(savedMarkers);

        foreach(HighlightHandler highlightHandler in GetComponentsInChildren<HighlightHandler>())
        {
            highlightHandler.Rebuild(Color.clear);
        }

        // NOTE: an unanswered choice stays pending in the runner and re-presents its drafts on display (spec: re-present on next display)
        if(runner != null && runner.HasPendingChoice) OnRunnerChoicePresented();
    }

    private void InstantiateBubble(ChatBubble chatBubble, string nodeGuid)
    {
        ChatBubbleController chatBubbleInstance = Instantiate(chatBubblePrefab, bubbleContainer).GetComponent<ChatBubbleController>();
        chatBubbleInstance.Setup(chatBubble, myChatLog, nodeGuid);
    }

    private void OnRunnerBubbleStarted(PlayedBubbleRecord record)
    {
        if(record == null) return;
        if(isOpen) typingIdicator.SetActive(true);
    }

    private void OnRunnerBubblePlayed(PlayedBubbleRecord record)
    {
        if(record == null) return;

        typingIdicator.SetActive(false);
        ChatBubble bubble = myChatLog.ResolvePlayedBubble(record);
        if(bubble == null) return;

        InstantiateBubble(bubble, record.nodeGuid);
        OnNewMessageEvent?.Invoke(bubble.message);

        if(!ChatLogManager.Instance.IsChatLogInFocus(this)) unreadMessages++;
    }

    private void OnRunnerChoicePresented()
    {
        if(UserChatResponseOptionPrefab == null) return;
        if(runner == null || runner.PendingChoiceNode == null || runner.PendingChoiceNode.options == null) return;

        ClearDrafts();

        foreach(ConversationChoiceOptionData option in runner.PendingChoiceNode.options)
        {
            if(option == null || string.IsNullOrEmpty(option.guid)) continue;
            UserChatResponseOptionController draftInstance = Instantiate(UserChatResponseOptionPrefab, bubbleContainer).GetComponent<UserChatResponseOptionController>();
            draftInstance.Setup(option.previewText, () => runner.ResolveChoice(option.guid));
            draftInstances.Add(draftInstance);
        }
    }

    private void OnRunnerChoiceResolved()
    {
        ClearDrafts();
    }

    private void ClearDrafts()
    {
        foreach(UserChatResponseOptionController draftInstance in draftInstances)
        {
            if(draftInstance != null) Destroy(draftInstance.gameObject);
        }

        draftInstances.Clear();
    }

    private void OnDestroy()
    {
        if(runner != null)
        {
            runner.OnBubbleStarted -= OnRunnerBubbleStarted;
            runner.OnBubblePlayed -= OnRunnerBubblePlayed;
            runner.OnChoicePresented -= OnRunnerChoicePresented;
            runner.OnChoiceResolved -= OnRunnerChoiceResolved;
        }

        OnDestroyEvent?.Invoke();
    }
}