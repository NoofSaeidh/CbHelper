using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestHelp
{
    public class AcumaticaRestClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly EndpointInfo _endpointInfo;
        public AcumaticaRestClient(EndpointInfo endpointInfo)
        {
            _endpointInfo = endpointInfo
                ?? throw new ArgumentNullException(nameof(endpointInfo));


            _httpClient = new HttpClient(
                new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new CookieContainer(),
                })
            {
                BaseAddress = new Uri(_endpointInfo.BaseUrl + $"/entity/{endpointInfo.Name}/{endpointInfo.Version}/"),
                DefaultRequestHeaders =
                {
                    Accept = { MediaTypeWithQualityHeaderValue.Parse("text/json") }
                },
                Timeout = TimeSpan.FromHours(1)
            };
        }

        public LoginInfo LoginSettings { get; }

        public void Login(LoginInfo loginInfo)
        {
            var response = _httpClient.PostAsync(_endpointInfo.BaseUrl + "/entity/auth/login", loginInfo.ToJsonContent()).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new RestException(response);
            }
        }

        public void Logout()
        {
            _httpClient.PostAsync(_endpointInfo.BaseUrl + "/entity/auth/logout", new ByteArrayContent(new byte[0])).Wait();
        }

        //public JToken Get(string entityName, object entity) => Get(KeyAttribute)


        public JToken Get(string entityName, string parameters) => Get(entityName + '?' + parameters).Content.ConvertToString().ToJToken();

        public JToken GetByKeys(string entityName, string keys, string parameters) => Get(entityName + '/' + keys + '?' + parameters).Content.ConvertToString().ToJToken();

        public JToken Put(string entityName, object entity) => Put(entityName, entity.ToJsonContent()).Content.ConvertToString().ToJToken();

        public JToken Put(string entityName, object entity, string parameters) => Put(entityName + '?' + parameters, entity.ToJsonContent()).Content.ConvertToString().ToJToken();

        public string Invoke(string entityName, string actionName, object input)
        {
            var result = Post(entityName + '/' + actionName, input.ToJsonContent());
            const int timeout = 1000000;
            var dt = DateTime.Now;
            while (true)
            {
                switch (result.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return "No content";
                    case HttpStatusCode.Accepted:
                        if ((DateTime.Now - dt).TotalMilliseconds > timeout) throw new TimeoutException();
                        Thread.Sleep(500);
                        result = _httpClient.GetAsync(result.Headers.Location).Result.CheckStatus();
                        continue;
                    default:
                        throw new InvalidOperationException("Invalid process result: " + result.StatusCode);
                }
            }
        }

        private HttpResponseMessage Get(string address) =>  _httpClient.GetAsync(_httpClient.BaseAddress + address).Result.CheckStatus();

        private HttpResponseMessage Post(string address, StringContent content) => _httpClient.PostAsync(_httpClient.BaseAddress + address, content).Result.CheckStatus();

        private HttpResponseMessage Post(string address) => _httpClient.PostAsync(_httpClient.BaseAddress + address, new ByteArrayContent(new byte[0])).Result.CheckStatus();

        private HttpResponseMessage Put(string address, StringContent content)
        {
            return _httpClient.PutAsync(_httpClient.BaseAddress + address, content).Result.CheckStatus();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }

    public class LoginInfo
    {
        public LoginInfo(string user, string password, string company = null, string branch = null)
        {
            Username = user ?? throw new ArgumentNullException(nameof(user));
            Password = password ?? throw new ArgumentNullException(nameof(password));
            Company = company;
            Branch = branch;
        }

        [JsonProperty("name")]
        public string Username { get; }
        public string Password { get; }
        public string Company { get; }
        public string Branch { get; }
    }

    public class EndpointInfo
    {
        public EndpointInfo(string baseUrl, string name, string version)
        {
            BaseUrl = (baseUrl ?? throw new ArgumentNullException(nameof(baseUrl))).Trim('/');
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        public string BaseUrl { get; }
        public string Name { get; }
        public string Version { get; }
    }

    public static class Extensions
    {
        public static StringContent ToJsonContent(this string value)
        {
            return new StringContent(value, Encoding.UTF8, "application/json");
        }

        public static StringContent ToJsonContent(this object value)
        {
            return value.ToJson().ToJsonContent();
        }

        public static string ToJson(this object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public static string ConvertToString(this HttpContent content)
        {
            return content.ReadAsStringAsync().Result;
        }

        public static JToken ToJToken(this string value)
        {
            return JToken.Parse(value);
        }

        public static HttpResponseMessage CheckStatus(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new RestException(response);
            }
            return response;
        }
    }

    public class RestException : Exception
    {
        private readonly HttpResponseMessage _response;

        public RestException(HttpResponseMessage response)
        {
            _response = response;
        }

        private PXResponseMessage pxResponseMessage;
        public PXResponseMessage PXResponseMessage => pxResponseMessage ?? (pxResponseMessage = JsonConvert.DeserializeObject<PXResponseMessage>(_response.Content.ReadAsStringAsync().Result));


        private string message;
        public override string Message
        {
            get
            {
                if (message != null)
                    return message;

                return message =
$@"Reason: {_response.ReasonPhrase}
Request: {_response.RequestMessage.ToString().Replace("\r\n", "   ")}
Message: {PXResponseMessage.Message},
Exception Type: {PXResponseMessage.ExceptionType},
Exception Message: 

{PXResponseMessage.ExceptionMessage},

Stack Trace: 

{PXResponseMessage.StackTrace}";
            }
        }
    }

    public class PXResponseMessage
    {
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
    }



    public class LoginLogout : IDisposable
    {
        private readonly LoginInfo _loginInfo;
        private readonly AcumaticaRestClient _restClient;
        public LoginLogout(AcumaticaRestClient restClient, LoginInfo loginInfo)
        {
            _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
            _loginInfo = loginInfo ?? throw new ArgumentNullException(nameof(loginInfo));

            _restClient.Login(loginInfo);
        }

        void IDisposable.Dispose()
        {
            _restClient.Logout();
        }
    }
}
