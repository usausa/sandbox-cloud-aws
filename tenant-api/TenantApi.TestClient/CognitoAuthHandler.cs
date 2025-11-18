using System.Net;
using System.Net.Http.Headers;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;

namespace TenantApi.TestClient;

/// <summary>
/// Cognito のアクセストークンを自動取得・自動更新する DelegatingHandler。
/// HttpClient に組み込むことで、呼び出し側がトークン管理を意識せずに API を呼べる。
/// </summary>
internal sealed class CognitoAuthHandler : DelegatingHandler
{
    // トークン期限切れの何秒前に先行更新するか
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

    private readonly string _region;
    private readonly string _userPoolId;
    private readonly string _clientId;
    private readonly string _username;
    private readonly string _password;

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CognitoAuthHandler(string region, string userPoolId, string clientId, string username, string password)
        : base(new HttpClientHandler())
    {
        _region = region;
        _userPoolId = userPoolId;
        _clientId = clientId;
        _username = username;
        _password = password;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetValidTokenAsync(cancellationToken);
        SetAuthHeader(request, token);

        var response = await base.SendAsync(request, cancellationToken);

        // 401 が返った場合はトークンを強制更新して 1 回だけリトライ
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            token = await ForceRefreshAsync(cancellationToken);

            // リクエストは使い捨てなので再構築
            using var retryRequest = CloneRequest(request, token);
            response = await base.SendAsync(retryRequest, cancellationToken);
        }

        return response;
    }

    /// <summary>有効なトークンを返す。期限が近い場合はリフレッシュする。</summary>
    private async Task<string> GetValidTokenAsync(CancellationToken ct)
    {
        // ロック外で先にチェックして無駄なロック取得を避ける
        if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAt - ExpiryMargin)
            return _accessToken;

        await _lock.WaitAsync(ct);
        try
        {
            // ロック取得後に再チェック
            if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAt - ExpiryMargin)
                return _accessToken;

            await RefreshOrAuthenticateAsync(ct);
            return _accessToken!;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>強制的にトークンを無効化して再取得する。</summary>
    private async Task<string> ForceRefreshAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _expiresAt = DateTimeOffset.MinValue;
            await RefreshOrAuthenticateAsync(ct);
            return _accessToken!;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>リフレッシュトークンがあれば使い、なければ SRP 認証する。</summary>
    private async Task RefreshOrAuthenticateAsync(CancellationToken ct)
    {
        var provider = CreateProviderClient();

        if (_refreshToken is not null)
        {
            try
            {
                var refreshResult = await provider.InitiateAuthAsync(
                    new Amazon.CognitoIdentityProvider.Model.InitiateAuthRequest
                    {
                        AuthFlow = Amazon.CognitoIdentityProvider.AuthFlowType.REFRESH_TOKEN_AUTH,
                        ClientId = _clientId,
                        AuthParameters = new Dictionary<string, string>
                        {
                            ["REFRESH_TOKEN"] = _refreshToken,
                        },
                    }, ct);

                ApplyResult(
                    refreshResult.AuthenticationResult.AccessToken,
                    refreshResult.AuthenticationResult.ExpiresIn ?? 3600,
                    refreshToken: null); // リフレッシュフローではリフレッシュトークンは返らない
                return;
            }
            catch
            {
                // リフレッシュ失敗 → SRP 認証にフォールバック
                _refreshToken = null;
            }
        }

        // SRP 認証
        var userPool = new CognitoUserPool(_userPoolId, _clientId, provider);
        var user = new CognitoUser(_username, _clientId, userPool, provider);
        var authResponse = await user.StartWithSrpAuthAsync(new InitiateSrpAuthRequest { Password = _password });

        ApplyResult(
            authResponse.AuthenticationResult.AccessToken,
            authResponse.AuthenticationResult.ExpiresIn ?? 3600,
            authResponse.AuthenticationResult.RefreshToken);
    }

    private void ApplyResult(string accessToken, int expiresIn, string? refreshToken)
    {
        _accessToken = accessToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        if (refreshToken is not null)
            _refreshToken = refreshToken;

        Console.WriteLine($"    [CognitoAuthHandler] Token refreshed. Expires at {_expiresAt:HH:mm:ss} UTC");
    }

    private AmazonCognitoIdentityProviderClient CreateProviderClient() =>
        new(new AnonymousAWSCredentials(), Amazon.RegionEndpoint.GetBySystemName(_region));

    private static void SetAuthHeader(HttpRequestMessage request, string token) =>
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static HttpRequestMessage CloneRequest(HttpRequestMessage original, string token)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return clone;
    }
}
