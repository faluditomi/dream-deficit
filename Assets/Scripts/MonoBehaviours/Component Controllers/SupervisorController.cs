using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SupervisorController : MonoBehaviour
{
    private GameObject chatLogPrefab;
    private GameObject daySignalButtonPrefab;
    private Transform chatBubbleHolder;
    private ChatLog supervisorChatLog;
    // TODO: instead of making this public, the GameManager should be able to call the chat log controller directly
    //       by this class being derived from ChatLogController or smth
    public ChatLogController chatLogController;

    private void Awake()
    {
        chatLogPrefab = AddressableManager.Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePaths.ChatLogPrefab);
        daySignalButtonPrefab = AddressableManager.Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePaths.DaySignalButtonPrefab);
        supervisorChatLog = AddressableManager.Instance
            .RetrieveAddressable<ChatLog>(Constants.AddressablePaths.ChatLogPrefix + Constants.ChatLogs.Supervisor);
        GetComponent<Button>().onClick.AddListener(() => OpenChatLog());

        Transform windowContainer = FindFirstObjectByType<Canvas>().transform.Find(Constants.GameObjectNames.WindowContainer);
        chatLogController = Instantiate(chatLogPrefab, windowContainer).GetComponent<ChatLogController>();
        chatLogController.Setup(supervisorChatLog);
        chatLogController.GetComponent<TopBarHandler>().Close();
        chatBubbleHolder = chatLogController.transform.Find(Constants.GameObjectNames.Viewport).Find(Constants.GameObjectNames.Content);
    }

    private void OpenChatLog()
    {
        if(!chatLogController.GetIsOpen())
        {
            chatLogController.Open();
        }
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
}
