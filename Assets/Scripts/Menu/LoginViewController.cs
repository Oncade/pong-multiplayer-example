using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Elements.Client;
using Elements.Model;

public class LoginViewController : MonoBehaviour
{
    [SerializeField]
    private Toggle loginToExistingAccountView;

    [SerializeField]
    private Toggle activeSessionView;

    [SerializeField]
    private Toggle newSessionView;

    [SerializeField]
    private Toggle createAccountView;

    [SerializeField]
    private Toggle createProfileView;

    [SerializeField]
    private TMPro.TMP_Text welcomeBackText;

    [SerializeField]
    private TMPro.TMP_InputField loginUsernameInputField;

    [SerializeField]
    private TMPro.TMP_InputField loginPasswordInputField;

    [SerializeField]
    private TMPro.TMP_InputField signUpUsernameInputField;

    [SerializeField]
    private TMPro.TMP_InputField signUpPasswordInputField;

    [SerializeField]
    private TMPro.TMP_InputField signUpDisplayNameInputField;

    [SerializeField]
    private TMPro.TMP_InputField createProfileDisplayNameInputField;

    [SerializeField]
    private TMPro.TMP_Text createAccountErrorText;

    //It's generally best to keep this in an external config to help determine the environment
    private const string ELEMENTS_ROOT_URL = "http://localhost:8080/api/rest";

    //Must match the name of the Application that was created in Elements
    private const string ELEMENTS_APPLICATION_NAME = "Pong";

#region Setup

    private async void Start()
    {
        //You only need to do this once
        if(!ElementsClient.IsInitialized())
        {
            ElementsClient.Initialize(ELEMENTS_ROOT_URL);
            HelloClient.HelloApi.Configuration.DefaultHeaders["Elements-SessionSecret"] = ElementsClient.Api.Configuration.ApiKey["Elements-SessionSecret"];
            var respsonse = await HelloClient.HelloApi.SayHelloWithAuthAsync();
            Debug.Log(respsonse);
        }

        SetDefaultViewState();
    }

    private async void SetDefaultViewState()
    {
        if(ElementsClient.IsSessionActive())
        {
            if(ElementsClient.GetSession().Profile == null)
            {
                await FetchProfile(ElementsClient.GetSession().User.Id);
            }

            activeSessionView.isOn = true;
            welcomeBackText.text = $"Welcome back\n{ElementsClient.GetSession()?.Profile?.DisplayName}!";
        }
        else
        {            
            newSessionView.isOn = true;
        }

        createAccountErrorText.text = null;
    }

#endregion
#region Button Click Events

    public void OnNewAccountButtonPress()
    {        
        createAccountView.isOn = true;
    }

    public void OnExistingAccountButtonPress()
    {
        loginToExistingAccountView.isOn = true;
    }

    public void OnLoginButtonPress()
    {
        DoLogin(loginUsernameInputField.text, loginPasswordInputField.text, null);
    }

    public void OnContinuePress()
    {
        LoadPongScene();
    }

    public void OnLogoutPress()
    {
        ElementsClient.LogOut();

        newSessionView.isOn = true;
    }

    public void OnCreateAccount()
    {
        DoUsernamePasswordSignUp(signUpUsernameInputField.text, signUpPasswordInputField.text, signUpDisplayNameInputField.text);
    }

    public void OnGoogleLoginButtonPressed()
    {
        DoLoginWithGoogle();
    }

    public void OnCreateProfilePressed()
    {
        CreateProfile(createProfileDisplayNameInputField.text);
    }

#endregion

    private void LoadPongScene()
    {
        SceneManager.LoadScene("Pong");
    }

    private async void DoLogin(string username, string password, string profileId)
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
            await FetchProfile(sessionCreation.Session.User.Id);
            LoadPongScene();
        }
    }

    private async void DoUsernamePasswordSignUp(string username, string password, string displayname)
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
                    applicationId: ELEMENTS_APPLICATION_NAME,
                    displayName: displayname
                )
            }
        ));

        if(userCreateResponse != null)
        {
            DoLogin(username, password, userCreateResponse.Profiles.FirstOrDefault()?.Id);
        }
    }

    private async Task FetchProfile(string userId)
    {
        var profiles = await ElementsClient.Api.GetProfilesAsync(
            application: ELEMENTS_APPLICATION_NAME,
            user: userId
        );

        if (profiles != null)
        {
            var profile = profiles.Objects.FirstOrDefault();

            if (profile != null)
            {
                ElementsClient.SetProfile(profile);
                HelloClient.HelloApi.Configuration.ApiKey["Elements-SessionSecret"] = ElementsClient.Api.Configuration.ApiKey["Elements-SessionSecret"];
                var respsonse = await HelloClient.HelloApi.SayHelloWithAuthAsync();
                Debug.Log(respsonse);
            }
        }

    }

    private async void DoLoginWithGoogle()
    {
        var googleLogin = new GoogleOAuthLogin();
        string idToken = await googleLogin.LoginAsync();
        Debug.Log("Google ID Token: " + idToken);

        // Send to your server
        var sessionCreation = await ElementsClient.Api.CreateOidcSessionAsync(
            new OidcSessionRequest(jwt: idToken)
        );

        if (sessionCreation != null)
        {
            ElementsClient.SetSessionCreation(sessionCreation);
            await FetchProfile(sessionCreation.Session.User.Id);

            if(ElementsClient.GetSession().Profile == null)
            {
                createProfileView.isOn = true;
            }
            else
            {
                LoadPongScene();
            }
        }
    }

    private async void CreateProfile(string displayName)
    {
        var profile = await ElementsClient.Api.CreateProfileAsync(new CreateProfileRequest
        (
            applicationId: ELEMENTS_APPLICATION_NAME,
            displayName: displayName,
            userId: ElementsClient.GetSession().User.Id
        ));

        if (profile != null)
        {
            ElementsClient.GetSession().Profile = profile;
            LoadPongScene();
        }
    }

}
