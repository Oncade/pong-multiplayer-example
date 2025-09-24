using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ParrelSync;
using Elements.Client;
using Elements.Model;
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
        var request = new UsernamePasswordSessionRequest(
            userId: username,
            password: password,
            profileId: profileId
        );

        var sessionCreation = await ElementsClient.Api.CreateUsernamePasswordSessionAsync(request);

        if (sessionCreation != null)
        {
            ElementsClient.SetSessionCreation(sessionCreation);

            if (sessionCreation.Session.Profile == null)
                await FetchProfile(sessionCreation.Session.User.Id);

            if(!sessionManager.IsSessionActive)
            {
                Debug.Log("Starting session with id " + sessionCreation.Session.Profile.Id + " and secret " + sessionCreation.SessionSecret);
                sessionManager.StartSession(sessionCreation.Session.Profile.Id, sessionCreation.SessionSecret);
                sessionManager.OnPlayerJoined += OnAllPlayersConnected;
            }

            sessionManager.FindOrCreateMatch(ElementsProperties.MATCHMAKING_CONFIGURATION);
        }
    }

    private async void DoUsernamePasswordSignUp(string username, string password, string displayname)
    {
 
        try
        {
            var userCreateResponse = await ElementsClient.Api.SignUpUserAsync(new UserCreateRequest
            (
                name: username,
                password: password,
                level: UserCreateRequest.LevelEnum.USER,
                profiles: new List<CreateProfileSignupRequest>
                {
                    //Make sure that you've created an application named Pong in Elements
                    new CreateProfileSignupRequest(
                        applicationId: ElementsProperties.ELEMENTS_APPLICATION_NAME,
                        displayName: displayname
                    )
                }
            ));

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

    private async Task FetchProfile(string userId)
    {
        var profiles = await ElementsClient.Api.GetProfilesAsync(
            application: ElementsProperties.ELEMENTS_APPLICATION_NAME,
            user: userId
        );

        if (profiles != null)
        {
            var profile = profiles.Objects.FirstOrDefault();

            if (profile != null)
            {
                ElementsClient.SetProfile(profile);
            }
            else
            {
                profile = await ElementsClient.Api.CreateProfileAsync(new CreateProfileRequest
                (
                    applicationId: ElementsProperties.ELEMENTS_APPLICATION_NAME,
                    displayName: "CPU",
                    userId: userId
                ));

                if (profile != null)
                {
                    ElementsClient.SetProfile(profile);
                }
            }
        }
    }


    private void OnAllPlayersConnected(PlayerInfo playerInfo)
    {
        Debug.Log("All players connected! " + playerInfo.profileId);

        SceneManager.LoadScene("PongMulti");
    }

}
