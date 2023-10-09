using Common;
using System;
using System.Collections;
using UnityEngine;

public enum CardState
{
    Hidden,
    Shown,
    Thrown,
    Transition
}

[SelectionBase]
public class Card : MonoBehaviour
{
    [SerializeField]
    private float rotationTime;

    [InspectorReadOnly]
    private float rotationTimer = 0;

    [SerializeField]
    private AnimationCurve curve;

    [SerializeField]
    private Transform centerPoint;

    [SerializeField]
    private MeshRenderer meshRenderer;

    [InspectorReadOnly]
    private readonly CardState nextState;
    public CardState State { get; private set; } = CardState.Transition;
    public Guid Guid { get; set; }

    public void SetCardState(CardState state)
    {
        State = state;
    }

    public void InitCard(Material material, Guid guid)
    {
        Guid = guid;
        meshRenderer.material = material;
    }

    public void ShowCard(Action nextAction)
    {
        rotationTimer = rotationTime;
        _ = StartCoroutine(RotateAnimation(false, nextAction));
    }

    public void HideCard(Action nextAction)
    {
        rotationTimer = rotationTime;
        _ = StartCoroutine(RotateAnimation(true, nextAction));
    }

    private IEnumerator RotateAnimation(bool reverse, Action nextAction)
    {
        while (rotationTimer > 0)
        {
            rotationTimer -= Time.deltaTime;
            float degreePoint =
                (reverse ? (1 - (rotationTime - rotationTimer)) : (rotationTime - rotationTimer))
                / rotationTime;
            transform.rotation = Quaternion.Euler(
                new Vector3(
                    (reverse ? -360 : 0) + (curve.Evaluate(degreePoint) * 180),
                    transform.rotation.y,
                    transform.rotation.z
                )
            );
            yield return new WaitForEndOfFrame();
        }
        transform.rotation = Quaternion.Euler(
            new Vector3(
                curve.Evaluate(reverse ? 0 : 1) * 180,
                transform.rotation.y,
                transform.rotation.z
            )
        );
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

    public void RiseAndThrow(
        float liftY,
        Vector3 destination,
        float riseTime,
        float flyTime,
        float spinSpeed,
        Action nextAction
    )
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
                    _ = StartCoroutine(Move(flyTime, destination, nextAction));
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
