using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public struct PlayerConfig
{
    public float LiftY;
    public float LiftTime;
    public float FlyTime;
    public float SpinSpeed;
    public float ReturningBackTime;
    public float WaitingTime;
}

public class Player : NetworkBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera virtualCamera;

    [SerializeField]
    private NetworkVariable<float> maxHealthPoints;
    public float MaxHealthPoints => maxHealthPoints.Value;
    private NetworkVariable<float> healthPoints =
        new(0, writePerm: NetworkVariableWritePermission.Owner);
    public float HealthPoints
    {
        get => healthPoints.Value;
        set => healthPoints.Value = value;
    }

    [SerializeField]
    private NetworkVariable<float> maxShieldPoints;
    public float MaxShieldPoints => maxShieldPoints.Value;
    private NetworkVariable<float> shieldPoints =
        new(0, writePerm: NetworkVariableWritePermission.Owner);

    public float ShieldPoints
    {
        get => shieldPoints.Value;
        set => shieldPoints.Value = value;
    }

    [SerializeField]
    private NetworkVariable<float> shieldDecreasing;
    private bool lockInteraction = true;

    [SerializeField]
    private Table table;
    public Table Table => table;

    [SerializeField]
    private Deck deck;
    public Deck Deck => deck;

    private Player enemyPlayer;
    public Player EnemyPlayer;

    private PlayingCard clickedCard = null;

    [SerializeField]
    private PlayerConfig playerConfig;

    public void SetLockInteraction(bool value)
    {
        lockInteraction = value;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
        {
            virtualCamera.gameObject.SetActive(false);
        }
        else
        {
            PreparePlayerToGame();
            FindObjectOfType<PlayerView>().Player = this;
        }
        base.OnNetworkSpawn();
    }

    public void PreparePlayerToGame()
    {
        healthPoints.Value = MaxHealthPoints;
        shieldPoints.Value = 0;
    }

    public void Suicide()
    {
        healthPoints.Value = 0;
    }

    private void Update()
    {
        if (IsLocalPlayer)
        {
            if (ShieldPoints > 0)
            {
                ShieldPoints -= Time.deltaTime * shieldDecreasing.Value;
            }
            else
            {
                ShieldPoints = 0;
            }
            if (!lockInteraction)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit[] hits = Physics.RaycastAll(ray);
                    PlayingCard hitCard = hits.Select(x => x.collider.GetComponent<PlayingCard>())
                        .FirstOrDefault(x => x != null && x.IsOwner);
                    if (
                        hitCard != null
                        && hitCard.State != CardState.Thrown
                        && hitCard.State != CardState.Transition
                    )
                    {
                        if (clickedCard == null)
                        {
                            if (hitCard.State == CardState.Hidden)
                            {
                                clickedCard = hitCard;
                                hitCard.SetCardState(CardState.Transition);
                                hitCard.ShowCard(() => hitCard.SetCardState(CardState.Shown));
                            }
                        }
                        else if (clickedCard != hitCard && hitCard.State == CardState.Hidden)
                        {
                            PlayingCard buffer = clickedCard;
                            hitCard.SetCardState(CardState.Transition);
                            buffer.SetCardState(CardState.Transition);
                            if (CompareCard(hitCard, clickedCard))
                            {
                                hitCard.ShowCard(() => ThrowCards(hitCard, buffer));
                            }
                            else
                            {
                                hitCard.ShowCard(() =>
                                {
                                    hitCard.HideCard(() => hitCard.SetCardState(CardState.Hidden));
                                    buffer.HideCard(() => buffer.SetCardState(CardState.Hidden));
                                });
                            }
                            clickedCard = null;
                        }
                    }
                }
                if (healthPoints.Value <= 0)
                {
                    lockInteraction = true;
                    GameController.Singleton.PlayerLoseServerRpc(IsHost ? "Player1" : "Player2");
                }
            }
        }
    }

    private bool CompareCard(PlayingCard firstCard, PlayingCard secondCard)
    {
        return firstCard.Guid == secondCard.Guid;
    }

    private void ThrowCards(PlayingCard firstCard, PlayingCard secondCard)
    {
        PlayingCard buffer1 = firstCard;
        PlayingCard buffer2 = secondCard;
        firstCard.SetCardState(CardState.Transition);
        secondCard.SetCardState(CardState.Transition);
        firstCard.RiseAndThrow(
            playerConfig.LiftY,
            enemyPlayer.transform.position,
            playerConfig.LiftTime,
            playerConfig.FlyTime,
            playerConfig.SpinSpeed,
            () => AfterThrow(buffer1)
        );
        secondCard.RiseAndThrow(
            playerConfig.LiftY,
            enemyPlayer.transform.position,
            playerConfig.LiftTime,
            playerConfig.FlyTime,
            playerConfig.SpinSpeed,
            () => AfterThrow(buffer2)
        );
    }

    public void AfterThrow(PlayingCard card)
    {
        card.SetCardState(CardState.Thrown);
        if (deck.PlayingCards.All(x => x.State == CardState.Thrown))
        {
            lockInteraction = true;
            deck.PlaceCardsIntoDeck(
                playerConfig.ReturningBackTime,
                () =>
                {
                    deck.SetRandomCardValues();
                    BeginningGame();
                }
            );
        }
    }

    private void ShowAllCards(List<PlayingCard> cards, Action nextAction)
    {
        foreach (PlayingCard card in cards)
        {
            card.ShowCard(() => _ = StartCoroutine(Wait(() => card.HideCard(nextAction))));
        }
    }

    private IEnumerator Wait(Action nextAction)
    {
        yield return new WaitForSecondsRealtime(playerConfig.WaitingTime);
        nextAction?.Invoke();
    }

    public void BeginningGame()
    {
        deck.ShuffleDeck(
            () =>
                deck.PlaceDeckToTable(
                    table.CardsPositions,
                    () =>
                        ShowAllCards(
                            deck.PlayingCards,
                            () =>
                            {
                                lockInteraction = false;
                                deck.PlayingCards.ForEach(x => x.SetCardState(CardState.Hidden));
                            }
                        )
                )
        );
    }
}
