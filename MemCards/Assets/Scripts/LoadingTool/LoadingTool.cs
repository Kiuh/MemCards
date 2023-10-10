using UnityEngine;

public class LoadingTool : MonoBehaviour
{
    public static LoadingTool Singleton { get; private set; }

    [SerializeField]
    private LoadingScreen loadingPrefab;
    private LoadingScreen loadingScreen;

    private void Awake()
    {
        if (Singleton != null)
        {
            Destroy(Singleton.gameObject);
        }
        Singleton = this;
    }

    public void ShowLoading(string text)
    {
        loadingScreen = Instantiate(loadingPrefab, FindObjectOfType<Canvas>().transform);
        loadingScreen.SetText(text);
    }

    public void HideLoading()
    {
        Destroy(loadingScreen);
    }
}
