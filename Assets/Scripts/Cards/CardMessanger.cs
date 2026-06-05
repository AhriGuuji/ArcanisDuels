using Unity.Netcode;

public struct CardMessanger : INetworkSerializable
{
    public int CardPrefabId; 
    public int PositionInHand; 
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CardPrefabId);
        serializer.SerializeValue(ref PositionInHand);
    }
}