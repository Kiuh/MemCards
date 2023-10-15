using Unity.Netcode;
using UnityEngine;

public class PlayerSlot : NetworkBehaviour
{
    private NetworkVariable<bool> isBusy = new(false);
    public bool IsBusy
    {
        get => isBusy.Value;
        set => isBusy.Value = value;
    }

    [SerializeField]
    private DynamicPlayerConfig dynamicPlayerConfig;
    public DynamicPlayerConfig DynamicPlayerConfig => dynamicPlayerConfig;

    [SerializeField]
    private DeckConfig deckConfig;
    public DeckConfig DeckConfig => deckConfig;

    [SerializeField]
    private TableConfig tableConfig;
    public TableConfig TableConfig => tableConfig;
}
