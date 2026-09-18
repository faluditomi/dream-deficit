using UnityEngine;

/// <summary>
/// Authoring asset for a save slot. This is an identity, not a store: runtime state lives in the
/// JSON save file at Application.persistentDataPath/save_{slotName}.json, seeded from the template.
/// </summary>
[CreateAssetMenu(fileName = "SaveSlot", menuName = "Scriptable Objects/SaveSlot")]
public class SaveSlot : ScriptableObject
{
    public string slotName;
    public GameTemplate template;
}
