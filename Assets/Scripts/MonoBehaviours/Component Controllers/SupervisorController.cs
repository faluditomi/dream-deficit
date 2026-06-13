using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SupervisorController : BaseChatLogInitialiser, ISavable, ILoadable
{
    private GameObject daySignalButtonPrefab;
    private Transform chatBubbleHolder;
    private ChatLog supervisorChatLog;
    // TODO: instead of making this public, the GameManager should be able to call the chat log controller directly
    //       by this class being derived from ChatLogController or smth

    //TODO: these are sequences like 'supervisor_day_1_start_sequence'
    //      they are named based on a structure/framework such that they can be called from code
    //      (but also, they shouldn't be stored here, since they are already in part of the save system)
    private List<ChatBubbleSequence> dailySequences = new List<ChatBubbleSequence>();

    private void Awake()
    {
        supervisorChatLog = AddressableManager.Instance
            .RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + Constants.ChatLogs.Supervisor);
        base.Setup(supervisorChatLog);
        daySignalButtonPrefab = AddressableManager.Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.DaySignalButton);
        chatBubbleHolder = myChatLogController.transform.Find(Constants.GameObjectNames.Viewport).Find(Constants.GameObjectNames.Content);
    }

    public void DayStartSignal()
    {
        GameObject startDayButton = Instantiate(daySignalButtonPrefab, chatBubbleHolder);
        Button button = startDayButton.GetComponent<Button>();
        button.GetComponentInChildren<TMP_Text>().text = "Start Day";
        button.onClick.AddListener(() => 
        {
            GameManager.Instance.TriggerDayTimePassing();
            Destroy(startDayButton);
        });
    }

    public void DayEndSignal()
    {
        GameObject endDayButton = Instantiate(daySignalButtonPrefab, chatBubbleHolder);
        Button button = endDayButton.GetComponent<Button>();
        button.GetComponentInChildren<TMP_Text>().text = "End Day";
        button.onClick.AddListener(() => 
        {
            GameManager.Instance.EndDay();
            Destroy(endDayButton);
        });
    }

    public void SaveToDayData(DayData dayData)
    {
        dayData.supervisorBubbleSequenceNames = dailySequences
            .Where(seq => seq != null)
            .Select(seq => seq.name)
            .ToList();
    }

    public void LoadFromDayData(DayData dayData)
    {
        List<ChatBubbleSequence> sequences = dayData.GetSupervisorSequences();
        dailySequences = sequences;
    }
}
