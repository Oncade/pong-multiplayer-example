using UnityEngine;
using UnityEngine.SceneManagement;
using ParrelSync;
using System.Linq;
using Elements.Client;
using Elements.Crossfire;
using Elements.Crossfire.Model;

public class CPUPlayerLoader : MonoBehaviour
{     

    [SerializeField]
    private NetworkSessionManager sessionManager;

    public static string USER_NAME = "cpu_player";
    public static string DISPLAY_NAME = "CPU";
    
    private void Start()
    {
        CheckCpuPlayer();
    }

    private void CheckCpuPlayer()
    {
        if (ClonesManager.IsClone() && ElementsClient.Default != null && ElementsClient.Default.IsSessionActive())
        {
            Debug.Log("Logging in as a CPU player");

            const string password = "test";

            ElementsClient.InitializeDefault(ElementsProperties.ELEMENTS_ROOT_URL, false);

            DoUsernamePasswordSignUp(USER_NAME, password, DISPLAY_NAME);
            
        }
    }

    private async void DoUsernamePasswordSignUp(string username, string password, string displayname)
    {
        try
        {
            var userCreateResponse = await ElementsClient.Default.DoSignUpAsync(username, password, displayname);

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

    private async void DoLogin(string username, string password, string profileId = null)
    {
        // Clear headers of any old session data in case it expired
        ElementsClient.Default.LogOut();

        var session = await ElementsClient.Default.DoLoginAsync(username, password, profileId);

        if (session != null)
        {
            if (!sessionManager.IsSessionActive)
            {
                Debug.Log($"Starting session with id {session.Session.Profile.Id}");
                sessionManager.StartSession(session.Session.Profile.Id, session.SessionSecret);
            }

            //Forcibly proceed through the UI
            var loginVC = FindAnyObjectByType<LoginViewController>();
            loginVC.OnContinuePress();
        }
    }

}
