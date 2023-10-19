using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;


[Serializable]
public struct DeckConfig : INetworkSerializable
{
    public Transform StartDeckPosition;
    public Vector3 StartDeckPositionVector;
    public Vector3 StartRotationModifier;
    public float ShuffleAnimationTime;
    public float MoveToDeckAnimationTime;
    public float YDeckGrowth;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref ShuffleAnimationTime);
        serializer.SerializeValue(ref MoveToDeckAnimationTime);
        serializer.SerializeValue(ref YDeckGrowth);
        serializer.SerializeValue(ref StartDeckPositionVector);
        serializer.SerializeValue(ref StartRotationModifier);
    }
}

public class Deck : NetworkBehaviour
{
    [SerializeField]
    private PlayingCard cardPrefab;

    [SerializeField]
    private int initDeckCount;

    [SerializeField]
    private NetworkVariable<DeckConfig> deckConfig = new();
    public DeckConfig DeckConfig
    {
        get => deckConfig.Value;
        set => deckConfig.Value = value;
    }

    [SerializeField]
    private List<PlayingCard> playingCards = new();
    public List<PlayingCard> PlayingCards => playingCards;

    private int lastPlacedIndex = 0;

    public void SpawnDeck(ulong OwnerId)
    {
        Vector3 creatingPosition = DeckConfig.StartDeckPositionVector;
        for (int i = 0; i < initDeckCount; i++)
        {
            playingCards.Add(
                Instantiate(
                    cardPrefab,
                    creatingPosition,
                    Quaternion.Euler(
                        cardPrefab.transform.rotation.eulerAngles + DeckConfig.StartRotationModifier
                    )
                )
            );
            playingCards.Last().GetComponent<NetworkObject>().SpawnWithOwnership(OwnerId);
            creatingPosition.y += DeckConfig.YDeckGrowth;
        }
    }

    public void FillCardsPositions()
    {
        _ = StartCoroutine(WaitForData());
    }

    private IEnumerator WaitForData()
    {
        while (DeckConfig.StartDeckPositionVector == Vector3.zero)
        {
            yield return new WaitForEndOfFrame();
        }
        Vector3 creatingPosition = DeckConfig.StartDeckPositionVector;
        for (int i = 0; i < initDeckCount; i++)
        {
            cardsPositions.Add(creatingPosition);
            creatingPosition.y += DeckConfig.YDeckGrowth;
        }
    }

    public void CollectOwnedCards()
    {
        playingCards.Clear();
        foreach (PlayingCard item in FindObjectsOfType<PlayingCard>().Where(x => x.IsOwner))
        {
            playingCards.Add(item);
        }
    }

    private struct CardTypeWithHash
    {
        public CardType CardType;
        public string Hash;
    }

    public void SetRandomCardValues()
    {
        IEnumerable<CardType> types = Enumerable.Range(0, 6).Select(x => (CardType)x);
        List<CardTypeWithHash> pairs = types
            .Select(x => new CardTypeWithHash() { CardType = x, Hash = Guid.NewGuid().ToString() })
            .ToList();
        List<CardTypeWithHash> randomCards = GenerateRandomizedDeck(playingCards.Count)
            .Select(x => pairs.Find(x1 => x1.CardType == x))
            .ToList();
        for (int i = 0; i < playingCards.Count; i++)
        {
            playingCards[i].InitCard(randomCards[i].CardType, randomCards[i].Hash);
        }
    }

    private List<CardType> GenerateRandomizedDeck(int cardsCount)
    {
        // TODO: implement
        List<CardType> cardList = new List<CardType>();
        var enumSize = Enum.GetNames(typeof(CardType)).Length;
        for (int i = 0 ; i < enumSize; i++) 
        {
            cardList.Add((CardType)i);
            cardList.Add((CardType)i);
        }
        for (int i = 0 ; i < cardsCount - enumSize; i++) 
        {
            cardList.Add((CardType)UnityEngine.Random.Range(0,enumSize-1));
            cardList.Add(cardList[cardList.Count - 1]);
        }
        return cardList;
    }

    private List<Vector3> cardsPositions = new();

    public void PlaceCardsIntoDeck(Vector3 hiddenAngles, float time, Action nextAction)
    {
        bool[] bools = new bool[playingCards.Count * 2];
        int index = 0;
        Vector3 creatingPosition = DeckConfig.StartDeckPositionVector;

        foreach (PlayingCard card in playingCards)
        {
            bools[index] = false;
            int buffer = index;
            card.MoveCardToPosition(creatingPosition, time, () => bools[buffer] = true);
            index++;
            bools[index] = false;
            int buffer1 = index;
            card.RotateCard(hiddenAngles, () => bools[buffer1] = true);
            index++;
            creatingPosition.y += DeckConfig.YDeckGrowth;
        }
        _ = StartCoroutine(WaitForAll(bools, nextAction));
    }

    private class CardTaken
    {
        public PlayingCard Card;
        public bool Taken;
    }

    public void ShuffleDeck(Action nextAction)
    {
        bool[] bools = new bool[playingCards.Count];
        int index = 0;
        List<CardTaken> pairs = playingCards
            .Select(x => new CardTaken() { Card = x, Taken = false })
            .ToList();
        while (!pairs.All(x => x.Taken))
        {
            CardTaken card1 = pairs.Where(x => !x.Taken).ToList().GetRandom();
            card1.Taken = true;
            CardTaken card2 = pairs.Where(x => !x.Taken).ToList().GetRandom();
            card2.Taken = true;
            bools[index] = false;
            int buffer1 = index;
            card1.Card.SpinAndMove(
                card2.Card.transform.position,
                DeckConfig.ShuffleAnimationTime,
                () => bools[buffer1] = true
            );
            index++;
            bools[index] = false;
            int buffer2 = index;
            card2.Card.SpinAndMove(
                card1.Card.transform.position,
                DeckConfig.ShuffleAnimationTime,
                () => bools[buffer2] = true
            );
            index++;
        }
        lastPlacedIndex = 0;
        _ = StartCoroutine(WaitForAll(bools, nextAction));
    }

    public void PlaceCardToDeck(PlayingCard card, float time, Action nextAction)
    {
        card.MoveCardToPosition(cardsPositions[lastPlacedIndex], time, nextAction);
        lastPlacedIndex++;
    }

    public void PlaceDeckToTable(List<Vector3> positions, Action nextAction)
    {
        List<PlayingCard> localCards = playingCards.ToList();
        List<Vector3> localPositions = positions.ToList();

        bool[] bools = new bool[localCards.Count];
        int index = 0;
        if (localCards.Count != localPositions.Count)
        {
            Debug.LogError(
                "Local cards: " + localCards.Count + "local Positions: " + localPositions.Count
            );
            throw new Exception();
        }
        while (localCards.Count > 0)
        {
            bools[index] = false;
            int buffer = index;
            PlayingCard card = localCards.TakeRandom();
            Vector3 position = localPositions.TakeRandom();
            card.MoveCardToPosition(
                position,
                DeckConfig.MoveToDeckAnimationTime,
                () => bools[buffer] = true
            );
            index++;
        }
        _ = StartCoroutine(WaitForAll(bools, nextAction));
    }

    private IEnumerator WaitForAll(bool[] bools, Action nextAction)
    {
        while (!bools.All(x => x))
        {
            yield return new WaitForEndOfFrame();
        }
        nextAction?.Invoke();
    }
}
