using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct DeckConfig
{
    public Transform StartDeckPosition;
    public Vector3 StartRotationModifier;
    public float ShuffleAnimationTime;
    public float MoveToDeckAnimationTime;
    public float YDeckGrowth;
}

public class Deck : NetworkBehaviour
{
    [SerializeField]
    private PlayingCard cardPrefab;

    [SerializeField]
    private int initDeckCount;
    private DeckConfig deckConfig;
    private List<PlayingCard> playingCards = new();
    public List<PlayingCard> PlayingCards => playingCards;

    public void SpawnDeck(ulong OwnerId)
    {
        Vector3 creatingPosition = deckConfig.StartDeckPosition.position;
        for (int i = 0; i < initDeckCount; i++)
        {
            playingCards.Add(
                Instantiate(
                    cardPrefab,
                    creatingPosition,
                    Quaternion.Euler(
                        cardPrefab.transform.rotation.eulerAngles + deckConfig.StartRotationModifier
                    )
                )
            );
            playingCards.Last().GetComponent<NetworkObject>().SpawnWithOwnership(OwnerId);
            creatingPosition.y += deckConfig.YDeckGrowth;
        }
    }

    public void CollectOwnedCards()
    {
        playingCards.Clear();
        FindObjectsOfType<PlayingCard>()
            .Where(x => x.IsOwner)
            .ToList()
            .ForEach(x => playingCards.Add(x));
    }

    public void SetInitInfo(DeckConfig initInfo)
    {
        deckConfig = initInfo;
    }

    public void SetRandomCardValues()
    {
        IEnumerable<CardType> types = Enumerable.Range(0, 6).Select(x => (CardType)x);
        // TODO: Implement separating logics
        List<(CardType x, Guid)> pairs = types.Select(x => (x, Guid.NewGuid())).ToList();
        (CardType x, Guid) pair = pairs.GetRandom();
        for (int i = 0; i < playingCards.Count; i++)
        {
            if (i % 2 == 0)
            {
                pair = pairs.GetRandom();
            }
            playingCards[i].InitCard(pair.x, pair.Item2);
        }
    }

    public void PlaceCardsIntoDeck(float time, Action nextAction)
    {
        bool[] bools = new bool[playingCards.Count * 2];
        int index = 0;
        Vector3 creatingPosition = deckConfig.StartDeckPosition.position;
        foreach (PlayingCard card in playingCards)
        {
            bools[index] = false;
            int buffer = index;
            card.MoveCardToPosition(creatingPosition, time, () => bools[buffer] = true);
            index++;
            bools[index] = false;
            int buffer1 = index;
            card.HideCard(() => bools[buffer1] = true);
            index++;
            creatingPosition.y += deckConfig.YDeckGrowth;
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
                deckConfig.ShuffleAnimationTime,
                () => bools[buffer1] = true
            );
            index++;
            bools[index] = false;
            int buffer2 = index;
            card2.Card.SpinAndMove(
                card1.Card.transform.position,
                deckConfig.ShuffleAnimationTime,
                () => bools[buffer2] = true
            );
            index++;
        }
        _ = StartCoroutine(WaitForAll(bools, nextAction));
    }

    public void PlaceDeckToTable(List<Vector3> positions, Action nextAction)
    {
        List<PlayingCard> localCards = playingCards.ToList();
        List<Vector3> localPositions = positions.ToList();

        bool[] bools = new bool[localCards.Count];
        int index = 0;
        if (localCards.Count != localPositions.Count)
        {
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
                deckConfig.MoveToDeckAnimationTime,
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
