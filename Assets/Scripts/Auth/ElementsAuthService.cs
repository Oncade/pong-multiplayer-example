using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Elements.Client;
using Elements.Model;

public static class ElementsAuthService
{

    /// <summary>
    /// Log in with username/password, and fetch profile if needed.
    /// </summary>
    public static async Task<SessionCreation> DoLoginAsync(
        this ElementsClient elementsClient,
        string username,
        string password,
        string profileId = null)
    {
        var request = new UsernamePasswordSessionRequest(
            userId: username,
            password: password,
            profileId: profileId
        );

        var sessionCreation = await elementsClient.Api.CreateUsernamePasswordSessionAsync(request);

        if (sessionCreation != null)
        {
            elementsClient.SetSessionCreation(sessionCreation);

            if (sessionCreation.Session.Profile == null)
            {
                await FetchProfileAsync(elementsClient, sessionCreation.Session.User.Id);
            }
        }

        return sessionCreation;
    }

    /// <summary>
    /// Create a new user account with username, password, and display name.
    /// </summary>
    public static async Task<UserCreateResponse> DoSignUpAsync(
        this ElementsClient elementsClient,
        string username,
        string password,
        string displayname)
    {
        return await elementsClient.Api.SignUpUserAsync(new UserCreateRequest
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
    public static async Task<Profile> FetchProfileAsync(
        this ElementsClient elementsClient,
        string userId,        
        string displayName = "Player")
    {
        var profiles = await elementsClient.Api.GetProfilesAsync(
            application: ElementsProperties.ELEMENTS_APPLICATION_NAME,
            user: userId
        );

        var profile = profiles?.Objects.FirstOrDefault();

        if (profile != null)
        {
            elementsClient.SetProfile(profile);
            return profile;
        }

        // Create if missing
        profile = await elementsClient.Api.CreateProfileAsync(new CreateProfileRequest
            (
                applicationId: ElementsProperties.ELEMENTS_APPLICATION_NAME,
                displayName: displayName,
                userId: userId
            ));

        if (profile != null)
        {
            elementsClient.SetProfile(profile);
        }

        return profile;
    }
}
