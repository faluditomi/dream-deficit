using System.Collections.Generic;
using System.Linq;

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
        userProfiles = runData.dataDoubleProfiles.ToDictionary(profile => profile.Key, profile => profile.Value);
        SeedPlayerProfile();
    }

    public void SaveToRunData(RunData runData)
    {
        runData.dataDoubleProfiles = userProfiles.ToDictionary(profile => profile.Key, profile => profile.Value);
    }

    private void GenerateDoublesAndUpdateProfiles()
    {
        // TODO: generate the daily doubles and update the global doubles   
        //          - how do we make sure daily doubles are created only for the player and the ones that are "on trial" in the assignments?
        //          - when do chat users get their doubles generated? will that be a scripted event instead? (since that is not based on user input)
    }

    public DataDoubleProfile GetDoubleProfileForUser(ChatUser user)
    {
        if(!userProfiles.ContainsKey(user)) return null;
        return userProfiles[user];
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
}
