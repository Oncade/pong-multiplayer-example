using UnityEngine;
using System;
using Elements.Client;
using Elements.Crossfire;
using Elements.Model;
using Elements.Crossfire.Model;
using TMPro;

public class MatchLoadingViewController : MonoBehaviour, IViewController
{
    [SerializeField]
    private TMP_Text messageText;
    
    // IViewController Events
    public event Action OnBack;
    public event Action OnNext;

    [SerializeField]
    private NetworkSessionManager sessionManager;

    public void LoadMatch(Match match = null)
    {
        messageText.text = "Waiting for opponent...";

        var sessionToken = ElementsClient.Api.Configuration.ApiKey["Elements-SessionSecret"].Split(" ")[0];

        if (!sessionManager.IsSessionActive)
        {
            sessionManager.StartSession(ElementsClient.GetSession().Profile.Id, sessionToken);

            sessionManager.OnMatchJoined += OnMatchJoined;
            sessionManager.OnPlayerJoined += OnPlayerJoined;
        }

        if (match != null)
        {
            sessionManager.JoinMatch(match.Id);
        }
        else
        {
            sessionManager.FindOrCreateMatch(ElementsProperties.MATCHMAKING_CONFIGURATION);
        }

    }

    public void CancelMatch()
    {        
        OnBack?.Invoke();
    }

    private void OnMatchJoined(string matchId)
    {
        messageText.text = "Joined match: " + matchId;
    }

    private void OnPlayerJoined(PlayerInfo playerInfo)
    {
        messageText.text = "All players connected! Starting match...";

        OnNext?.Invoke();
    }

}
