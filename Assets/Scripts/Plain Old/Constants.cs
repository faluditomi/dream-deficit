public static class Constants
{
    #region Classes
    public static class AddressablePrefixes
    {
        public const string ChatUser = "chat_user/";
        public const string ChatLog = "chat_log/";
        public const string ChatBubbleSequence = "chat_bubble_sequence/";
        public const string SequenceEventChannel = "sequence_event_channel/";
        public const string Prefab = "prefabs/";
    }

    public static class AddressableLabels
    {
        public const string EventChannel = "EventChannel";
        public const string EventChatBubbleSequence = "EventChatBubbleSequence";
    }

    public static class SequenceEventChannels
    {
        public const string MarkerOverload = AddressablePrefixes.SequenceEventChannel + "marker_overload";
    }

    // TODO: maybe we could rework all the other Constants, so we don't have to have 
    //       multiple Constants calls in one line in other scripts
    public static class AddressablePrefabs
    {
        public const string LogDirectoryEntry = AddressablePrefixes.Prefab + "log_directory_entry";
        public const string ChatLog = AddressablePrefixes.Prefab + "chat_log";
        public const string ChatBubble = AddressablePrefixes.Prefab + "chat_bubble";
        public const string MarkerFlag = AddressablePrefixes.Prefab + "marker_flag";
        public const string TopBar = AddressablePrefixes.Prefab + "top_bar";
        public const string MarkerCheatSheet = AddressablePrefixes.Prefab + "marker_cheat_sheet";
        public const string MarkerCheatSheetEntry = AddressablePrefixes.Prefab + "marker_cheat_sheet_entry";
        public const string DaySignalButton = AddressablePrefixes.Prefab + "day_signal_button";
    }

    public static class ChatLogs
    {
        public const string Supervisor = "supervisor";
    }

    public static class ChatBubbleSequences
    {
        public const string DaySignalTest = "day_signal_test";
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
        public const string Clock = "Clock";
        public const string Time = "Time";
        public const string Viewport = "Viewport";
        public const string Content = "Content";
    }

    public static class SceneNames
    {
        public const string DreamPrefix = "dream_scene_day_";
        public const string Desktop = "v1 prototye";
    }
    #endregion

    #region Enums
    public enum ChatUser
    {
        Delilah,
        Dave,
        Supervisor
    }

    public enum ChatBubbleSequenceType
    {
        Simple,
        SupervisorDayEnd,
        SupervisorDayStart
    }

    public enum SequenceEventType
    {
        MarkerOverload
    }
    #endregion
}
