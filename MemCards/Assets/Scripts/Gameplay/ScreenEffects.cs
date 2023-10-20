using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ScreenColor
{
    Green,
    Orange,
    Yellow
}

[Serializable]
public struct EffectConfig
{
    public Image Image;
    public ScreenColor Color;

    [HideInInspector]
    public Coroutine Coroutine;
}

public class ScreenEffects : MonoBehaviour
{
    [SerializeField]
    private List<EffectConfig> pairs;

    [SerializeField]
    private float enterTime = 1;

    [SerializeField]
    private float exitTime = 1;

    [SerializeField]
    private float maxCapacity = 0.5f;

    public void ShowImage(ScreenColor screenColor)
    {
        EffectConfig effect = pairs.Find(x => x.Color == screenColor);
        if (effect.Coroutine != null)
        {
            StopCoroutine(effect.Coroutine);
        }
        effect.Coroutine = StartCoroutine(ShowScreen(effect));
    }

    private IEnumerator ShowScreen(EffectConfig effect)
    {
        float timer = enterTime;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            effect.Image.color = new Color(
                effect.Image.color.r,
                effect.Image.color.g,
                effect.Image.color.b,
                (1 - (timer / enterTime)) * maxCapacity
            );
            yield return new WaitForEndOfFrame();
        }
        effect.Coroutine = StartCoroutine(HideScreen(effect));
    }

    private IEnumerator HideScreen(EffectConfig effect)
    {
        float timer = exitTime;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            effect.Image.color = new Color(
                effect.Image.color.r,
                effect.Image.color.g,
                effect.Image.color.b,
                timer / enterTime * maxCapacity
            );
            yield return new WaitForEndOfFrame();
        }
        effect.Image.color = new Color(
            effect.Image.color.r,
            effect.Image.color.g,
            effect.Image.color.b,
            0
        );
        effect.Coroutine = null;
    }
}
