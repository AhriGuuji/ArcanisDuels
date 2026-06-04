using System.Collections.Generic;
using Unity.Mathematics;

public class PlayerInfo
{
    public ulong ClientID { get; private set; }
    public string CharacterName { get; private set; }
    public List<int> DeckIds { get; private set; }

    public PlayerInfo(ulong clientID, string name, List<int> deckIds)
    {
        ClientID = clientID;
        CharacterName = name;
        DeckIds = deckIds;
    }
}