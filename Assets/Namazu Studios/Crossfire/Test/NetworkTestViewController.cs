using UnityEngine;
using Unity.Netcode;
using System;
using System.Threading.Tasks;
using Elements.Client;
using Elements.Model;

namespace Elements.Crossfire.Test
{

    public class NetworkTestViewController : MonoBehaviour
    {
        [SerializeField]
        private string player1Name = "TestPlayer1";

        [SerializeField]
        private string player2Name = "TestPlayer2";

        [SerializeField]
        private string elementsRootUrl = "http://localhost:8080/api/rest";

        [SerializeField]
        private NetworkSessionManager client;

        [SerializeField]
        private TestNetworkObject testNetworkObject;

        private SessionCreation playerSessionInfo;

        // Make a matchmaking configuration under your Application with the corresponding name
        private const string MATCHMAKING_CONFIGURATION = "default";

        private void Start()
        {
            ElementsClient.Initialize(elementsRootUrl, false);

            NetworkManager.Singleton.OnServerStarted += () =>
            {
                var go = Instantiate(testNetworkObject.gameObject);
                go.GetComponent<NetworkObject>().Spawn();
            };
        }

        public async void ConnectPlayer1()
        {
            await CreateAccountIfNeeded(player1Name);
            client.StartSession(playerSessionInfo.Session.Profile.Id, playerSessionInfo.SessionSecret);
        }

        public async void ConnectPlayer2()
        {
            await CreateAccountIfNeeded(player2Name);
            client.StartSession(playerSessionInfo.Session.Profile.Id, playerSessionInfo.SessionSecret);
        }

        public void GetMatches()
        {

        }

        public void FindOrCreateMatch()
        {
            client.FindOrCreateMatch(MATCHMAKING_CONFIGURATION);
        }

        public void JoinMatch(string matchId)
        {
            client.JoinMatch(matchId);
        }

        private async Task CreateAccountIfNeeded(string playerName)
        {
            var sessionCreation = await SignIn(playerName);
            playerSessionInfo = sessionCreation;
        }

        private async Task<SessionCreation> SignIn(string userName)
        {
            ElementsClient.LogOut();

            const string password = "test";
            // Be sure that the Application is created in Elements ahead of time!
            const string applicationName = "example";

            try
            {
                //Creates new account if necessary
                await ElementsClient.Api.SignUpUserWithHttpInfoAsync(new UserCreateRequest(
                    name: userName,
                    password: password,
                    level: UserCreateRequest.LevelEnum.USER,
                    profiles: new System.Collections.Generic.List<CreateProfileSignupRequest>
                    {
                        new CreateProfileSignupRequest
                        (
                            displayName: userName,
                            applicationId: applicationName
                        )
                    }
                ));
            }
            catch (ApiException e)
            {
                if (e.ErrorCode == 409)
                    Debug.Log("Account already created, proceeding to login...");
                else
                    Debug.LogError(e);
            }

            var signInResponse = await ElementsClient.Api.CreateUsernamePasswordSessionWithHttpInfoAsync(new UsernamePasswordSessionRequest
            (
                userId: userName,
                password: password,
                profileSelector: $"displayName:{userName}"
            ));

            return signInResponse.Data;
        }
    }
}