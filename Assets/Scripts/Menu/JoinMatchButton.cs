using UnityEngine;
using TMPro;
using Elements.Model;

public class JoinMatchButton : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;

    private LobbyViewController viewController;

    private MultiMatch match;
    
    public void SetMatch(LobbyViewController viewController, MultiMatch match)
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
