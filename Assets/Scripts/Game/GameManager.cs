using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Linq;
using System.Threading.Tasks;
using Elements.Client;
using Elements.Model;
using Elements.Crossfire;
using Elements.Crossfire.Model;

[DefaultExecutionOrder(-1)]
public class GameManager : NetworkBehaviour
{
    // ------------------------------------------------------
    // Inspector References
    // ------------------------------------------------------

    [Header("Network Objects")]
    [SerializeField] private NetworkObject ballObject;
    [SerializeField] private NetworkObject hostPaddleObject;
    [SerializeField] private NetworkObject clientPaddleObject;

    [Header("UI Elements")]
    [SerializeField] private Text hostScoreText;
    [SerializeField] private Text clientScoreText;
    [SerializeField] private Text hostName;
    [SerializeField] private Text clientName;

    // ------------------------------------------------------
    // Network State
    // ------------------------------------------------------

    private readonly NetworkVariable<int> hostScore = new(0);
    private readonly NetworkVariable<int> clientScore = new(0);

    // ------------------------------------------------------
    // Runtime State
    // ------------------------------------------------------

    private Profile opponentProfile;
    private Paddle hostPaddle;
    private Paddle clientPaddle;
    private Ball ball;

    // ------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------

    public override async void OnNetworkSpawn()
    {
        // Guard: Skip setup if not in a connected multiplayer session
        if (!IsConnectedGame())
            return;

        // Subscribe to score updates
        hostScore.OnValueChanged += OnHostScoreChanged;
        clientScore.OnValueChanged += OnClientScoreChanged;

        bool isHost = NetworkManager.Singleton.IsServer;

        // Find the opponent (this is a 2-player game so we just take the other one)
        var opponentPlayerInfo = NetworkSessionManager.Instance
            .GetConnectedPlayers()
            .First(p => p.isHost != isHost);

        // Fetch opponent's profile for name display
        await FetchOpponentProfile(opponentPlayerInfo.profileId);

        // Update names and components
        SetPlayerNames();
        InitializeComponents();

        // Host takes control of starting the game and assigning ownership
        if (isHost)
        {
            AssignOwnership(opponentPlayerInfo);
            NewGame();
        }
    }

    public override void OnNetworkDespawn()
    {
        hostScore.OnValueChanged -= OnHostScoreChanged;
        clientScore.OnValueChanged -= OnClientScoreChanged;
    }

    // ------------------------------------------------------
    // Game Flow
    // ------------------------------------------------------

    public void NewGame()
    {
        SetPlayerScoreServerRpc(0);
        SetComputerScoreServerRpc(0);
        NewRound();
    }

    public void NewRound()
    {
        // Reset paddles
        hostPaddle.ResetPosition();
        clientPaddle.ResetPosition();

        // Reset ball with smoothing temporarily disabled
        var ballNetworkTransform = ballObject.NetworkTransforms.First();
        ballNetworkTransform.PositionLerpSmoothing = false;
        ball.ResetPosition();
        ballNetworkTransform.PositionLerpSmoothing = true;

        // Delay ball launch slightly
        CancelInvoke();
        Invoke(nameof(StartRound), 1f);
    }

    private void StartRound()
    {
        ball.AddStartingForce();
    }

    // ------------------------------------------------------
    // Scoring
    // ------------------------------------------------------

    public void OnPlayerScored()
    {
        if (IsServer)
        {
            hostScore.Value++;
            NewRound();
        }
    }

    public void OnComputerScored()
    {
        if (IsServer)
        {
            clientScore.Value++;
            NewRound();
        }
    }

    private void OnHostScoreChanged(int oldValue, int newValue)
    {
        hostScoreText.text = newValue.ToString();
    }

    private void OnClientScoreChanged(int oldValue, int newValue)
    {
        clientScoreText.text = newValue.ToString();
    }

    // ------------------------------------------------------
    // Networking
    // ------------------------------------------------------

    [ServerRpc]
    private void SetPlayerScoreServerRpc(int score)
    {
        hostScore.Value = score;
    }

    [ServerRpc]
    private void SetComputerScoreServerRpc(int score)
    {
        clientScore.Value = score;
    }

    private void AssignOwnership(PlayerInfo opponentPlayerInfo)
    {
        clientPaddleObject.ChangeOwnership(opponentPlayerInfo.networkId);
    }

    private bool IsConnectedGame()
    {
        return ElementsClient.IsSessionActive() && NetworkSessionManager.Instance != null;
    }

    // ------------------------------------------------------
    // Profiles & UI
    // ------------------------------------------------------

    private async Task FetchOpponentProfile(string opponentProfileId)
    {
        opponentProfile = await ElementsClient.Api.GetProfileAsync(opponentProfileId);
    }

    private void SetPlayerNames()
    {
        if (IsServer)
        {
            hostName.text = ElementsClient.GetSession().Profile.DisplayName;
            clientName.text = opponentProfile.DisplayName;
        }
        else
        {
            hostName.text = opponentProfile.DisplayName;
            clientName.text = ElementsClient.GetSession().Profile.DisplayName;
        }
    }

    // ------------------------------------------------------
    // Component Setup
    // ------------------------------------------------------

    private void InitializeComponents()
    {
        ball = ballObject.GetComponent<Ball>();

        // One paddle is controlled by a player, the other by AI
        if (IsCPU(opponentProfile.DisplayName))
        {
            // Opponent is CPU, so host is player
            var cpuPaddle = clientPaddleObject.gameObject.AddComponent<ComputerPaddle>();
            cpuPaddle.ball = ball.GetComponent<Rigidbody2D>();
            clientPaddle = cpuPaddle;

            hostPaddle = hostPaddleObject.gameObject.AddComponent<PlayerPaddle>();
        }
        else
        {
            // Opponent is real player, so host is CPU
            var cpuPaddle = hostPaddleObject.gameObject.AddComponent<ComputerPaddle>();
            cpuPaddle.ball = ball.GetComponent<Rigidbody2D>();
            hostPaddle = cpuPaddle;

            clientPaddle = clientPaddleObject.gameObject.AddComponent<PlayerPaddle>();
        }
    }

    private bool IsCPU(string name)
    {
        return name == CPUPlayerLoader.USER_NAME || name == CPUPlayerLoader.DISPLAY_NAME;
    }
}