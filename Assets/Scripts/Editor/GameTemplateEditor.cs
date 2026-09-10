using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

public class GameTemplateEditor : EditorWindow
{
    private GameTemplate gameTemplate;
    private Vector2 scrollPosition;
    private int selectedDayIndex = -1;
    private ReorderableList dayReorderableList;
    private bool showMarkerDataFoldout = false;

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
            int chatLogCount = entry.dayData.activeAssignments != null ? entry.dayData.activeAssignments.Count : 0;
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
        DrawChatClientUsersSection(dayData);
        EditorGUILayout.Space(10);
        DrawMarkerDataSection(dayData);
    }

    private void DrawChatLogSection(DayData dayData)
    {
        EditorGUILayout.LabelField("Active Assignments", EditorStyles.label);

        if(dayData.activeAssignments == null)
        {
            dayData.activeAssignments = new List<ChatLogEntry>();
        }

        int chatLogToRemove = -1;

        for(int i = 0; i < dayData.activeAssignments.Count; i++)
        {
            ChatLogEntry entry = dayData.activeAssignments[i];
            EditorGUILayout.BeginHorizontal();
            ChatLog currentLog = FindAssetByName<ChatLog>(entry.logName);
            EditorGUI.BeginChangeCheck();
            ChatLog newLog = (ChatLog)EditorGUILayout.ObjectField(currentLog, typeof(ChatLog), false);
           
            if(EditorGUI.EndChangeCheck())
            {
                entry.logName = newLog != null ? newLog.name : string.Empty;
                AutoSave();
            }

            EditorGUI.BeginChangeCheck();
            bool newIsBonus = EditorGUILayout.ToggleLeft("Bonus", entry.isBonus, GUILayout.Width(110));
            
            if(EditorGUI.EndChangeCheck())
            {
                entry.isBonus = newIsBonus;
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
            dayData.activeAssignments.RemoveAt(chatLogToRemove);
            AutoSave();
        }

        if(GUILayout.Button("+ Add Chat Log", GUILayout.Width(120)))
        {
            dayData.activeAssignments.Add(new ChatLogEntry { logName = string.Empty, isBonus = false });
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

    private void DrawChatClientUsersSection(DayData dayData)
    {
        EditorGUILayout.LabelField("Active Chat Client Users", EditorStyles.label);

        if(dayData.activeChatClientUsers == null)
        {
            dayData.activeChatClientUsers = new List<string>();
        }

        int userToRemove = -1;

        for(int i = 0; i < dayData.activeChatClientUsers.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            ChatUser currentChatUser = FindAssetByName<ChatUser>(dayData.activeChatClientUsers[i]);
            EditorGUI.BeginChangeCheck();
            ChatUser newChatUser = (ChatUser)EditorGUILayout.ObjectField(currentChatUser, typeof(ChatUser), false, GUILayout.Width(250));
            
            if(EditorGUI.EndChangeCheck())
            {
                dayData.activeChatClientUsers[i] = newChatUser != null ? newChatUser.name : string.Empty;
                AutoSave();
            }

            if(GUILayout.Button("-", GUILayout.Width(25)))
            {
                userToRemove = i;
            }

            EditorGUILayout.EndHorizontal();
        }
        if(userToRemove >= 0)
        {
            dayData.activeChatClientUsers.RemoveAt(userToRemove);
            AutoSave();
        }

        if(GUILayout.Button("+ Add Chat User", GUILayout.Width(140)))
        {
            dayData.activeChatClientUsers.Add(string.Empty);
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
