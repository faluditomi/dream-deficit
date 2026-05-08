using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class ChatLogEditor : EditorWindow
{
    private Object selectedAsset;
    private ChatLog chatLog;
    private ChatBubbleSequence chatBubbleSequence;
    private List<ChatBubble> bubbles = new List<ChatBubble>();
    private int bubbleIndex;
    private ReorderableList bubbleReorderableList;
    private const string TextAreaControlName = "ChatBubbleMarkableTextArea";
    private Vector2 mainScrollPosition;
    private Vector2 markableScrollPosition;
    private string editedMessage = string.Empty;
    private int newMarkableMarkerIndex;
    private int currentSelectionStart = -1;
    private int currentSelectionEnd = -1;
    private static MarkerType[] markerTypesCache;

    private static MarkerType[] MarkerTypes
    {
        get
        {
            if (markerTypesCache == null)
            {
                markerTypesCache = typeof(Markers)
                    .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .Where(field => field.FieldType == typeof(MarkerType))
                    .Select(field => field.GetValue(null) as MarkerType)
                    .Where(markerType => markerType != null)
                    .ToArray();
            }

            return markerTypesCache;
        }
    }

    [MenuItem("Window/Dream Deficit/Chat Log Editor")]
    public static void OpenWindow()
    {
        GetWindow<ChatLogEditor>("Chat Log Editor");
    }

    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
        RefreshTarget(Selection.activeObject);
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        RefreshTarget(Selection.activeObject);
        Repaint();
    }

    private void RefreshTarget(Object asset)
    {
        selectedAsset = asset;
        chatLog = asset as ChatLog;
        chatBubbleSequence = asset as ChatBubbleSequence;
        LoadBubbles();
        SetupBubbleReorderableList();
    }

    private void LoadBubbles()
    {
        bubbles = chatLog != null ? chatLog.messages : chatBubbleSequence != null ? chatBubbleSequence.messages : new List<ChatBubble>();
        if (bubbles == null)
        {
            bubbles = new List<ChatBubble>();
            if (chatLog != null)
            {
                chatLog.messages = bubbles;
            }
            else if (chatBubbleSequence != null)
            {
                chatBubbleSequence.messages = bubbles;
            }
        }

        bubbleIndex = Mathf.Clamp(bubbleIndex, 0, Mathf.Max(0, bubbles.Count - 1));
        UpdateEditedMessage();
    }

    private void SetupBubbleReorderableList()
    {
        if (bubbles == null) return;

        bubbleReorderableList = new ReorderableList(bubbles, typeof(ChatBubble), true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Bubbles"),
            drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= bubbles.Count) return;
                var bubble = bubbles[index];
                string text = bubble?.message ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text)) text = "<empty bubble>";
                else if (text.Length > 80) text = text.Substring(0, 80) + "...";
                rect.y += 2;
                EditorGUI.LabelField(rect, $"{index}: {text}");
            },
            onSelectCallback = list =>
            {
                bubbleIndex = list.index;
                UpdateEditedMessage();
            },
            onAddCallback = list =>
            {
                AddNewBubble();
                list.index = bubbles.Count - 1;
            },
            onRemoveCallback = list =>
            {
                RemoveBubbleAt(list.index);
                list.index = Mathf.Clamp(list.index, 0, Mathf.Max(0, bubbles.Count - 1));
            },
            onReorderCallback = list =>
            {
                bubbleIndex = list.index;
                UpdateEditedMessage();
                MarkDirty();
            }
        };
    }

    private void UpdateEditedMessage()
    {
        if (bubbles.Count > 0 && bubbleIndex >= 0 && bubbleIndex < bubbles.Count)
        {
            editedMessage = bubbles[bubbleIndex].message ?? string.Empty;
        }
        else
        {
            editedMessage = string.Empty;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Select a ChatLog or ChatBubbleSequence asset, then edit a bubble message and create markables from text selection.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        selectedAsset = EditorGUILayout.ObjectField("Target Asset", selectedAsset, typeof(Object), false);
        if (EditorGUI.EndChangeCheck())
        {
            RefreshTarget(selectedAsset);
        }

        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition, GUILayout.ExpandHeight(true));

        if (chatLog == null && chatBubbleSequence == null)
        {
            EditorGUILayout.HelpBox("Please select a ChatLog or ChatBubbleSequence asset in the Project window.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawBubbleList();
        EditorGUILayout.Space();
        DrawChatLogSettings();
        EditorGUILayout.Space();
        if (GetCurrentBubble() != null)
        {
            DrawMessageEditor();
            if (Event.current.type == EventType.Repaint)
            {
                CaptureTextAreaSelection();
            }
            EditorGUILayout.Space();
            DrawBubbleMetadata();
            EditorGUILayout.Space();
            DrawMarkableControls();
            EditorGUILayout.Space();
            DrawMarkables();
        }
        else
        {
            EditorGUILayout.HelpBox("No bubble selected. Use the list above to add or select a bubble.", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private void CaptureTextAreaSelection()
    {
        TextEditor editor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
        if (editor == null) return;

        currentSelectionStart = editor.selectIndex;
        currentSelectionEnd = editor.cursorIndex;

        if (currentSelectionStart == currentSelectionEnd)
        {
            currentSelectionStart = currentSelectionEnd = -1;
            return;
        }

        if (currentSelectionStart < 0 || currentSelectionEnd < 0)
        {
            currentSelectionStart = currentSelectionEnd = -1;
            return;
        }

        int start = Mathf.Min(currentSelectionStart, currentSelectionEnd);
        int end = Mathf.Max(currentSelectionStart, currentSelectionEnd);
        if (start < 0 || end > editedMessage.Length)
        {
            currentSelectionStart = currentSelectionEnd = -1;
            return;
        }
    }

    private void DrawBubbleList()
    {
        if (bubbleReorderableList == null)
        {
            SetupBubbleReorderableList();
        }

        if (bubbleReorderableList != null)
        {
            bubbleReorderableList.DoLayoutList();
        }
    }

    private void AddNewBubble()
    {
        if (selectedAsset == null) return;

        Undo.RecordObject(selectedAsset, "Add chat bubble");
        var newBubble = new ChatBubble
        {
            message = string.Empty,
            delayLength = 0,
            typingFlagLength = 0,
            markables = new List<Markable>()
        };

        bubbles.Add(newBubble);
        bubbleIndex = bubbles.Count - 1;
        UpdateEditedMessage();
        MarkDirty();
    }

    private void RemoveBubbleAt(int index)
    {
        if (selectedAsset == null || index < 0 || index >= bubbles.Count) return;

        Undo.RecordObject(selectedAsset, "Remove chat bubble");
        bubbles.RemoveAt(index);
        bubbleIndex = Mathf.Clamp(index, 0, Mathf.Max(0, bubbles.Count - 1));
        UpdateEditedMessage();
        MarkDirty();
    }

    private void DrawMessageEditor()
    {
        EditorGUILayout.LabelField("Editable Bubble Message", EditorStyles.boldLabel);
        GUI.SetNextControlName(TextAreaControlName);
        EditorGUI.BeginChangeCheck();
        editedMessage = GUILayout.TextArea(editedMessage, GUI.skin.textArea, GUILayout.Height(110));

        if (EditorGUI.EndChangeCheck())
        {
            ApplyMessageChange();
        }
    }

    private void DrawBubbleMetadata()
    {
        ChatBubble bubble = GetCurrentBubble();
        if (bubble == null) return;

        EditorGUILayout.LabelField("Bubble Data", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        var newUserId = (ChatUserId)EditorGUILayout.EnumPopup("Chat User", bubble.chatUserId);
        var newDelay = EditorGUILayout.Slider("Delay Length", bubble.delayLength, 0f, 10f);
        var newTyping = EditorGUILayout.Slider("Typing Flag Length", bubble.typingFlagLength, 0f, 10f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(selectedAsset, "Edit bubble metadata");
            bubble.chatUserId = newUserId;
            bubble.delayLength = newDelay;
            bubble.typingFlagLength = newTyping;
            MarkDirty();
        }
    }

    private void DrawChatLogSettings()
    {
        if (chatLog != null)
        {
            EditorGUILayout.LabelField("Chat Log Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string newLogName = EditorGUILayout.TextField("Log Name", chatLog.logName);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedAsset, "Edit chat log name");
                chatLog.logName = newLogName;
                MarkDirty();
            }
        }
        else if (chatBubbleSequence != null)
        {
            EditorGUILayout.LabelField("Chat Bubble Sequence", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Bubbles", bubbles.Count.ToString());
        }
    }

    private void ApplyMessageChange()
    {
        ChatBubble bubble = GetCurrentBubble();
        if (bubble == null) return;

        if (bubble.message != editedMessage)
        {
            Undo.RecordObject(selectedAsset, "Edit chat bubble message");
            bubble.message = editedMessage;
            bubble.SyncMarkables();
            MarkDirty();
        }
    }

    private void DrawMarkableControls()
    {
        EditorGUILayout.LabelField("New Markable", EditorStyles.boldLabel);
        newMarkableMarkerIndex = EditorGUILayout.Popup("Marker Type", newMarkableMarkerIndex, GetMarkerTypeLabels());

        EditorGUILayout.LabelField("Current Selection", GetCurrentSelectionDisplay());

        EditorGUI.BeginDisabledGroup(!HasTextSelection());
        if (GUILayout.Button("Create Markable from Selection"))
        {
            CreateMarkableFromSelection();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Sync all markable indexes from current message"))
        {
            SyncAllMarkables();
        }
    }

    private string GetCurrentSelectionDisplay()
    {
        if (currentSelectionStart < 0 || currentSelectionEnd < 0 || currentSelectionStart == currentSelectionEnd)
        {
            return "No active selection";
        }

        int start = Mathf.Min(currentSelectionStart, currentSelectionEnd);
        int end = Mathf.Max(currentSelectionStart, currentSelectionEnd);
        end = Mathf.Min(end, editedMessage.Length);

        if (end <= start)
            return "No active selection";

        string selected = editedMessage.Substring(start, end - start);
        return $"'{selected}' ({start} to {end - 1})";
    }

    private bool HasTextSelection()
    {
        return currentSelectionStart >= 0 && currentSelectionEnd >= 0 && currentSelectionStart != currentSelectionEnd && !string.IsNullOrEmpty(editedMessage);
    }

    private void CreateMarkableFromSelection()
    {
        ChatBubble bubble = GetCurrentBubble();
        if (bubble == null) return;

        if (!HasTextSelection()) return;

        int start = Mathf.Min(currentSelectionStart, currentSelectionEnd);
        int end = Mathf.Max(currentSelectionStart, currentSelectionEnd);
        end = Mathf.Min(end, editedMessage.Length);

        if (end <= start) return;

        string selectedText = editedMessage.Substring(start, end - start);
        var markerType = MarkerTypes[Mathf.Clamp(newMarkableMarkerIndex, 0, MarkerTypes.Length - 1)];

        Undo.RecordObject(selectedAsset, "Add chat bubble markable");
        bubble.markables.Add(new Markable
        {
            markerType = markerType,
            spanText = selectedText,
            occurrence = 0,
            startIndex = start,
            endIndex = end - 1
        });

        MarkDirty();
    }

    private void SyncAllMarkables()
    {
        ChatBubble bubble = GetCurrentBubble();
        if (bubble == null) return;

        Undo.RecordObject(selectedAsset, "Sync chat bubble markables");
        bubble.SyncMarkables();
        MarkDirty();
    }

    private void DrawMarkables()
    {
        ChatBubble bubble = GetCurrentBubble();
        if (bubble == null) return;

        if (bubble.markables == null)
        {
            bubble.markables = new List<Markable>();
        }

        markableScrollPosition = EditorGUILayout.BeginScrollView(markableScrollPosition, GUILayout.Height(260));

        for (int i = 0; i < bubble.markables.Count; i++)
        {
            Markable markable = bubble.markables[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Markable {i + 1}", EditorStyles.boldLabel);

            int currentMarkerIndex = GetMarkerIndex(markable?.markerType);
            int selectedMarkerIndex = EditorGUILayout.Popup("Marker Type", currentMarkerIndex, GetMarkerTypeLabels());
            if (selectedMarkerIndex != currentMarkerIndex)
            {
                Undo.RecordObject(selectedAsset, "Change markable type");
                markable.markerType = MarkerTypes[selectedMarkerIndex];
                MarkDirty();
            }

            string spanText = markable?.spanText ?? string.Empty;
            string newText = EditorGUILayout.TextField("Anchor Text", spanText);
            if (newText != spanText)
            {
                Undo.RecordObject(selectedAsset, "Edit markable span text");
                markable.spanText = newText;
                MarkDirty();
            }

            int newOccurrence = EditorGUILayout.IntField("Occurrence", markable?.occurrence ?? 0);
            if (newOccurrence != markable.occurrence)
            {
                Undo.RecordObject(selectedAsset, "Edit markable occurrence");
                markable.occurrence = Mathf.Max(0, newOccurrence);
                MarkDirty();
            }

            EditorGUILayout.LabelField("Indexes", $"{markable.startIndex} .. {markable.endIndex}");
            EditorGUILayout.LabelField("Resolved Text", markable.GetSelectedText(editedMessage));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Re-sync indexes"))
            {
                Undo.RecordObject(selectedAsset, "Re-sync markable indexes");
                markable.RecalculateIndexes(editedMessage);
                MarkDirty();
            }

            if (GUILayout.Button("Remove"))
            {
                Undo.RecordObject(selectedAsset, "Remove markable");
                bubble.markables.RemoveAt(i);
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private ChatBubble GetCurrentBubble()
    {
        if (bubbles == null || bubbles.Count == 0 || bubbleIndex < 0 || bubbleIndex >= bubbles.Count) return null;
        return bubbles[bubbleIndex];
    }

    private int GetMarkerIndex(MarkerType markerType)
    {
        if (markerType == null) return 0;
        for (int i = 0; i < MarkerTypes.Length; i++)
        {
            if (MarkerTypes[i].name == markerType.name)
            {
                return i;
            }
        }
        return 0;
    }

    private string[] GetMarkerTypeLabels()
    {
        return MarkerTypes.Select(mt => mt.name).ToArray();
    }

    private void MarkDirty()
    {
        if (selectedAsset != null)
        {
            EditorUtility.SetDirty(selectedAsset);
            if (selectedAsset is ScriptableObject)
            {
                AssetDatabase.SaveAssets();
            }
        }
    }
}
