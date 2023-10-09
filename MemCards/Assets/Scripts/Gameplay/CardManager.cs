using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField]
    private Card cardPrefab;

    [SerializeField]
    private Vector2Int matrixSize;

    [SerializeField]
    private Vector2 shift;

    [SerializeField]
    private Vector3 startPoint;

    [SerializeField]
    private DeckBuilder deckBuilder;

    [SerializeField]
    private HitPanel hitPanel;

    [SerializeField]
    private float waitingTime;

    [SerializeField]
    private List<Material> cardMaterials = new();

    private Card clickedCard = null;
    private List<Card> cards = new();
    private readonly List<Vector3> cardsPositions = new();
    private bool lockInteraction = true;

    private void Awake()
    {
        FillCardsPositions();
        if (matrixSize.x * matrixSize.y % 2 != 0)
        {
            Debug.LogError("Require even matrix!");
        }
        cards = deckBuilder.CreateDeck(cardPrefab, matrixSize.x * matrixSize.y);
        deckBuilder.SetRandomCardValues(cardMaterials, cards);
        BeginningGame();
    }

    public void BeginningGame()
    {
        deckBuilder.ShuffleDeck(
            cards,
            () =>
                deckBuilder.PlaceDeckToTable(
                    cards,
                    cardsPositions,
                    () =>
                        ShowAllCards(
                            cards,
                            () =>
                            {
                                lockInteraction = false;
                                cards.ForEach(x => x.SetCardState(CardState.Hidden));
                            }
                        )
                )
        );
    }

    private void ShowAllCards(List<Card> cards, Action nextAction)
    {
        foreach (Card card in cards)
        {
            card.ShowCard(() => _ = StartCoroutine(Wait(() => card.HideCard(nextAction))));
        }
    }

    private IEnumerator Wait(Action nextAction)
    {
        yield return new WaitForSecondsRealtime(waitingTime);
        nextAction?.Invoke();
    }

    private void FillCardsPositions()
    {
        Vector3 creatingPosition = startPoint;
        for (int i = 0; i < matrixSize.x; i++)
        {
            for (int j = 0; j < matrixSize.y; j++)
            {
                cardsPositions.Add(creatingPosition);
                creatingPosition.z += shift.y;
            }
            creatingPosition.x += shift.x;
            creatingPosition.z = startPoint.z;
        }
    }

    private void Update()
    {
        if (!lockInteraction && (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            Card hitCard = hits.Select(x => x.collider.GetComponent<Card>())
                .FirstOrDefault(x => x != null);
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
                    Card buffer = clickedCard;
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
    }

    private bool CompareCard(Card firstCard, Card secondCard)
    {
        return firstCard.Guid == secondCard.Guid;
    }

    [SerializeField]
    private float liftY;

    [SerializeField]
    private float liftTime;

    [SerializeField]
    private float flyTime;

    [SerializeField]
    private float spinSpeed;

    private void ThrowCards(Card firstCard, Card secondCard)
    {
        Card buffer1 = firstCard;
        Card buffer2 = secondCard;
        firstCard.SetCardState(CardState.Transition);
        secondCard.SetCardState(CardState.Transition);
        firstCard.RiseAndThrow(
            liftY,
            hitPanel.GetRandomPoint(),
            liftTime,
            flyTime,
            spinSpeed,
            () => AfterThrow(buffer1)
        );
        secondCard.RiseAndThrow(
            liftY,
            hitPanel.GetRandomPoint(),
            liftTime,
            flyTime,
            spinSpeed,
            () => AfterThrow(buffer2)
        );
    }

    [SerializeField]
    private float returningBackTime;

    public void AfterThrow(Card card)
    {
        card.SetCardState(CardState.Thrown);
        if (cards.All(x => x.State == CardState.Thrown))
        {
            lockInteraction = true;
            deckBuilder.PlaceCardsIntoDeck(
                cards,
                returningBackTime,
                () =>
                {
                    deckBuilder.SetRandomCardValues(cardMaterials, cards);
                    BeginningGame();
                }
            );
        }
    }
}
