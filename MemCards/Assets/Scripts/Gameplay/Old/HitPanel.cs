using UnityEngine;

public class HitPanel : MonoBehaviour
{
    [SerializeField]
    private Transform leftUp;

    [SerializeField]
    private Transform rightDown;

    public Vector3 GetRandomPoint()
    {
        return new Vector3(
            Random.Range(leftUp.position.x, rightDown.position.x),
            Random.Range(leftUp.position.y, rightDown.position.y),
            Random.Range(leftUp.position.z, rightDown.position.z)
        );
    }
}
