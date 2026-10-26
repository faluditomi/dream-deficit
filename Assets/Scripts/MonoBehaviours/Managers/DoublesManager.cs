using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// TODO: need some logic to dynamically set baselines and profile types (client/moderator) for each marker for the next day
//          - should utilise the flags in the Conversation Graph system
public class DoublesManager : Singleton<DoublesManager>, IDayLoadable, IRunLoadable, IRunSavable
{
    private Dictionary<ChatUser, DataDoubleProfile> userProfiles = new Dictionary<ChatUser, DataDoubleProfile>();

    protected override void Awake()
    {
        base.Awake();
        SequenceEventChannel workEndEventChannel = AddressableManager
            .Instance
            .RetrieveAddressable<SequenceEventChannel>(Constants.SequenceEventChannels.WorkEnd);
        workEndEventChannel.OnSequenceEvent += _ => GenerateDoublesAndUpdateProfiles();
    }

    public void LoadFromDayData(DayData dayData)
    {
        // TODO: check if the current day's chat users are present in the userProfiles. if not, create a profile for them.
        // TODO: check the current day's assignments and the connected ChatUser that is "on trial" in it. make sure they have a profile.
    }

    public void LoadFromRunData(RunData runData)
    {
        userProfiles = new Dictionary<ChatUser, DataDoubleProfile>();

        if(runData.dataDoubleProfiles != null)
        {
            foreach(DataDoubleProfileEntry entry in runData.dataDoubleProfiles)
            {
                if(entry == null || string.IsNullOrEmpty(entry.chatUserName)) continue;

                ChatUser chatUser = AddressableManager.Instance.RetrieveAddressable<ChatUser>(Constants.AddressablePrefixes.ChatUser + entry.chatUserName.ToLower());

                if(chatUser == null)
                {
                    Debug.LogWarning($"DoublesManager: could not resolve ChatUser '{entry.chatUserName}'. Skipping profile.");
                    continue;
                }

                userProfiles[chatUser] = ToDataDoubleProfile(entry);
            }
        }

        SeedPlayerProfile();
    }

    public void SaveToRunData(RunData runData)
    {
        runData.dataDoubleProfiles = new List<DataDoubleProfileEntry>();

        foreach(KeyValuePair<ChatUser, DataDoubleProfile> userProfile in userProfiles)
        {
            if(userProfile.Key == null || userProfile.Value == null) continue;
            runData.dataDoubleProfiles.Add(ToProfileEntry(userProfile.Key.name, userProfile.Value));
        }
    }

    private void GenerateDoublesAndUpdateProfiles()
    {
        // TODO: generate the daily doubles and update the global doubles   
        //          - how do we make sure daily doubles are created only for the player and the ones that are "on trial" in the assignments?
        //          - when do chat users get their doubles generated? will that be a scripted event instead? (since that is not based on user input)
    }

    public DataDoubleProfile GetDoubleProfileForUser(ChatUser user)
    {
        if(userProfiles.ContainsKey(user)) return userProfiles[user];
        return null;
    }

    private void SeedPlayerProfile()
    {
        if(userProfiles.Keys.All(key => key.chatUser != Constants.ChatUser.Avner))
        {
             ChatUser playerChatUser = AddressableManager
                .Instance
                .RetrieveAddressable<ChatUser>(Constants.AddressablePrefixes.ChatUser + Constants.ChatUser.Avner.ToString().ToLower());
            DataDouble playerGlobalDouble = new DataDouble(true, false);
            DataDoubleProfile playerDataDoubleProfile = new DataDoubleProfile(playerGlobalDouble);
            userProfiles.Add(playerChatUser, playerDataDoubleProfile);
        }
    }

    #region Serialization conversion
    private static DataDoubleProfileEntry ToProfileEntry(string chatUserName, DataDoubleProfile dataDoubleProfile)
    {
        DataDoubleProfileEntry entry = new DataDoubleProfileEntry();
        entry.chatUserName = chatUserName;
        entry.global = ToDataDoubleSave(dataDoubleProfile.GetGlobalDataDouble());

        foreach(KeyValuePair<int, DataDouble> dailyDouble in dataDoubleProfile.DailyDoubles)
        {
            entry.daily.Add(new DataDoubleDaySave
            {
                day = dailyDouble.Key,
                dataDouble = ToDataDoubleSave(dailyDouble.Value)
            });
        }

        return entry;
    }

    private static DataDoubleSave ToDataDoubleSave(DataDouble dataDouble)
    {
        DataDoubleSave save = new DataDoubleSave();
        if(dataDouble == null) return save;
        save.isModeratorProfile = dataDouble.isModeratorProfile;
        save.isClientProfile = dataDouble.isClientProfile;

        foreach(DoubleMarker marker in dataDouble.MarkerKeys)
        {
            if(marker == null) continue;

            save.markers.Add(new MarkerEntry
            {
                markerName = marker.name,
                markerType = marker.markerType,
                baseline = dataDouble.GetMarkerBaseline(marker),
                score = dataDouble.GetMarkerScore(marker)
            });
        }

        return save;
    }

    private static DataDoubleProfile ToDataDoubleProfile(DataDoubleProfileEntry entry)
    {
        DataDoubleProfile dataDoubleProfile = new DataDoubleProfile(ToDataDouble(entry.global));
        if(entry.daily == null) return dataDoubleProfile;

        foreach(DataDoubleDaySave dailyDouble in entry.daily)
        {
            if(dailyDouble == null || dailyDouble.dataDouble == null) continue;
            dataDoubleProfile.AddDailyDouble(dailyDouble.day, ToDataDouble(dailyDouble.dataDouble));
        }

        return dataDoubleProfile;
    }

    private static DataDouble ToDataDouble(DataDoubleSave save)
    {
        if(save == null) return new DataDouble(false, false);
        DataDouble dataDouble = new DataDouble(save.isModeratorProfile, save.isClientProfile);
        if(save.markers == null) return dataDouble;

        foreach(MarkerEntry markerEntry in save.markers)
        {
            if(markerEntry == null || string.IsNullOrEmpty(markerEntry.markerName)) continue;
            DoubleMarker marker = AddressableManager.Instance.RetrieveAddressable<DoubleMarker>(Constants.AddressablePrefixes.DoubleMarker + markerEntry.markerName.ToLower());

            if(marker == null)
            {
                Debug.LogWarning($"DoublesManager: could not resolve DoubleMarker '{markerEntry.markerName}'. Skipping marker.");
                continue;
            }

            dataDouble.SetMarker(marker, markerEntry.baseline, markerEntry.score);
        }

        return dataDouble;
    }
    #endregion
}
