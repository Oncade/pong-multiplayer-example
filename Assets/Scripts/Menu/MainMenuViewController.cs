using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Threading.Tasks;


public class MainMenuViewController : MonoBehaviour
{
    [SerializeField]
    private LoginViewController loginViewController;

    [SerializeField]
    private LobbyViewController lobbyViewController;

    [SerializeField]
    private MatchLoadingViewController matchLoadingViewController;

    [SerializeField]
    private Toggle loginViewControllerToggle;

    [SerializeField]
    private Toggle lobbyViewControllerToggle;

    [SerializeField]
    private Toggle matchLoadingViewControllerToggle;

    private void Start()
    {
        loginViewController.OnNext += GoToLobby;
        lobbyViewController.OnNext += GoToMatch;
        lobbyViewController.OnBack += GoToLogin;
        matchLoadingViewController.OnBack += GoToLobby;
        matchLoadingViewController.OnNext += LoadGame;
    }

    private void GoToLobby()
    {
        lobbyViewControllerToggle.isOn = true;
    }

    private void GoToLogin()
    {
        loginViewControllerToggle.isOn = true;
    }

    private void GoToMatch()
    {
        matchLoadingViewControllerToggle.isOn = true;
        matchLoadingViewController.LoadMatch(lobbyViewController.SelectedMatch);
    }

    private async void LoadGame()
    {
        // This is important as it may take a moment to fully set up the
        // NetworkManager, and the SceneManager is created later in the process
        while (NetworkManager.Singleton.SceneManager == null)
        {
            Debug.Log("Waiting for network manager to start...");
            await Task.Delay(100);
        }

        NetworkManager.Singleton.SceneManager.LoadScene("PongMulti", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
