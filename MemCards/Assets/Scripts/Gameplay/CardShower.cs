using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct CardImage
{
    public CardType CardType;
    public Image Card;
}

public class CardShower : MonoBehaviour
{
    [SerializeField]
    private List<CardImage> cardImages;

    [SerializeField]
    private Color defaultColor;

    [SerializeField]
    private Color transparentColor;

    [SerializeField]
    private float showTime;
    private float timer = 0;

    private Coroutine currentCoroutine;

    public void ShowCard(CardType CardType)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        foreach (CardImage image in cardImages)
        {
            image.Card.gameObject.SetActive(false);
            if (CardType == image.CardType)
            {
                currentCoroutine = StartCoroutine(WaitAndHide(image));
                image.Card.gameObject.SetActive(true);
            }
        }
    }

    public IEnumerator WaitAndHide(CardImage card)
    {
        timer = showTime;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            card.Card.color = Color.Lerp(defaultColor, transparentColor, 1 - (timer / showTime));
            yield return new WaitForEndOfFrame();
        }
        card.Card.gameObject.SetActive(false);
    }
}
