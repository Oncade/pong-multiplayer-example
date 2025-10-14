using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading;
using System.Threading.Tasks;
using Elements.Model;
using Elements.Client;
using TMPro;

public class LobbyViewController : MonoBehaviour, IViewController
{
    [SerializeField] private JoinMatchButton joinMatchButtonPrefab;

    [SerializeField] private Toggle matchListToggle;
    [SerializeField] private Toggle playerListToggle;

    [SerializeField] private Transform matchListParent;

    [SerializeField] private TMP_Text infoText;

    // IViewController Events
    public event Action OnBack;
    public event Action OnNext;

    public MultiMatch SelectedMatch { get; private set; }

    private CancellationTokenSource cancellationTokenSource = new();

    private int offset = 0;
    private int count = 20;
    private const int refreshDelay = 3000;

    private void OnEnable()
    {
        ClearMatches();
        matchListToggle.isOn = true;
        GetMatches();
    }

    public void FindOrCreateMatch()
    {
        SelectedMatch = null;
        OnNext?.Invoke();
    }

    public void JoinMatchLobby(MultiMatch match)
    {
        playerListToggle.isOn = true;
        SelectedMatch = match;
        OnNext?.Invoke();
    }

    public void Logout()
    {
        cancellationTokenSource.Cancel();
        OnBack?.Invoke();
    }

    private async void GetMatches()
    {
        SetInfoText("Searching for matches...");

        while(this != null && gameObject.activeSelf)
        {
            try
            {
                var matches = await ElementsClient.Default.Api.GetMatches1Async(offset, count, null, cancellationTokenSource.Token);

                if (matches != null)
                {
                    HideInfoText();
                    ClearMatches();
                    SpawnMatchButtons(matches);
                }
                else
                {
                    SetInfoText("There was an error fetching matches!");
                }

            }
            catch
            {
                HideInfoText();
            }

            await Task.Delay(refreshDelay);
        }
    }

    private void SpawnMatchButtons(PaginationMultiMatch matches)
    {
        foreach(MultiMatch match in matches.Objects)
        {
            var button = Instantiate(joinMatchButtonPrefab, matchListParent);
            button.SetMatch(this, match);
        }
    }

    private void ClearMatches()
    {
        foreach(Transform child in matchListParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void SetInfoText(string text)
    {
        infoText.text = text;
        infoText.gameObject.SetActive(true);
    }

    private void HideInfoText()
    {
        infoText.gameObject.SetActive(false);
    }
}
