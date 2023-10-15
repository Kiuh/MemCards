using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum CardType
{
    Shield,
    Heal,
    Evade,
    Attack,
    Freeze,
    Poison
}

public enum AffectType
{
    Player,
    Enemy
}

[Serializable]
public struct TypeView
{
    public CardType CardType;
    public GameObject Model;
}

public enum CardState
{
    Hidden,
    Shown,
    Thrown,
    Transition
}

public class PlayingCard : NetworkBehaviour
{
    [SerializeField]
    private float rotationTime;

    [InspectorReadOnly]
    private float rotationTimer = 0;

    [SerializeField]
    private AnimationCurve curve;

    [SerializeField]
    private List<TypeView> cardViews;

    [InspectorReadOnly]
    private readonly CardState nextState;
    public CardState State { get; private set; } = CardState.Transition;

    [SerializeField]
    private NetworkVariable<CardType> cardType;

    public string Guid { get; set; }
    public CardType CardType
    {
        get => cardType.Value;
        set => cardType.Value = value;
    }

    public void SetCardState(CardState state)
    {
        State = state;
    }

    public void InitCard(CardType cardType, string guid)
    {
        this.cardType.Value = cardType;
        SetCardTypeClientRpc(cardType, guid);
    }

    [ClientRpc]
    private void SetCardTypeClientRpc(CardType newType, string guid)
    {
        Guid = guid;
        foreach (TypeView view in cardViews)
        {
            view.Model.SetActive(view.CardType == newType);
        }
    }

    public void RotateCard(Vector3 finalAngles, Action nextAction)
    {
        rotationTimer = rotationTime;
        _ = StartCoroutine(RotateAnimation(finalAngles, nextAction));
    }

    private IEnumerator RotateAnimation(Vector3 finalAngles, Action nextAction)
    {
        Vector3 startDegrees = transform.rotation.eulerAngles;
        while (rotationTimer > 0)
        {
            rotationTimer -= Time.deltaTime;
            transform.rotation = Quaternion.Euler(
                Vector3.Lerp(startDegrees, finalAngles, 1 - (rotationTimer / rotationTime))
            );
            yield return new WaitForEndOfFrame();
        }
        transform.rotation = Quaternion.Euler(finalAngles);
        State = nextState;
        nextAction?.Invoke();
    }

    public void SpinAndMove(Vector3 position, float animationTime, Action nextAction)
    {
        _ = StartCoroutine(RotateAround(animationTime, Vector3.up, () => { }));
        _ = StartCoroutine(Move(animationTime, position, nextAction));
    }

    public void MoveCardToPosition(Vector3 position, float animationTime, Action nextAction)
    {
        _ = StartCoroutine(Move(animationTime, position, nextAction));
    }

    public IEnumerator RotateAround(float fullTime, Vector3 axis, Action nextAction)
    {
        fullTime = UnityEngine.Random.Range(fullTime / 2, fullTime);
        bool rotation = UnityEngine.Random.value > 0.5f;
        float time = 0;
        Quaternion startRotation = transform.rotation;
        while (time < fullTime)
        {
            time += Time.deltaTime;
            transform.Rotate(axis, Time.deltaTime / fullTime * 180 * (rotation ? 1 : -1));
            yield return new WaitForEndOfFrame();
        }
        transform.rotation = startRotation;
        nextAction?.Invoke();
    }

    public IEnumerator Move(float fullTime, Vector3 destination, Action nextAction)
    {
        float time = 0;
        while (time < fullTime)
        {
            time += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, destination, time / fullTime);
            yield return new WaitForEndOfFrame();
        }
        transform.position = destination;
        nextAction?.Invoke();
    }

    private bool stopper = true;

    public void Rise(float liftY, float riseTime, float spinSpeed, Action nextAction)
    {
        stopper = false;
        _ = StartCoroutine(Spin(spinSpeed));
        _ = StartCoroutine(
            Move(
                riseTime,
                transform.position + (Vector3.up * liftY),
                () =>
                {
                    stopper = true;
                    nextAction?.Invoke();
                }
            )
        );
    }

    public IEnumerator Spin(float spinSpeed)
    {
        Quaternion prevRotation = transform.rotation;
        while (!stopper)
        {
            transform.Rotate(UnityEngine.Random.onUnitSphere * spinSpeed);
            yield return new WaitForEndOfFrame();
        }
        transform.rotation = prevRotation;
    }
}
