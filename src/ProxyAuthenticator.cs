using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;

namespace CrmWebApiProxy
{
    public class CRMProxyAuthenticator : ICRMAuthenticator
    {
        private readonly UserCredentails _creds;
        private readonly ProxyConfig _config;

        private CRMProxyAuthenticator(ProxyConfig config, UserCredentails creds)
        {
            _creds = creds;
            _config = config;
        }

        public static CRMProxyAuthenticator Create(IOptions<ProxyConfig> options)
        {
            var credBldr = new CredentialBuilder();

            var config = options.Value;
            var ucred = new UserCredentails(config);
            for (; ; )
            {
                Log.Info(" ");
                Log.Info(" --- --- --- ");
                var rx = credBldr.GetCredentials();
                try
                {
                    Log.Info("Authenticating ...");
                    ucred.Authenticate(rx.Item1, rx.Item2);
                    return new CRMProxyAuthenticator(config, ucred);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    Log.Info("Press any key to continue");
                    Console.ReadKey();
                }
            }
        }

        public string GetAccessToken()
        {
            return _creds.GetAccessToken();
        }

        public string UserName => _creds.DisplayName;
        public Guid UserId { get; private set; }
        public Guid BusinessUnitId { get; private set; }

        public void EnsureConnection()
        {
            using (var hc = new HttpClient
            {
                BaseAddress = new Uri($"{_config.CrmHostUri}/"),
                Timeout = new TimeSpan(0, 2, 0)
            })
            {
                hc.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                hc.DefaultRequestHeaders.Add("OData-Version", "4.0");
                hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetAccessToken());
                // ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                var retrieveResponse = hc.GetAsync($"{_config.WebApiUri}/WhoAmI").GetAwaiter().GetResult();
                if (retrieveResponse.IsSuccessStatusCode)
                {
                    var jRetrieveResponse = JObject.Parse(retrieveResponse.Content.ReadAsStringAsync().Result);
                    UserId = (Guid)jRetrieveResponse["UserId"];
                    BusinessUnitId = (Guid)jRetrieveResponse["BusinessUnitId"];
                }
            }
        }

        private class UserCredentails
        {
            private readonly IPublicClientApplication _clientApp;
            private readonly ProxyConfig _config;
            private readonly string _authorityUrl;

            private readonly string[] _scopes;

            public UserCredentails(ProxyConfig config)
            {
                _config = config;
                _scopes = new[] { $"{_config.CrmHostUri}/user_impersonation" };
                _authorityUrl = $"{_config.ContextAuthority}/{_config.TenantId}";
                _clientApp = PublicClientApplicationBuilder.Create(_config.ClientId)
                                                  .WithAuthority(_authorityUrl)
                                                  .Build();
            }

            public string DisplayName { get; private set; }
            public string UserName { get; private set; }
            public SecureString Password { get; private set; }

            private bool IsValid()
            {
                return !string.IsNullOrEmpty(UserName)
                    && Password != null && Password.Length > 0;
            }

            public void Authenticate(string username, SecureString secPassword)
            {
                UserName = username;
                Password = secPassword;
                Authenticate();
            }

            private void Authenticate()
            {
                //var accounts = _clientApp.GetAccountsAsync()
                //    .GetAwaiter().GetResult();
                var result = _clientApp.AcquireTokenByUsernamePassword(_scopes, UserName, Password)
                    .ExecuteAsync().GetAwaiter().GetResult();
                if (result == null)
                    throw new Exception("Failed to authenticate!");
                DisplayName = result.Account.Username;
                _token = result.AccessToken;
                _expiresOn = result.ExpiresOn;
            }

            private string _token;
            private DateTimeOffset _expiresOn;

            public string GetAccessToken()
            {
                if (string.IsNullOrEmpty(_token))
                    Authenticate();
                return _token;
            }
        }
    }
}