using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TODO: maybe this could be revamped to handle files and folders too (since those will probably only have a name too in code)
public class AssignmentEntryController : MonoBehaviour
{
    private ChatLog myChatLog;
    private ChatLogController myChatLogController;
    private TMP_Text logNameText;
    private GameObject lockPanel;
    private Animator animator;
    private bool isLocked;

    public void Setup(ChatLog chatLog, bool isBonus, bool isUnlocked)
    {
        myChatLog = chatLog;
        isLocked = isBonus && !isUnlocked;
        logNameText = transform.Find(Constants.GameObjectNames.Name).GetComponent<TMP_Text>();
        logNameText.text = chatLog.logName;
        lockPanel = transform.Find(Constants.GameObjectNames.Lock).gameObject;
        animator = GetComponent<Animator>();
        // the chat window is always instantiated (and starts closed). locked logs keep it
        // closed until the player unlocks them; content stays suppressed by the locked-log
        // drop in ConversationRunner.EvaluateEntries
        myChatLogController = ChatLogManager.Instance.InstantiateChatLog(chatLog, transform);
        lockPanel.SetActive(isLocked);
        GetComponent<Button>().onClick.AddListener(OnEntryClicked);
    }

    private void OnEntryClicked()
    {
        if(isLocked) UnlockAndOpen();
        else if(myChatLogController != null) myChatLogController.Open();
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
        myChatLogController.Open();
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
