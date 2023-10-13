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
    private DeckConfig deckInitInfo;
    public DeckConfig DeckInitInfo => deckInitInfo;

    [SerializeField]
    private TableConfig tableInitInfo;
    public TableConfig TableInitInfo => tableInitInfo;
}
