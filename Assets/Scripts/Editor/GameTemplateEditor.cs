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

    private string[] flagTypeNames;

    [MenuItem("Custom Tools/Game Template Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<GameTemplateEditor>("Game Template Editor");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        RefreshFlagTypeNames();
    }

    private void RefreshFlagTypeNames()
    {
        var flagTypeFields = typeof(Flags).GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static);
        var flagTypeList = new List<string>();

        foreach(var field in flagTypeFields)
        {
            if(field.FieldType == typeof(FlagType))
            {
                FlagType flagType = (FlagType)field.GetValue(null);

                if(flagType != null)
                {
                    flagTypeList.Add(flagType.name);
                }
            }
        }

        flagTypeNames = flagTypeList.ToArray();
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
            int chatLogCount = entry.dayData.activeAssignments != null ? entry.dayData.activeAssignments.Count : 0;
            text += " - " + chatLogCount + " logs";
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
        DrawFlagTypeSection(dayData);
        EditorGUILayout.Space(10);
        DrawChatClientUsersSection(dayData);
        EditorGUILayout.Space(10);
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
            ChatLog currentLog = FindAssetByName<ChatLog>(entry.chatLogPath);
            EditorGUI.BeginChangeCheck();
            ChatLog newLog = (ChatLog)EditorGUILayout.ObjectField(currentLog, typeof(ChatLog), false);
           
            if(EditorGUI.EndChangeCheck())
            {
                entry.chatLogPath = newLog != null ? newLog.name : string.Empty;
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
            dayData.activeAssignments.Add(new ChatLogEntry { chatLogPath = string.Empty, isBonus = false });
            AutoSave();
        }
    }

    private void DrawFlagTypeSection(DayData dayData)
    {
        EditorGUILayout.LabelField("Active Flag Types", EditorStyles.label);

        if(dayData.flagTypeNames == null)
        {
            dayData.flagTypeNames = new List<string>();
        }

        int flagTypeToRemove = -1;

        for(int i = 0; i < dayData.flagTypeNames.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            int selectedIndex = System.Array.IndexOf(flagTypeNames, dayData.flagTypeNames[i]);
            if(selectedIndex < 0) selectedIndex = 0;
            EditorGUI.BeginChangeCheck();
            int newSelected = EditorGUILayout.Popup(selectedIndex, flagTypeNames, GUILayout.Width(250));
            
            if(EditorGUI.EndChangeCheck())
            {
                dayData.flagTypeNames[i] = flagTypeNames[newSelected];
                AutoSave();
            }

            if(GUILayout.Button("-", GUILayout.Width(25)))
            {
                flagTypeToRemove = i;
            }

            EditorGUILayout.EndHorizontal();
        }
        if(flagTypeToRemove >= 0)
        {
            dayData.flagTypeNames.RemoveAt(flagTypeToRemove);
            AutoSave();
        }

        if(GUILayout.Button("+ Add Flag Type", GUILayout.Width(140)))
        {
            dayData.flagTypeNames.Add(flagTypeNames.Length > 0 ? flagTypeNames[0] : string.Empty);
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
