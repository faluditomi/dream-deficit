using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

public class GameTemplateEditor : EditorWindow
{
    private GameTemplate gameTemplate;
    private Vector2 scrollPosition;
    private int selectedDayIndex = -1;
    private int selectedChatLogIndex = -1;
    private ReorderableList dayReorderableList;
    private bool showMarkerDataFoldout = false;
    private bool showSequencedChatLogsFoldout = false;

    private string[] markerTypeNames;

    [MenuItem("Custom Tools/Game Template Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<GameTemplateEditor>("Game Template Editor");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        RefreshMarkerTypeNames();
    }

    private void RefreshMarkerTypeNames()
    {
        var markerTypeFields = typeof(Markers).GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static);
        var markerTypeList = new List<string>();

        foreach(var field in markerTypeFields)
        {
            if(field.FieldType == typeof(MarkerType))
            {
                MarkerType markerType = (MarkerType)field.GetValue(null);

                if(markerType != null)
                {
                    markerTypeList.Add(markerType.name);
                }
            }
        }

        markerTypeNames = markerTypeList.ToArray();
    }

    private T FindAssetByName<T>(string name) where T : ScriptableObject
    {
        if(string.IsNullOrEmpty(name)) return null;
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);

        foreach(var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if(asset != null && asset.name == name)
            {
                return asset;
            }
        }
        return null;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Game Template Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        gameTemplate = EditorGUILayout.ObjectField("Game Template", gameTemplate, typeof(GameTemplate), false) as GameTemplate;
        
        if(EditorGUI.EndChangeCheck())
        {
            dayReorderableList = null;
            selectedDayIndex = -1;
            selectedChatLogIndex = -1;
        }

        if(gameTemplate == null)
        {
            EditorGUILayout.HelpBox("Assign a GameTemplate asset to begin editing.", MessageType.Info);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space();
        DrawDayList();
        EditorGUILayout.Space();

        if(selectedDayIndex >= 0 && selectedDayIndex < gameTemplate.dayEntries.Count)
        {
            DrawSelectedDay(selectedDayIndex);
        }

        EditorGUILayout.Space();
        DrawSequencedChatLogsSection();
        EditorGUILayout.Space();
        EditorGUILayout.EndScrollView();
    }

    private void DrawDayList()
    {
        if(dayReorderableList == null)
        {
            SetupDayReorderableList();
        }

        dayReorderableList.DoLayoutList();
    }

    private void SetupDayReorderableList()
    {
        dayReorderableList = new ReorderableList(gameTemplate.dayEntries, typeof(GameTemplate.DayDataEntry), true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Days"),
            drawElementCallback = (rect, index, active, focused) =>
            {
                if(index < 0 || index >= gameTemplate.dayEntries.Count) return;
                var entry = gameTemplate.dayEntries[index];
                string text = GetDayEntryDisplayText(index, entry);
                rect.y += 2;
                EditorGUI.LabelField(rect, $"{index}: {text}");
            },
            onSelectCallback = list =>
            {
                selectedDayIndex = list.index;
            },
            onAddCallback = list =>
            {
                int nextDayNumber = gameTemplate.dayEntries.Count + 1;
                DayData newDayData = new DayData { dayNumber = nextDayNumber };
                gameTemplate.dayEntries.Add(new GameTemplate.DayDataEntry { dayNumber = nextDayNumber, dayData = newDayData });
                list.index = gameTemplate.dayEntries.Count - 1;
                selectedDayIndex = list.index;
                AutoSave();
            },
            onRemoveCallback = list =>
            {
                RecalculateAndRemoveDay(list.index);
                list.index = Mathf.Clamp(list.index, 0, Mathf.Max(0, gameTemplate.dayEntries.Count - 1));
            },
            onReorderCallback = list =>
            {
                RecalculateDayNumbers();
                selectedDayIndex = list.index;
                AutoSave();
            }
        };
    }

    private string GetDayEntryDisplayText(int index, GameTemplate.DayDataEntry entry)
    {
        string text = "Day " + (index + 1);

        if(entry.dayData != null)
        {
            int markerCount = entry.dayData.markerData != null ? entry.dayData.markerData.Count : 0;
            int chatLogCount = entry.dayData.activeChatLogNames != null ? entry.dayData.activeChatLogNames.Count : 0;
            text += " - " + chatLogCount + " logs, " + markerCount + " markers";
        }
        else
        {
            text += " (empty)";
        }

        return text;
    }

    private void RecalculateDayNumbers()
    {
        for(int i = 0; i < gameTemplate.dayEntries.Count; i++)
        {
            var entry = gameTemplate.dayEntries[i];
            entry.dayNumber = i + 1;

            if(entry.dayData != null)
            {
                entry.dayData.dayNumber = i + 1;
            }

            gameTemplate.dayEntries[i] = entry;
        }
    }

    private void RecalculateAndRemoveDay(int index)
    {
        gameTemplate.dayEntries.RemoveAt(index);
        RecalculateDayNumbers();

        if(selectedDayIndex >= gameTemplate.dayEntries.Count)
        {
            selectedDayIndex = -1;
        }

        AutoSave();
    }

    private void DrawSelectedDay(int index)
    {
        var entry = gameTemplate.dayEntries[index];
        DayData dayData = entry.dayData;

        if(dayData == null)
        {
            dayData = new DayData { dayNumber = entry.dayNumber };
            gameTemplate.dayEntries[index] = new GameTemplate.DayDataEntry
            {
                dayNumber = entry.dayNumber,
                dayData = dayData
            };
        }

        EditorGUILayout.LabelField("Day " + dayData.dayNumber, EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        DrawChatLogSection(dayData);
        EditorGUILayout.Space(10);
        DrawMarkerTypeSection(dayData);
        EditorGUILayout.Space(10);
        DrawSupervisorSequenceSection(dayData);
        EditorGUILayout.Space(10);
        DrawMarkerDataSection(dayData);
    }

    private void DrawChatLogSection(DayData dayData)
    {
        EditorGUILayout.LabelField("Active Chat Logs", EditorStyles.label);

        if(dayData.activeChatLogNames == null)
        {
            dayData.activeChatLogNames = new List<string>();
        }

        int chatLogToRemove = -1;

        for(int i = 0; i < dayData.activeChatLogNames.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            ChatLog currentLog = FindAssetByName<ChatLog>(dayData.activeChatLogNames[i]);
            EditorGUI.BeginChangeCheck();
            ChatLog newLog = (ChatLog)EditorGUILayout.ObjectField(currentLog, typeof(ChatLog), false);
           
            if(EditorGUI.EndChangeCheck())
            {
                dayData.activeChatLogNames[i] = newLog != null ? newLog.name : string.Empty;
                AutoSave();
            }

            if(GUILayout.Button("-", GUILayout.Width(25)))
            {
                chatLogToRemove = i;
            }

            EditorGUILayout.EndHorizontal();
        }

        if(chatLogToRemove >= 0)
        {
            dayData.activeChatLogNames.RemoveAt(chatLogToRemove);
            AutoSave();
        }

        if(GUILayout.Button("+ Add Chat Log", GUILayout.Width(120)))
        {
            dayData.activeChatLogNames.Add(string.Empty);
            AutoSave();
        }
    }

    private void DrawMarkerTypeSection(DayData dayData)
    {
        EditorGUILayout.LabelField("Active Marker Types", EditorStyles.label);

        if(dayData.markerTypeNames == null)
        {
            dayData.markerTypeNames = new List<string>();
        }

        int markerTypeToRemove = -1;

        for(int i = 0; i < dayData.markerTypeNames.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            int selectedIndex = System.Array.IndexOf(markerTypeNames, dayData.markerTypeNames[i]);
            if(selectedIndex < 0) selectedIndex = 0;
            EditorGUI.BeginChangeCheck();
            int newSelected = EditorGUILayout.Popup(selectedIndex, markerTypeNames, GUILayout.Width(250));
            
            if(EditorGUI.EndChangeCheck())
            {
                dayData.markerTypeNames[i] = markerTypeNames[newSelected];
                AutoSave();
            }

            if(GUILayout.Button("-", GUILayout.Width(25)))
            {
                markerTypeToRemove = i;
            }

            EditorGUILayout.EndHorizontal();
        }
        if(markerTypeToRemove >= 0)
        {
            dayData.markerTypeNames.RemoveAt(markerTypeToRemove);
            AutoSave();
        }

        if(GUILayout.Button("+ Add Marker Type", GUILayout.Width(140)))
        {
            dayData.markerTypeNames.Add(markerTypeNames.Length > 0 ? markerTypeNames[0] : string.Empty);
            AutoSave();
        }
    }

    private void DrawMarkerDataSection(DayData dayData)
    {
        showMarkerDataFoldout = EditorGUILayout.Foldout(showMarkerDataFoldout, "Marker Data", true);

        if(showMarkerDataFoldout)
        {
            EditorGUI.indentLevel++;

            if(dayData.markerData == null)
            {
                dayData.markerData = new List<MarkerData>();
            }

            int markerToRemove = -1;
            for(int i = 0; i < dayData.markerData.Count; i++)
            {
                var markerData = dayData.markerData[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Marker #" + (i + 1), EditorStyles.boldLabel);
                
                if(GUILayout.Button("-", GUILayout.Width(25)))
                {
                    markerToRemove = i;
                }

                EditorGUILayout.EndHorizontal();
                int markerTypeIndex = System.Array.IndexOf(markerTypeNames, markerData.markerTypeName);
                if(markerTypeIndex < 0) markerTypeIndex = 0;
                EditorGUI.BeginChangeCheck();
                int newMarkerTypeIndex = EditorGUILayout.Popup("Marker Type", markerTypeIndex, markerTypeNames);
                
                if(EditorGUI.EndChangeCheck())
                {
                    markerData.markerTypeName = markerTypeNames[newMarkerTypeIndex];
                    AutoSave();
                }

                ChatLog currentLog = FindAssetByName<ChatLog>(markerData.chatLogPath);
                EditorGUI.BeginChangeCheck();
                ChatLog newLog = (ChatLog)EditorGUILayout.ObjectField("Chat Log", currentLog, typeof(ChatLog), false);
                
                if(EditorGUI.EndChangeCheck())
                {
                    markerData.chatLogPath = newLog != null ? newLog.name : string.Empty;
                    AutoSave();
                }

                EditorGUI.BeginChangeCheck();
                markerData.chatBubbleIndex = EditorGUILayout.IntField("Bubble Index", markerData.chatBubbleIndex);
                markerData.startIndex = EditorGUILayout.IntField("Start Index", markerData.startIndex);
                markerData.endIndex = EditorGUILayout.IntField("End Index", markerData.endIndex);
                
                if(EditorGUI.EndChangeCheck())
                {
                    AutoSave();
                }

                EditorGUILayout.EndVertical();
            }
            if(markerToRemove >= 0)
            {
                dayData.markerData.RemoveAt(markerToRemove);
                AutoSave();
            }

            if(GUILayout.Button("+ Add Marker Data", GUILayout.Width(140)))
            {
                MarkerData newMd = new MarkerData();
                newMd.markerTypeName = markerTypeNames.Length > 0 ? markerTypeNames[0] : string.Empty;
                dayData.markerData.Add(newMd);
                AutoSave();
            }

            EditorGUI.indentLevel--;
        }
    }

    private void DrawSupervisorSequenceSection(DayData dayData)
    {
        EditorGUILayout.LabelField("Supervisor Bubble Sequences", EditorStyles.label);

        if(dayData.supervisorBubbleSequenceNames == null)
        {
            dayData.supervisorBubbleSequenceNames = new List<string>();
        }

        int sequenceToRemove = -1;
        
        for(int i = 0; i < dayData.supervisorBubbleSequenceNames.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            ChatBubbleSequence currentSeq = FindAssetByName<ChatBubbleSequence>(dayData.supervisorBubbleSequenceNames[i]);
            EditorGUI.BeginChangeCheck();
            ChatBubbleSequence newSeq = (ChatBubbleSequence)EditorGUILayout.ObjectField(currentSeq, typeof(ChatBubbleSequence), false);
            
            if(EditorGUI.EndChangeCheck())
            {
                dayData.supervisorBubbleSequenceNames[i] = newSeq != null ? newSeq.name : string.Empty;
                AutoSave();
            }

            if(GUILayout.Button("-", GUILayout.Width(25)))
            {
                sequenceToRemove = i;
            }

            EditorGUILayout.EndHorizontal();
        }
        if(sequenceToRemove >= 0)
        {
            dayData.supervisorBubbleSequenceNames.RemoveAt(sequenceToRemove);
            AutoSave();
        }

        if(GUILayout.Button("+ Add Sequence", GUILayout.Width(120)))
        {
            dayData.supervisorBubbleSequenceNames.Add(string.Empty);
            AutoSave();
        }
    }

    private void DrawSequencedChatLogsSection()
    {
        showSequencedChatLogsFoldout = EditorGUILayout.Foldout(showSequencedChatLogsFoldout, "Sequenced Chat Logs", true);

        if(showSequencedChatLogsFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();

            if(GUILayout.Button("Add Chat Log Entry", GUILayout.Width(140)))
            {
                gameTemplate.sequencedChatLogEntries.Add(new GameTemplate.SequencedChatLogEntry
                {
                    chatLog = null,
                    sequences = new List<ChatBubbleSequence>()
                });
                selectedChatLogIndex = gameTemplate.sequencedChatLogEntries.Count - 1;
                AutoSave();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
            int sequencedChatLogToRemove = -1;

            for(int i = 0; i < gameTemplate.sequencedChatLogEntries.Count; i++)
            {
                var entry = gameTemplate.sequencedChatLogEntries[i];
                bool isSelected = (i == selectedChatLogIndex);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                GUIStyle headerStyle = new GUIStyle(EditorStyles.label);
                
                if(isSelected)
                {
                    headerStyle.fontStyle = FontStyle.Bold;
                }

                EditorGUILayout.LabelField("Chat Log #" + (i + 1), headerStyle, GUILayout.Width(100));

                if(GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    selectedChatLogIndex = isSelected ? -1 : i;
                }

                if(GUILayout.Button("X", GUILayout.Width(25)))
                {
                    sequencedChatLogToRemove = i;
                }

                EditorGUILayout.EndHorizontal();

                if(isSelected)
                {
                    EditorGUILayout.Space(5);
                    EditorGUI.BeginChangeCheck();
                    ChatLog newChatLog = (ChatLog)EditorGUILayout.ObjectField("Chat Log", entry.chatLog, typeof(ChatLog), false);
                    
                    if(EditorGUI.EndChangeCheck())
                    {
                        entry.chatLog = newChatLog;
                        gameTemplate.sequencedChatLogEntries[i] = entry;
                        AutoSave();
                    }

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Bubble Sequences", EditorStyles.label);

                    if(entry.sequences == null)
                    {
                        entry.sequences = new List<ChatBubbleSequence>();
                        gameTemplate.sequencedChatLogEntries[i] = entry;
                    }

                    int sequenceIndexToRemove = -1;
                    
                    for(int j = 0; j < entry.sequences.Count; j++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginChangeCheck();
                        ChatBubbleSequence newSeq = (ChatBubbleSequence)EditorGUILayout.ObjectField(entry.sequences[j], typeof(ChatBubbleSequence), false);
                        
                        if(EditorGUI.EndChangeCheck())
                        {
                            entry.sequences[j] = newSeq;
                            gameTemplate.sequencedChatLogEntries[i] = entry;
                            AutoSave();
                        }

                        if(GUILayout.Button("-", GUILayout.Width(25)))
                        {
                            sequenceIndexToRemove = j;
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    if(sequenceIndexToRemove >= 0)
                    {
                        entry.sequences.RemoveAt(sequenceIndexToRemove);
                        gameTemplate.sequencedChatLogEntries[i] = entry;
                        AutoSave();
                    }

                    if(GUILayout.Button("+ Add Sequence", GUILayout.Width(120)))
                    {
                        entry.sequences.Add(null);
                        gameTemplate.sequencedChatLogEntries[i] = entry;
                        AutoSave();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            if(sequencedChatLogToRemove >= 0)
            {
                gameTemplate.sequencedChatLogEntries.RemoveAt(sequencedChatLogToRemove);
                AutoSave();
            }

            EditorGUI.indentLevel--;
        }
    }

    private void AutoSave()
    {
        if(gameTemplate == null) return;
        EditorUtility.SetDirty(gameTemplate);
        AssetDatabase.SaveAssetIfDirty(gameTemplate);
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for(int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
