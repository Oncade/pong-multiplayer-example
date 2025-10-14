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

    public void LoadMatch(MultiMatch match = null)
    {
        messageText.text = "Waiting for opponent...";

        if (!sessionManager.IsSessionActive)
        {
            sessionManager.StartSession(ElementsClient.Default.GetSession().Profile.Id,
                                        ElementsClient.Default.GetSessionToken());

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
