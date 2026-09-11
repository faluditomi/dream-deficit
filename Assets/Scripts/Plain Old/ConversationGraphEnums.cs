public enum ConversationNodeKind { Bubble = 0, Choice = 1, Wait = 2, Entry = 3, End = 4 }
public enum ConversationConditionKind { Event = 0, DayMin = 1, DayMax = 2, RequiredFlag = 3 }
public enum ConversationEffectOperation { SetFlag = 0, RaiseEvent = 1 }
public enum ConversationThreadStateKind { Playing = 0, ParkedChoice = 1, ParkedWait = 2 }
