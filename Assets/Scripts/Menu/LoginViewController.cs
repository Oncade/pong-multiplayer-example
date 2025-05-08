using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Net;
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
    private TMPro.TMP_Text createAccountErrorText;

    //It's generally best to keep this in an external config to help determine the environment
    private const string ELEMENTS_ROOT_URL = "http://localhost:8080/api/rest";

    //Must match the name of the Application that was created in Elements
    private const string ELEMENTS_APPLICATION_NAME = "Pong";

#region Setup

    private void Start()
    {
        //You only need to do this once
        if(!ElementsClient.IsInitialized())
        {
            ElementsClient.Initialize(ELEMENTS_ROOT_URL);
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
        DoSignUp(signUpUsernameInputField.text, signUpPasswordInputField.text, signUpDisplayNameInputField.text);
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
            ElementsClient.SetSession(sessionCreation);
            await FetchProfile(sessionCreation.Session.User.Id);
            LoadPongScene();
        }
    }

    private async void DoSignUp(string username, string password, string displayname)
    {
        var userCreateResponse = await ElementsClient.Api.SignUpUserAsync(new UserCreateRequest
        {
            Name = username,
            Password = password,
            Level = UserCreateRequest.LevelEnum.USER,
            Profiles = new List<CreateProfileSignupRequest>
            {
                //Make sure that you've created an application named Pong in Elements
                new CreateProfileSignupRequest(
                    applicationId: ELEMENTS_APPLICATION_NAME,
                    displayName: displayname
                )
            }
        });

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
                ElementsClient.GetSession().Profile = profile;                
            }
        }

    }

}
