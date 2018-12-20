using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace WelcomeToMyWorld {
    public class VrcApi {
        HttpClient Client { get; }
        static readonly string ApiKey = "JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26";
        static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        
        public VrcApi(string username, string password) {
            Client = new HttpClient { BaseAddress = new Uri("https://api.vrchat.cloud/api/1/") };
            Client.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"))}");
        }

        public async Task<World> GetWorld(string id) => await GetAsync<World>($"worlds/{id}");
        public async Task<WorldInstance> GetWorldInstance(string worldId, string instanceId) => await GetAsync<WorldInstance>($"worlds/{worldId}/{instanceId}");

        async Task<T> GetAsync<T>(string url) {
            var response = await Client.GetAsync($"{url}?apiKey={ApiKey}");
            
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                var obj = JsonConvert.DeserializeObject<ErrorMessage>(body, JsonSerializerSettings);
                throw new ApiError(obj.Error.Message);
            }
            return JsonConvert.DeserializeObject<T>(body, JsonSerializerSettings);
        }
    }

    public class ApiError : Exception {
        public ApiError(string message) : base(message) { }
    }

    public class ErrorMessage {
        public ErrorContent Error { get; set; }
    }

    public class ErrorContent {
        public string Message { get; set; }
    }

    public class World {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int TotalLikes { get; set; }
        public int TotalVisits { get; set; }
        public ReleaseStatus ReleaseStatus { get; set; }
        public string ImageUrl { get; set; }
        public string ThumbnailImageUrl { get; set; }
        [JsonProperty(PropertyName = "instances")]
        public List<JArray> InstanceArrays { get; set; }
        [JsonIgnore]
        public List<WorldInstanceSummary> Instances {
            get {
                return _Instances ?? (_Instances = InstanceArrays.Select(arr => new WorldInstanceSummary { Id = (string)arr[0], Occupants = (int)arr[1] }).ToList());
            }
        }
        List<WorldInstanceSummary> _Instances;
        public int Occupants { get; set; }
    }

    public class WorldInstanceSummary {
        public string Id { get; set; }
        public int Occupants { get; set; }
    }

    public enum ReleaseStatus {
        Public,
        Private,
        All,
        Hidden,
    }

    public class WorldInstance {
        public string Id { get; set; }
        public string Name { get; set; }
        [JsonProperty(PropertyName = "private")]
        public object PrivateTmp { get; set; }
        [JsonProperty(PropertyName = "friends")]
        public object FriendsTmp { get; set; }
        [JsonProperty(PropertyName = "users")]
        public object UsersTmp { get; set; }
        public List<User> Private { get => _Private ?? (_Private = GetUsers(PrivateTmp)); }
        List<User> _Private;
        public List<User> Friends { get => _Friends ?? (_Friends = GetUsers(FriendsTmp)); }
        List<User> _Friends;
        public List<User> Users { get => _Users ?? (_Users = GetUsers(UsersTmp)); }
        List<User> _Users;
        public string Hidden { get; set; }
        public string Nonce { get; set; }

        List<User> GetUsers(object val) =>
            val is JArray ?
            (val as JArray).Select(item => item.ToObject<User>()).ToList() :
            new List<User>();
    }

    public class User {
        public string Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string CurrentAvatarImageUrl { get; set; }
        public string CurrentAvatarThumbnailImageUrl { get; set; }
        public List<string> Tags { get; set; }
        public string DeveloperType { get; set; }
        public string Status { get; set; }
        public string StatusDescription { get; set; }
    }
}
