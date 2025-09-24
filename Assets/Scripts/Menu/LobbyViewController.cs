using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading;
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

    public Match SelectedMatch { get; private set; }

    private CancellationTokenSource cancellationTokenSource = new();

    private int offset = 0;
    private int count = 20;

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

    public void JoinMatchLobby(Match match)
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

        try
        {
            var matches = await ElementsClient.Api.GetMatchesAsync(offset, count, null, cancellationTokenSource.Token);

            if (matches != null)
            {
                HideInfoText();
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
    }

    private void SpawnMatchButtons(PaginationMatch matches)
    {
        foreach(Match match in matches.Objects)
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
