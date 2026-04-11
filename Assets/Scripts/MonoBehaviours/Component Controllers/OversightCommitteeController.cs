// using UnityEngine;

// public class OversightCommitteeController : MonoBehaviour
// {
//     private PointerHandler oversightCommitteePointerHandler;

//     private bool isSetUp = false;

//     #region Setup
//     public async void Setup(ChatLog chatLog)
//     {
//         if(isSetUp || !FindElements()) return;

//         this.chatLog = chatLog;
//         chatLog.isOpen = false;
//         logNameText.text = chatLog.logName;
//         chatLogPrefab = await AddressableController.Instance().RetrieveAddressable<GameObject>(Constants.Addressable.ChatLog);

//         entryPointerHandler.OnPointerClickEvent += (eventData) => OpenLog(chatLog);

//         isSetUp = true;
//     }

//     private bool FindElements()
//     {
//         uiCanvas = FindFirstObjectByType<Canvas>().transform;
//         logNameText = transform.Find(Constants.GameObjectNames.LogName).GetComponent<TMP_Text>();
//         entryPointerHandler = gameObject.AddComponent<PointerHandler>();
        
//         if(uiCanvas == null || logNameText == null)
//         {
//             Debug.LogError("Setup of LogDirectoryEntry failed. A necessary component wasn't found during setup.");
//             return false;
//         }

//         return true;
//     }
//     #endregion
// }
