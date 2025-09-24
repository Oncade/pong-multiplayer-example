using UnityEngine;
using TMPro;
using Elements.Model;

public class JoinMatchButton : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;

    private LobbyViewController viewController;

    private Match match;
    
    public void SetMatch(LobbyViewController viewController, Match match)
    {
        this.viewController = viewController;
        this.match = match;
        this.text.text = $"Join match {match.Id}";
    }

    public void OnClick()
    {
        viewController.JoinMatchLobby(match);
    }
}
