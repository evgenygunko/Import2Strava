using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Import2Strava.Services
{
    public interface IAuthenticationService
    {
        Task<string> GetAccessTokenAsync();
    }

    public class AuthenticationService : IAuthenticationService
    {
        private const string AuthorizationEndpointUri = "https://www.strava.com/oauth/authorize";
        private const string TokenRequestUri = "https://www.strava.com/oauth/token";

        // client configuration
        private string _clientID;
        private string _clientSecret;

        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;

        private string _accessToken;
        private string _refreshToken;
        private DateTime _expiresAt;

        public AuthenticationService(IConfiguration configuration,
            ILogger<AuthenticationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_expiresAt <= DateTime.Now)
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    await AuthenticateAsync();
                }
                else
                {
                    _logger.LogInformation("Exchange refresh token for new authentication token...");

                    // Creates a redirect URI using an available port on the loopback address.
                    string redirectURI = $"http://{IPAddress.Loopback}:{GetRandomUnusedPort()}/";
                    string code_verifier = RandomDataBase64url(32);

                    await PerformCodeExchangeAsync(_refreshToken, code_verifier, redirectURI);
                }
            }

            return _accessToken;
        }

        #region Private Methods

        private async Task AuthenticateAsync()
        {
            Console.WriteLine("Starting authentication, you will be redirected to the browser...");
            _logger.LogInformation("Starting authentication...");

            _accessToken = null;
            _refreshToken = null;
            _expiresAt = DateTime.Now.AddSeconds(-1);

            _clientID = _configuration["Strava:ClientId"];
            _clientSecret = _configuration["Strava:ClientSecret"];

            // Generates state and PKCE values.
            string state = RandomDataBase64url(32);
            string code_verifier = RandomDataBase64url(32);
            string code_challenge = ToBase64urlencodeNoPadding(ToSHA256(code_verifier));
            const string code_challenge_method = "S256";

            // Creates a redirect URI using an available port on the loopback address.
            string redirectURI = $"http://{IPAddress.Loopback}:{GetRandomUnusedPort()}/";
            _logger.LogInformation("redirect URI: " + redirectURI);

            // Creates an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(redirectURI);
            _logger.LogInformation("Listening..");
            http.Start();

            // Creates the OAuth 2.0 authorization request.
            // http://developers.strava.com/docs/authentication/
            // approval_prompt: force or auto. Use force to always show the authorization prompt even if the user has already authorized the current application, default is auto.
            string authorizationRequest = $"{AuthorizationEndpointUri}?response_type=code&approval_prompt=auto" +
                $"&scope={Uri.EscapeDataString("read,activity:write")}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectURI)}" +
                $"&client_id={_clientID}" +
                $"&state={state}" +
                $"&code_challenge={code_challenge}" +
                $"&code_challenge_method={code_challenge_method}";

            // Opens request in the browser.
            // In original example they call System.Diagnostics.Process.Start(authorizationRequest), but it doesn't work - see https://github.com/dotnet/runtime/issues/28005#issuecomment-442214248
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = authorizationRequest,
                UseShellExecute = true
            };
            Process.Start(psi);

            // Waits for the OAuth authorization response.
            var context = await http.GetContextAsync();

            // Brings the Console to Focus.
            BringConsoleToFront();

            // Sends an HTTP response to the browser.
            var response = context.Response;
            string responseString = "<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>You can close this window now. Return to the app to continue.</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
            {
                responseOutput.Close();
                http.Stop();
                _logger.LogInformation("HTTP server stopped.");
            });

            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                _logger.LogError(string.Format(CultureInfo.InvariantCulture, "OAuth authorization error: {0}.", context.Request.QueryString.Get("error")));
                return;
            }
            if (context.Request.QueryString.Get("code") == null
                || context.Request.QueryString.Get("state") == null)
            {
                _logger.LogError("Malformed authorization response. " + context.Request.QueryString);
                return;
            }

            // extracts the code
            var code = context.Request.QueryString.Get("code");
            var incoming_state = context.Request.QueryString.Get("state");

            // Compares the received state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incoming_state != state)
            {
                _logger.LogError(string.Format(CultureInfo.InvariantCulture, "Received request with invalid state ({0})", incoming_state));
                return;
            }
            _logger.LogInformation("Authorization code: " + code);

            // Starts the code exchange at the Token Endpoint.
            await PerformCodeExchangeAsync(code, code_verifier, redirectURI);

            Console.WriteLine("The access token has been acquired.");
        }

        // ref http://stackoverflow.com/a/3978040
        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private async Task PerformCodeExchangeAsync(string code, string code_verifier, string redirectURI)
        {
            _logger.LogInformation("Exchanging code for tokens...");

            // builds the  request
            string tokenRequestBody = $"code={code}&redirect_uri={Uri.EscapeDataString(redirectURI)}&client_id={_clientID}&code_verifier={code_verifier}&client_secret={_clientSecret}&scope=&grant_type=authorization_code";

            // sends the request
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(TokenRequestUri);
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            byte[] _byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);
            tokenRequest.ContentLength = _byteVersion.Length;
            Stream stream = tokenRequest.GetRequestStream();
            await stream.WriteAsync(_byteVersion, 0, _byteVersion.Length);
            stream.Close();

            try
            {
                // gets the response
                WebResponse tokenResponse = await tokenRequest.GetResponseAsync();
                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();
                    _logger.LogTrace(responseText);

                    // converts to dictionary
                    var data = (JObject)JsonConvert.DeserializeObject(responseText);
                    _accessToken = data["access_token"].Value<string>();
                    _refreshToken = data["refresh_token"].Value<string>();

                    DateTime jan1970 = Convert.ToDateTime("1970-01-01T00:00:00Z", CultureInfo.InvariantCulture);
                    _expiresAt = jan1970.AddSeconds(data["expires_at"].Value<long>());

                    _logger.LogInformation("The tokens have been acquired.");
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        _logger.LogInformation("HTTP: " + response.StatusCode);
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            // reads response body
                            string responseText = await reader.ReadToEndAsync();
                            _logger.LogInformation(responseText);
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        private static string RandomDataBase64url(uint length)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[length];
                rng.GetBytes(bytes);

                return ToBase64urlencodeNoPadding(bytes);
            }
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputStirng"></param>
        /// <returns></returns>
        private static byte[] ToSHA256(string inputStirng)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputStirng);
            using (SHA256Managed sha256 = new SHA256Managed())
            {
                return sha256.ComputeHash(bytes);
            }
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static string ToBase64urlencodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        // Hack to bring the Console window to front.
        // ref: http://stackoverflow.com/a/12066376

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static void BringConsoleToFront()
        {
            SetForegroundWindow(GetConsoleWindow());
        }

        #endregion
    }
}
