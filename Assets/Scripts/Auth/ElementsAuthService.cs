using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Elements.Client;
using Elements.Model;
using Elements.Crossfire;

public static class ElementsAuthService
{
    /// <summary>
    /// Ensure ElementsClient is initialized (safe to call multiple times).
    /// </summary>
    public static void EnsureInitialized(bool enableLogging = true)
    {
        if (!ElementsClient.IsInitialized())
        {
            ElementsClient.Initialize(ElementsProperties.ELEMENTS_ROOT_URL, enableLogging);
        }
    }

    /// <summary>
    /// Log in with username/password, and fetch profile if needed.
    /// </summary>
    public static async Task<SessionCreation> DoLoginAsync(
        string username,
        string password,
        string profileId = null)
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
            {
                await FetchProfileAsync(sessionCreation.Session.User.Id);
            }
        }

        return sessionCreation;
    }

    /// <summary>
    /// Create a new user account with username, password, and display name.
    /// </summary>
    public static async Task<UserCreateResponse> DoSignUpAsync(
        string username,
        string password,
        string displayname)
    {
        return await ElementsClient.Api.SignUpUserAsync(new UserCreateRequest
        (
            name: username,
            password: password,
            level: UserCreateRequest.LevelEnum.USER,
            profiles: new List<CreateProfileSignupRequest>
            {
                new CreateProfileSignupRequest(
                    applicationId: ElementsProperties.ELEMENTS_APPLICATION_NAME,
                    displayName: displayname
                )
            }
        ));
    }

    /// <summary>
    /// Fetch the first profile for a given user. Creates one if missing (optional).
    /// </summary>
    public static async Task<Profile> FetchProfileAsync(string userId, bool createIfMissing = false, string defaultDisplayName = "Player")
    {
        var profiles = await ElementsClient.Api.GetProfilesAsync(
            application: ElementsProperties.ELEMENTS_APPLICATION_NAME,
            user: userId
        );

        var profile = profiles?.Objects.FirstOrDefault();
        if (profile != null)
        {
            ElementsClient.SetProfile(profile);
            return profile;
        }

        if (createIfMissing)
        {
            profile = await ElementsClient.Api.CreateProfileAsync(new CreateProfileRequest
            (
                applicationId: ElementsProperties.ELEMENTS_APPLICATION_NAME,
                displayName: defaultDisplayName,
                userId: userId
            ));

            if (profile != null)
            {
                ElementsClient.SetProfile(profile);
            }
        }

        return profile;
    }
}
