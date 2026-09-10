public static class Constants
{
    #region Classes
    public static class AddressablePrefixes
    {
        public const string ChatUser = "chat_user/";
        public const string ChatLog = "chat_log/";
        public const string ConversationGraph = "conversation_graph/";
        public const string EventChannel = "sequence_event_channel/";
        public const string Prefab = "prefabs/";
    }

    public static class AddressableLabels
    {
        public const string EventChannel = "SequenceEventChannel";
        public const string ConversationGraph = "ConversationGraph";
    }

    public static class SequenceEventChannels
    {
        public const string MarkerOverload = AddressablePrefixes.EventChannel + "marker_overload";
        public const string DayStart = AddressablePrefixes.EventChannel + "day_start";
        public const string DayEnd = AddressablePrefixes.EventChannel + "day_end";
    }

    // TODO: maybe we could rework all the other Constants, so we don't have to have 
    //       multiple Constants calls in one line in other scripts
    public static class AddressablePrefabs
    {
        public const string AssignmentEntry = AddressablePrefixes.Prefab + "assignment_entry";
        public const string ChatLog = AddressablePrefixes.Prefab + "chat_log";
        public const string ChatBubble = AddressablePrefixes.Prefab + "chat_bubble";
        public const string MarkerFlag = AddressablePrefixes.Prefab + "marker_flag";
        public const string TopBar = AddressablePrefixes.Prefab + "top_bar";
        public const string MarkerCheatSheet = AddressablePrefixes.Prefab + "marker_cheat_sheet";
        public const string MarkerCheatSheetEntry = AddressablePrefixes.Prefab + "marker_cheat_sheet_entry";
        public const string ChatClientUserEntry = AddressablePrefixes.Prefab + "chat_client_user_entry";
        public const string WindowShadow = AddressablePrefixes.Prefab + "window_shadow";
        public const string UserChatResponseOption = AddressablePrefixes.Prefab + "user_chat_response_option";
    }

    public static class ChatLogs
    {
        public const string Phoebe = "phoebe";
        public const string Mara = "mara";
    }

    public static class GameObjectNames
    {
        public const string ProfilePicture = "Profile Picture";
        public const string Message = "Message";
        public const string Username = "Username";
        public const string CloseButton = "Close";
        public const string Name = "Name";
        public const string Description = "Description";
        public const string Keycode = "Keycode";
        public const string TypingIndicator = "Typing Indicator";
        public const string WindowContainer = "Window Container";
        public const string Lock = "Lock";
        public const string Clock = "Clock";
        public const string Time = "Time";
        public const string Viewport = "Viewport";
        public const string Content = "Content";
        public const string WindowName = "Window Name";
        public const string LastMessage = "Last Message";
        public const string UserListWindow = "User List Window";
        public const string ChatWindowHolder = "Chat Window Holder";
        public const string PreviewText = "Preview Text";
    }

    public static class SceneNames
    {
        public const string DreamPrefix = "dream_scene_day_";
        public const string Desktop = "v1 prototye";
    }

    public static class WindowAndFileNames
    {
        public const string FlagCheatSheet = "Flag Cheat Sheet";
        public const string AssignmentDocket = "Assignment Docket";
        public const string ChatClient = "Chat Client";
    }

    public static class AnimationTriggers
    {
        public const string UnlockAssignmentEntry = "Unlock";
    }
    #endregion

    #region Enums
    public enum ChatUser
    {
        Phoebe = 2,
        Mara = 3,
        Avner = 4,
        Moira = 5
    }

    public enum SequenceEventType
    {
        MarkerOverload = 0,
        DayStart = 1,
        DayEnd = 2,
        Default = 3
    }
    #endregion
}
