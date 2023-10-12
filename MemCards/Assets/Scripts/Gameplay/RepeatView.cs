using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RepeatView : MonoBehaviour
{
    [SerializeField]
    private Button leave;
    public Button Leave => leave;

    [SerializeField]
    private Button ready;
    public Button Ready => ready;

    [SerializeField]
    private TMP_Text winLabel;
    public TMP_Text WinLabel => winLabel;

    [SerializeField]
    private TMP_Text infoLabel;
    public TMP_Text InfoLabel => infoLabel;
}
