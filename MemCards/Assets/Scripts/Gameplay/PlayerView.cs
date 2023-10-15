using TMPro;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text playerName;

    [SerializeField]
    private TMP_Text fullMatches;

    [SerializeField]
    private TMP_Text buffs;

    [SerializeField]
    private TMP_Text buffsEnemy;

    [SerializeField]
    private RectTransform healthPointsBack;

    [SerializeField]
    private RectTransform healthPointsFront;

    [SerializeField]
    private RectTransform shieldPointsBack;

    [SerializeField]
    private RectTransform shieldPointsFront;

    [SerializeField]
    private RectTransform healthPointsBackEnemy;

    [SerializeField]
    private RectTransform healthPointsFrontEnemy;

    [SerializeField]
    private RectTransform shieldPointsBackEnemy;

    [SerializeField]
    private RectTransform shieldPointsFrontEnemy;

    [SerializeField]
    private TMP_Text healthPoints;

    [SerializeField]
    private TMP_Text shieldPoints;

    [SerializeField]
    private TMP_Text healthPointsEnemy;

    [SerializeField]
    private TMP_Text shieldPointsEnemy;

    [Range(0, 1)]
    [SerializeField]
    private float lerpSpeed;

    private Player player;
    public Player Player
    {
        get => player;
        set => player = value;
    }

    public void CallPlayerSuicide()
    {
        Player.Suicide();
    }

    private void Update()
    {
        if (player != null)
        {
            healthPoints.text = (
                (int)Mathf.Lerp(float.Parse(healthPoints.text), player.HealthPoints, lerpSpeed)
            ).ToString();
            shieldPoints.text = (
                (int)Mathf.Lerp(float.Parse(shieldPoints.text), player.ShieldPoints, lerpSpeed)
            ).ToString();

            fullMatches.text = $"Full matches: {player.FullMatches}";
            buffs.text = "";
            buffs.text += player.EvadeCoin ? "Have evade coin\n" : "Without evade\n";
            buffs.text +=
                player.FreezeTime > 0 ? $"Frizzed for {(int)player.FreezeTime}\n" : "NOT frizzed\n";
            buffs.text +=
                player.PoisonTime > 0 ? $"Poisonous for {(int)player.PoisonTime}" : "NOT poisoned";
            if (player.EnemyPlayer != null)
            {
                healthPointsEnemy.text = (
                    (int)
                        Mathf.Lerp(
                            float.Parse(healthPointsEnemy.text),
                            player.EnemyPlayer.HealthPoints,
                            lerpSpeed
                        )
                ).ToString();
                shieldPointsEnemy.text = (
                    (int)
                        Mathf.Lerp(
                            float.Parse(shieldPointsEnemy.text),
                            player.EnemyPlayer.ShieldPoints,
                            lerpSpeed
                        )
                ).ToString();

                fullMatches.text += $"\nEnemy: {player.EnemyPlayer.FullMatches}";
                healthPointsFrontEnemy.sizeDelta = Vector2.Lerp(
                    healthPointsFrontEnemy.sizeDelta,
                    new Vector2(
                        healthPointsBackEnemy.sizeDelta.x
                            * (
                                player.EnemyPlayer.HealthPoints
                                / player.EnemyPlayer.CardsConfig.MaxHealthPoints
                            ),
                        healthPointsFrontEnemy.sizeDelta.y
                    ),
                    lerpSpeed
                );
                shieldPointsFrontEnemy.sizeDelta = Vector2.Lerp(
                    shieldPointsFrontEnemy.sizeDelta,
                    new Vector2(
                        shieldPointsBackEnemy.sizeDelta.x
                            * (
                                player.EnemyPlayer.ShieldPoints
                                / player.EnemyPlayer.CardsConfig.ShieldMaxAmount
                            ),
                        shieldPointsFrontEnemy.sizeDelta.y
                    ),
                    lerpSpeed
                );
                buffsEnemy.text = "";
                buffsEnemy.text += player.EnemyPlayer.EvadeCoin
                    ? "Have evade coin\n"
                    : "Without evade\n";
                buffsEnemy.text +=
                    player.EnemyPlayer.FreezeTime > 0
                        ? $"Frizzed for {(int)player.EnemyPlayer.FreezeTime}\n"
                        : "NOT frizzed\n";
                buffsEnemy.text +=
                    player.EnemyPlayer.PoisonTime > 0
                        ? $"Poisonous for {(int)player.EnemyPlayer.PoisonTime}"
                        : "NOT poisoned";
            }
            playerName.text = player.IsHost ? "Player1" : "Player2";
            healthPointsFront.sizeDelta = Vector2.Lerp(
                healthPointsFront.sizeDelta,
                new Vector2(
                    healthPointsBack.sizeDelta.x
                        * (player.HealthPoints / player.CardsConfig.MaxHealthPoints),
                    healthPointsFront.sizeDelta.y
                ),
                lerpSpeed
            );
            shieldPointsFront.sizeDelta = Vector2.Lerp(
                shieldPointsFront.sizeDelta,
                new Vector2(
                    shieldPointsBack.sizeDelta.x
                        * (player.ShieldPoints / player.CardsConfig.ShieldMaxAmount),
                    shieldPointsFront.sizeDelta.y
                ),
                lerpSpeed
            );
        }
    }
}
