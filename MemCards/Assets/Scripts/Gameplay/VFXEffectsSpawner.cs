using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EffectWithCardType
{
    public CardType CardType;
    public GameObject EffectPrefab;
    public float Duration;
}

public class VFXEffectsSpawner : MonoBehaviour
{
    [SerializeField]
    private List<EffectWithCardType> effects;

    public void SpawnEffect(CardType cardType)
    {
        EffectWithCardType effect = effects.Find(x => x.CardType == cardType);
        GameObject newEffect = Instantiate(effect.EffectPrefab, transform);
        Destroy(newEffect, effect.Duration);
    }
}
