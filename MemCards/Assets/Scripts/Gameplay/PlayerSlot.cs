using Unity.Netcode;

public class PlayerSlot : NetworkBehaviour
{
    private NetworkVariable<bool> isBusy = new(false);
    public bool IsBusy
    {
        get => isBusy.Value;
        set => isBusy.Value = value;
    }
}
