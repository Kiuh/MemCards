using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct PlayerConfig
{
    public float LiftY;
    public float LiftTime;
    public float FlyTime;
    public float SpinSpeed;
    public float ReturningBackTime;
    public float WaitingTime;
}

[Serializable]
public struct CardsConfig
{
    [Header("Attack")]
    public float AttackDamage;

    [Header("Poison")]
    public float TickDamage;
    public float TickInterval;
    public float PoisonApplying;
    public float PoisonDecreasing;

    [Header("Freeze")]
    public float FreezeTime;
    public float FreezeDecreasing;

    [Header("Shield")]
    public float ShieldAmount;
    public float ShieldDecreasing;
    public float ShieldMaxAmount;
    public float StartShieldPoints;

    [Header("Heal")]
    public float HealAmount;
    public float MaxHealthPoints;
    public float StartHealthPoints;

    [Header("Evade")]
    public int MaxEvadeCount;
}

[Serializable]
public struct DynamicPlayerConfig : INetworkSerializable
{
    public Vector3 HiddenAngels;
    public Vector3 ShownAngels;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref HiddenAngels);
        serializer.SerializeValue(ref ShownAngels);
    }
}

public class Player : NetworkBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private CardShower shower;

    [SerializeField]
    private CinemachineVirtualCamera virtualCamera;

    [SerializeField]
    private CardsConfig cardsConfig;
    public CardsConfig CardsConfig => cardsConfig;

    private NetworkVariable<float> healthPoints = new(0);
    public float HealthPoints => healthPoints.Value;

    [ServerRpc(RequireOwnership = false)]
    public void SetHealthPointsServerRpc(float value)
    {
        healthPoints.Value = value;
    }

    [SerializeField]
    private NetworkVariable<bool> evadeCoin = new(false);
    public bool EvadeCoin => evadeCoin.Value;

    [ServerRpc(RequireOwnership = false)]
    public void SetEvadeCoinServerRpc(bool value)
    {
        evadeCoin.Value = value;
    }

    private NetworkVariable<int> fullMatches = new(0);
    public int FullMatches => fullMatches.Value;

    [ServerRpc(RequireOwnership = false)]
    public void SetFullMatchesServerRpc(int value)
    {
        fullMatches.Value = value;
    }

    private NetworkVariable<float> shieldPoints = new(0);
    public float ShieldPoints => shieldPoints.Value;

    [ServerRpc(RequireOwnership = false)]
    public void SetShieldPointsServerRpc(float value)
    {
        shieldPoints.Value = value;
    }

    private NetworkVariable<float> freezeTime = new(0);
    public float FreezeTime => freezeTime.Value;

    [ServerRpc(RequireOwnership = false)]
    public void SetFreezeTimeServerRpc(float value)
    {
        freezeTime.Value = value;
    }

    private NetworkVariable<float> poisonTime = new(0);
    private NetworkVariable<float> poisonTick = new(0);
    public float PoisonTime => poisonTime.Value;
    public float PoisonTick => poisonTick.Value;

    [ServerRpc(RequireOwnership = false)]
    public void SetPoisonTimeServerRpc(float value)
    {
        poisonTime.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPoisonTickServerRpc(float value)
    {
        poisonTick.Value = value;
    }

    private bool lockInteraction = true;
    private bool lockDeath = true;

    [SerializeField]
    private Table table;
    public Table Table => table;

    [SerializeField]
    private NetworkVariable<DynamicPlayerConfig> dynamicConfig = new();
    public DynamicPlayerConfig DynamicPlayerConfig
    {
        get => dynamicConfig.Value;
        set => dynamicConfig.Value = value;
    }

    [SerializeField]
    private Deck deck;
    public Deck Deck => deck;

    public Player EnemyPlayer;

    private PlayingCard clickedCard = null;

    [SerializeField]
    private PlayerConfig playerConfig;

    public void SetLockInteraction(bool value)
    {
        lockInteraction = value;
        lockDeath = value;
    }

    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            PreparePlayerToGameServerRpc();
            FindObjectOfType<PlayerView>().Player = this;
            shower = FindObjectOfType<CardShower>();
        }
        else
        {
            virtualCamera.gameObject.SetActive(false);
        }
        base.OnNetworkSpawn();
    }

    [ServerRpc]
    public void PreparePlayerToGameServerRpc()
    {
        healthPoints.Value = CardsConfig.StartHealthPoints;
        shieldPoints.Value = CardsConfig.StartShieldPoints;
        fullMatches.Value = 0;
    }

    public void Suicide()
    {
        healthPoints.Value = 0;
    }

    [ClientRpc]
    private void SetLockInteractionClientRpc(bool value)
    {
        lockInteraction = value;
    }

    private void Update()
    {
        if (IsHost)
        {
            if (ShieldPoints > 0)
            {
                shieldPoints.Value -= Time.deltaTime * CardsConfig.ShieldDecreasing;
            }
            else if (ShieldPoints != 0)
            {
                shieldPoints.Value = 0;
            }
            if (FreezeTime > 0)
            {
                freezeTime.Value -= Time.deltaTime * CardsConfig.FreezeDecreasing;
                SetLockInteractionClientRpc(true);
            }
            else if (FreezeTime != 0)
            {
                freezeTime.Value = 0;
                SetLockInteractionClientRpc(false);
            }
            if (PoisonTime > 0)
            {
                poisonTime.Value -= Time.deltaTime * CardsConfig.PoisonDecreasing;
                poisonTick.Value -= Time.deltaTime * CardsConfig.PoisonDecreasing;
                if (poisonTick.Value <= 0)
                {
                    if (EvadeCoin)
                    {
                        evadeCoin.Value = false;
                    }
                    else
                    {
                        healthPoints.Value -= CardsConfig.TickDamage;
                        PlayAnimation(CardType.Poison);
                    }
                    poisonTick.Value = CardsConfig.TickInterval;
                }
            }
            else if (FreezeTime != 0)
            {
                poisonTime.Value = 0;
            }
        }
        if (IsLocalPlayer)
        {
            if (!lockInteraction)
            {
                if (Input.GetMouseButtonDown(0))
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
                                hitCard.RotateCard(
                                    DynamicPlayerConfig.ShownAngels,
                                    () => hitCard.SetCardState(CardState.Shown)
                                );
                            }
                        }
                        else if (clickedCard != hitCard && hitCard.State == CardState.Hidden)
                        {
                            PlayingCard buffer = clickedCard;
                            hitCard.SetCardState(CardState.Transition);
                            buffer.SetCardState(CardState.Transition);
                            if (CompareCard(hitCard, clickedCard))
                            {
                                hitCard.RotateCard(
                                    DynamicPlayerConfig.ShownAngels,
                                    () => ThrowCards(hitCard, buffer)
                                );
                            }
                            else
                            {
                                lockInteraction = true;
                                hitCard.RotateCard(
                                    DynamicPlayerConfig.ShownAngels,
                                    () =>
                                    {
                                        hitCard.RotateCard(
                                            DynamicPlayerConfig.HiddenAngels,
                                            () => hitCard.SetCardState(CardState.Hidden)
                                        );
                                        buffer.RotateCard(
                                            DynamicPlayerConfig.HiddenAngels,
                                            () =>
                                            {
                                                buffer.SetCardState(CardState.Hidden);
                                                lockInteraction = false;
                                            }
                                        );
                                    }
                                );
                            }
                            clickedCard = null;
                        }
                    }
                }
            }
            if (!lockDeath)
            {
                if (healthPoints.Value <= 0)
                {
                    lockDeath = true;
                    GameController.Singleton.PlayerLoseServerRpc(IsHost ? "Player2" : "Player1");
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
        shower.ShowCard(buffer1.CardType);
        (bool FirstCard, bool SecondCard, CardType CardType) ward = (
            false,
            false,
            buffer1.CardType
        );
        firstCard.SetCardState(CardState.Transition);
        secondCard.SetCardState(CardState.Transition);
        firstCard.Rise(
            playerConfig.LiftY,
            playerConfig.LiftTime,
            playerConfig.SpinSpeed,
            () =>
            {
                deck.PlaceCardToDeck(buffer1, playerConfig.FlyTime, () => AfterThrow(buffer1));
                ward.FirstCard = true;
            }
        );
        secondCard.Rise(
            playerConfig.LiftY,
            playerConfig.LiftTime,
            playerConfig.SpinSpeed,
            () =>
            {
                deck.PlaceCardToDeck(buffer2, playerConfig.FlyTime, () => AfterThrow(buffer2));
                ward.SecondCard = true;
            }
        );
        _ = StartCoroutine(ExecuteEffect(ward));
    }

    private IEnumerator ExecuteEffect((bool FirstCard, bool SecondCard, CardType CardType) ward)
    {
        while (ward.FirstCard || ward.SecondCard)
        {
            yield return new WaitForEndOfFrame();
        }
        ApplyEffectServerRpc(ward.CardType, OwnerClientId, EnemyPlayer.OwnerClientId);
    }

    public void PlayAnimation(CardType CardType)
    {
        animator.Play(
            CardType switch
            {
                CardType.Attack => "GetHit",
                CardType.Freeze => "Dizzy",
                CardType.Poison => "GetHit",
                _ => "IdleNormal"
            }
        );
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyEffectServerRpc(CardType CardType, ulong playerId, ulong enemyID)
    {
        Player[] players = FindObjectsOfType<Player>();
        Player player = players.FirstOrDefault(x => x.OwnerClientId == playerId);
        Player enemy = players.FirstOrDefault(x => x.OwnerClientId == enemyID);
        if (CardType == CardType.Attack)
        {
            if (enemy.EvadeCoin)
            {
                enemy.SetEvadeCoinServerRpc(false);
            }
            else
            {
                enemy.SetHealthPointsServerRpc(
                    enemy.HealthPoints - Math.Max(0, CardsConfig.AttackDamage - enemy.ShieldPoints)
                );
                enemy.PlayAnimation(CardType);
            }
        }
        else if (CardType == CardType.Freeze)
        {
            // Freeze
            enemy.SetFreezeTimeServerRpc(enemy.FreezeTime + CardsConfig.FreezeTime);
            enemy.PlayAnimation(CardType);
        }
        else if (CardType == CardType.Poison)
        {
            // Poison
            enemy.SetPoisonTimeServerRpc(enemy.PoisonTime + CardsConfig.PoisonApplying);
            enemy.PlayAnimation(CardType);
        }
        else if (CardType == CardType.Shield)
        {
            player.SetShieldPointsServerRpc(
                Math.Min(
                    Math.Max(player.ShieldPoints + cardsConfig.ShieldAmount, player.ShieldPoints),
                    cardsConfig.ShieldMaxAmount
                )
            );
            player.PlayAnimation(CardType);
        }
        else if (CardType == CardType.Heal)
        {
            player.SetHealthPointsServerRpc(
                Math.Min(player.HealthPoints + cardsConfig.HealAmount, cardsConfig.MaxHealthPoints)
            );

            player.PlayAnimation(CardType);
        }
        else if (CardType == CardType.Evade)
        {
            player.SetEvadeCoinServerRpc(true);
            player.PlayAnimation(CardType);
        }
    }

    public void AfterThrow(PlayingCard card)
    {
        card.SetCardState(CardState.Thrown);
        if (deck.PlayingCards.All(x => x.State == CardState.Thrown))
        {
            lockInteraction = true;
            deck.PlaceCardsIntoDeck(
                DynamicPlayerConfig.HiddenAngels,
                playerConfig.ReturningBackTime,
                () =>
                {
                    SetRandomCardValuesServerRpc();
                    BeginningGame();
                    SetFullMatchesServerRpc(FullMatches + 1);
                }
            );
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetRandomCardValuesServerRpc()
    {
        deck.SetRandomCardValues();
    }

    private void ShowAllCards(List<PlayingCard> cards, Action nextAction)
    {
        foreach (PlayingCard card in cards)
        {
            card.RotateCard(
                DynamicPlayerConfig.ShownAngels,
                () =>
                    _ = StartCoroutine(
                        Wait(() => card.RotateCard(DynamicPlayerConfig.HiddenAngels, nextAction))
                    )
            );
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
