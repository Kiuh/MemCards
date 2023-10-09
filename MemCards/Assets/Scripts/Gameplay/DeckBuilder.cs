using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeckBuilder : MonoBehaviour
{
    [SerializeField]
    private Transform cardsParent;

    [SerializeField]
    private float shuffleAnimationTime;

    [SerializeField]
    private float moveToDeckAnimationTime;

    [SerializeField]
    private Vector3 bottomDeckPosition;

    [SerializeField]
    private float yDeckGrowth;

    public List<Card> CreateDeck(Card cardPrefab, int count)
    {
        List<Card> cards = new();
        Vector3 creatingPosition = bottomDeckPosition;
        for (int i = 0; i < count; i++)
        {
            cards.Add(
                Instantiate(
                    cardPrefab,
                    creatingPosition,
                    cardPrefab.transform.rotation,
                    cardsParent
                )
            );
            creatingPosition.y += yDeckGrowth;
        }
        return cards;
    }

    public void SetRandomCardValues(List<Material> materials, List<Card> cards)
    {
        List<(Material x, Guid)> pairs = materials.Select(x => (x, Guid.NewGuid())).ToList();
        (Material x, Guid) pair = pairs.GetRandom();
        for (int i = 0; i < cards.Count; i++)
        {
            if (i % 2 == 0)
            {
                pair = pairs.GetRandom();
            }
            cards[i].InitCard(pair.x, pair.Item2);
        }
    }

    public void PlaceCardsIntoDeck(List<Card> cards, float time, Action nextAction)
    {
        bool[] bools = new bool[cards.Count * 2];
        int index = 0;
        Vector3 creatingPosition = bottomDeckPosition;
        foreach (Card card in cards)
        {
            bools[index] = false;
            int buffer = index;
            card.MoveCardToPosition(creatingPosition, time, () => bools[buffer] = true);
            index++;
            bools[index] = false;
            int buffer1 = index;
            card.HideCard(() => bools[buffer1] = true);
            index++;
            creatingPosition.y += yDeckGrowth;
        }
        _ = StartCoroutine(WaitForAll(bools, nextAction));
    }

    private class CardTaken
    {
        public Card Card;
        public bool Taken;
    }

    public void ShuffleDeck(List<Card> cards, Action nextAction)
    {
        bool[] bools = new bool[cards.Count];
        int index = 0;
        List<CardTaken> pairs = cards
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
                shuffleAnimationTime,
                () => bools[buffer1] = true
            );
            index++;
            bools[index] = false;
            int buffer2 = index;
            card2.Card.SpinAndMove(
                card1.Card.transform.position,
                shuffleAnimationTime,
                () => bools[buffer2] = true
            );
            index++;
        }
        _ = StartCoroutine(WaitForAll(bools, nextAction));
    }

    public void PlaceDeckToTable(List<Card> cards, List<Vector3> positions, Action nextAction)
    {
        List<Card> localCards = cards.ToList();
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
            Card card = localCards.TakeRandom();
            Vector3 position = localPositions.TakeRandom();
            card.MoveCardToPosition(position, moveToDeckAnimationTime, () => bools[buffer] = true);
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
