using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleOAuthService
{
    private const string clientId = ""; // From Google Cloud
    private const string clientSecret = ""; // From Google Cloud
    private const string desktopRedirectUri = "http://localhost:51782/"; //You can use whichever port works best for you
    private const string mobileRedirectUri = "Pong://auth"; //Must match your Product Name from Project Settings -> Player

    private TaskCompletionSource<string> authCodeTcs;

    public async Task<string> LoginAsync()
    {
        string redirectUri = IsMobile() ? mobileRedirectUri : desktopRedirectUri;
        string authUrl = BuildGoogleAuthUrl(clientId, redirectUri);

        authCodeTcs = new TaskCompletionSource<string>();

        if (IsMobile())
        {
            Application.deepLinkActivated += OnDeepLinkActivated;
        }

        Application.OpenURL(authUrl);

        if (!IsMobile())
        {
            _ = WaitForCodeDesktopAsync(redirectUri);
        }

        string authCode = await authCodeTcs.Task;
        string idToken = await ExchangeCodeForIdToken(authCode, clientId, clientSecret, redirectUri);

        return idToken;
    }

    private string BuildGoogleAuthUrl(string clientId, string redirectUri)
    {
        string scope = Uri.EscapeDataString("openid email profile");
        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={clientId}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&response_type=code" +
               $"&scope={scope}" +
               $"&access_type=offline" +
               $"&prompt=consent";
    }

    private async Task WaitForCodeDesktopAsync(string redirectUri)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        var context = await listener.GetContextAsync();
        var code = context.Request.QueryString["code"];

        byte[] responseBytes = Encoding.UTF8.GetBytes("<html><body>Login complete. You may close this window.</body></html>");
        context.Response.ContentLength64 = responseBytes.Length;

        await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);

        context.Response.Close();
        listener.Stop();

        authCodeTcs.TrySetResult(code);
    }

    private async Task<string> ExchangeCodeForIdToken(string code, string clientId, string clientSecret, string redirectUri)
    {
        var form = new Dictionary<string, string>
        {
            { "code", code },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" },
        };

        using var request = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form);
        request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        var asyncOp = request.SendWebRequest();

        TaskCompletionSource<UnityWebRequest.Result> tsc = new TaskCompletionSource<UnityWebRequest.Result>();
        asyncOp.completed += (_) => tsc.TrySetResult(request.result);

        await tsc.Task;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Token exchange failed: " + request.error);
            return null;
        }

        var json = request.downloadHandler.text;
        var tokenResponse = JsonUtility.FromJson<GoogleTokenResponse>(json);
        return tokenResponse.id_token;
    }

    private void OnDeepLinkActivated(string url)
    {
        Application.deepLinkActivated -= OnDeepLinkActivated;

        Uri uri = new(url);
        string code = null;

        foreach (var kv in uri.Query.TrimStart('?').Split('&'))
        {
            var pair = kv.Split('=');
            if (pair.Length == 2 && pair[0] == "code")
            {
                code = pair[1];
                break;
            }
        }

        authCodeTcs?.TrySetResult(code);
    }

    private bool IsMobile()
    {
#if UNITY_IOS || UNITY_ANDROID
        return true;
#else
        return false;
#endif
    }
    
    private class GoogleTokenResponse
    {
        public string access_token;
        public string id_token;
        public string refresh_token;
        public string token_type;
        public int expires_in;
    }
}