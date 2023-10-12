using TMPro;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text buffs;

    [SerializeField]
    private TMP_Text evasion;

    [SerializeField]
    private RectTransform healthPointsBack;

    [SerializeField]
    private RectTransform healthPointsFront;

    [SerializeField]
    private RectTransform shieldPointsBack;

    [SerializeField]
    private RectTransform shieldPointsFront;

    [Range(0, 1)]
    [SerializeField]
    private float lerpSpeed;

    private Player player;
    public Player Player
    {
        get => player;
        set => player = value;
    }

    private void Update()
    {
        if (player != null)
        {
            healthPointsFront.sizeDelta = Vector2.Lerp(
                healthPointsFront.sizeDelta,
                new Vector2(
                    healthPointsBack.sizeDelta.x * (player.HealthPoints / player.MaxHealthPoints),
                    healthPointsFront.sizeDelta.y
                ),
                lerpSpeed
            );
            shieldPointsFront.sizeDelta = Vector2.Lerp(
                shieldPointsFront.sizeDelta,
                new Vector2(
                    shieldPointsBack.sizeDelta.x * (player.ShieldPoints / player.MaxShieldPoints),
                    shieldPointsFront.sizeDelta.y
                ),
                lerpSpeed
            );
        }
    }
}
