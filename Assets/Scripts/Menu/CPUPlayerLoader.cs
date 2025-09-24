using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using ParrelSync;
using Elements.Client;
using Elements.Crossfire;
using Elements.Crossfire.Model;

public class CPUPlayerLoader : MonoBehaviour
{     

    [SerializeField]
    private NetworkSessionManager sessionManager;

    public static string USER_NAME = "cpu_player";
    public static string DISPLAY_NAME = "CPU";
    
    private bool isCpuPlayer = false;

    private void Start()
    {
        CheckCpuPlayer();
    }

    private void OnDestroy()
    {
        if(isCpuPlayer)
        {
            sessionManager.OnPlayerJoined -= OnAllPlayersConnected;
        }
    }

    private void CheckCpuPlayer()
    {
        if (ClonesManager.IsClone())
        {
            Debug.Log("Logging in as a CPU player");

            if (!ElementsClient.IsInitialized())
            {
                ElementsClient.Initialize(ElementsProperties.ELEMENTS_ROOT_URL, false);
            }

            const string password = "test";

            isCpuPlayer = true;

            DoUsernamePasswordSignUp(USER_NAME, password, DISPLAY_NAME);
            
        }
    }

    private async void DoLogin(string username, string password, string profileId = null)
    {
        var session = await ElementsAuthService.DoLoginAsync(username, password, profileId);

        if (session != null)
        {
            if (!sessionManager.IsSessionActive)
            {
                Debug.Log($"Starting session with id {session.Session.Profile.Id}");
                sessionManager.StartSession(session.Session.Profile.Id, session.SessionSecret);
                sessionManager.OnPlayerJoined += OnAllPlayersConnected;
            }

            sessionManager.FindOrCreateMatch(ElementsProperties.MATCHMAKING_CONFIGURATION);
        }
    }

    private async void DoUsernamePasswordSignUp(string username, string password, string displayname)
    {
        try
        {
            var userCreateResponse = await ElementsAuthService.DoSignUpAsync(username, password, displayname);
            if (userCreateResponse != null)
            {
                DoLogin(username, password, userCreateResponse.Profiles.FirstOrDefault()?.Id);
            }
        }
        catch
        {
            DoLogin(username, password);
        }
    }

    private void OnAllPlayersConnected(PlayerInfo playerInfo)
    {
        Debug.Log("All players connected! " + playerInfo.profileId);

        SceneManager.LoadScene("PongMulti");
    }

}
