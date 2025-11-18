using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using TenantApi.TestClient;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var cognitoRegion = configuration["Cognito:Region"] ?? throw new InvalidOperationException("Cognito:Region is required.");
var userPoolId = configuration["Cognito:UserPoolId"] ?? throw new InvalidOperationException("Cognito:UserPoolId is required.");
var clientId = configuration["Cognito:ClientId"] ?? throw new InvalidOperationException("Cognito:ClientId is required.");
var apiBaseUrl = configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("Api:BaseUrl is required.");
var username = configuration["TestUser:Username"] ?? throw new InvalidOperationException("TestUser:Username is required.");
var password = configuration["TestUser:Password"] ?? throw new InvalidOperationException("TestUser:Password is required.");
var baseUri = new Uri(apiBaseUrl.TrimEnd('/'));

Console.WriteLine("=== TenantApi Test Client ===");
Console.WriteLine($"User Pool : {userPoolId}");
Console.WriteLine($"API       : {apiBaseUrl}");
Console.WriteLine();

// ----------------------------------------------------------------
// パターン 1: 手動でトークンを取得して HttpClient に渡す
// ----------------------------------------------------------------
Console.WriteLine("--- Pattern 1: Manual token management ---");
Console.WriteLine("[1] Authenticating...");
var accessToken = await GetAccessTokenAsync(cognitoRegion, userPoolId, clientId, username, password);
Console.WriteLine("    Access token acquired.");
Console.WriteLine();

using var manualClient = new HttpClient { BaseAddress = baseUri };

Console.WriteLine("[2] GET /health");
await CallApiAsync(manualClient, HttpMethod.Get, "/health", accessToken: null);
Console.WriteLine();

Console.WriteLine("[3] GET /tenant");
await CallApiAsync(manualClient, HttpMethod.Get, "/tenant", accessToken);
Console.WriteLine();

// ----------------------------------------------------------------
// パターン 2: CognitoAuthHandler を使用してトークン管理を自動化
//   - 初回リクエスト時に自動認証
//   - 有効期限 5 分前に自動リフレッシュ
//   - 401 応答時に強制再取得してリトライ
// ----------------------------------------------------------------
Console.WriteLine("--- Pattern 2: CognitoAuthHandler (auto token management) ---");
using var authHandler = new CognitoAuthHandler(cognitoRegion, userPoolId, clientId, username, password);
using var autoClient = new HttpClient(authHandler) { BaseAddress = baseUri };

Console.WriteLine("[4] GET /health  (no auth needed, handler skips Bearer)");
await CallApiAsync(autoClient, HttpMethod.Get, "/health", accessToken: null);
Console.WriteLine();

Console.WriteLine("[5] GET /tenant  (handler acquires token automatically)");
await CallApiAsync(autoClient, HttpMethod.Get, "/tenant", accessToken: null);
Console.WriteLine();

Console.WriteLine("[6] GET /tenant  (second call — uses cached token)");
await CallApiAsync(autoClient, HttpMethod.Get, "/tenant", accessToken: null);

// ----------------------------------------------------------------
// Helpers
// ----------------------------------------------------------------

static async Task<string> GetAccessTokenAsync(
    string region, string userPoolId, string clientId, string username, string password)
{
    var providerClient = new AmazonCognitoIdentityProviderClient(
        new AnonymousAWSCredentials(),
        Amazon.RegionEndpoint.GetBySystemName(region));

    var userPool = new CognitoUserPool(userPoolId, clientId, providerClient);
    var user = new CognitoUser(username, clientId, userPool, providerClient);

    var authRequest = new InitiateSrpAuthRequest { Password = password };
    var authResponse = await user.StartWithSrpAuthAsync(authRequest);

    return authResponse.AuthenticationResult.AccessToken;
}

static async Task CallApiAsync(HttpClient client, HttpMethod method, string path, string? accessToken)
{
    using var request = new HttpRequestMessage(method, path);
    if (accessToken is not null)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    using var response = await client.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();

    Console.WriteLine($"    Status : {(int)response.StatusCode} {response.ReasonPhrase}");
    Console.WriteLine($"    Body   : {body}");
}
