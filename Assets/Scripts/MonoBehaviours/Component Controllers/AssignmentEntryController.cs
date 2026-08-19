using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TODO: maybe this could be revamped to handle files and folders too (since those will probably only have a name too in code)
public class AssignmentEntryController : MonoBehaviour
{
    private ChatLog myChatLog;
    private ChatLogController chatLogController;
    private TMP_Text logNameText;
    private GameObject lockPanel;
    private Animator animator;
    private Button button;
    private bool isLocked;

    public void Setup(ChatLog chatLog, bool isBonus, bool isUnlocked)
    {
        myChatLog = chatLog;
        isLocked = isBonus && !isUnlocked;
        logNameText = transform.Find(Constants.GameObjectNames.Name).GetComponent<TMP_Text>();
        logNameText.text = chatLog.logName;
        lockPanel = transform.Find(Constants.GameObjectNames.Lock).gameObject;
        animator = GetComponent<Animator>();
        button = GetComponent<Button>();
        // the chat window is always instantiated (and starts closed). locked logs keep it
        // closed until the player unlocks them; content stays suppressed by the locked-log
        // guards in ChatLogController.RunBubbleSequence and SaveManager.GetSequencedChatBubblesForChatLog
        chatLogController = ChatLogManager.Instance.InstantiateChatLog(chatLog, transform);
        lockPanel.SetActive(isLocked);
        button.onClick.AddListener(OnEntryClicked);
    }

    private void OnEntryClicked()
    {
        if(isLocked) UnlockAndOpen();
        else if(chatLogController != null) chatLogController.Open();
    }

    private void UnlockAndOpen()
    {
        // play the reveal animation when the animator is wired with an Unlock trigger;
        // otherwise hide the lock panel directly so the data is revealed regardless
        if(animator != null && HasUnlockTrigger()) animator.SetTrigger(Constants.AnimationTriggers.UnlockAssignmentEntry);
        else lockPanel.SetActive(false);
        // record the unlock on the runtime day data
        DayData dayData = SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber);
        dayData?.UnlockLog(myChatLog.logName);
        isLocked = false;
        chatLogController.Open();
    }

    private bool HasUnlockTrigger()
    {
        foreach(AnimatorControllerParameter parameter in animator.parameters)
        {
            if(parameter.type == AnimatorControllerParameterType.Trigger
            && parameter.name == Constants.AnimationTriggers.UnlockAssignmentEntry)
            {
                return true;
            }
        }

        return false;
    }
}
