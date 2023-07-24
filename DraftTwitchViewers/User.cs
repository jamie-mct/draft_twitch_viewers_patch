using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
//using Newtonsoft.Json;

namespace DraftTwitchViewers
{    struct User
    {
        public enum QueryBy
        {
            Id,
            LoginName,
            AccessToken
        }

        //[JsonProperty(PropertyName = "id")]
        public string Id { get; private set; }

        //[JsonProperty(PropertyName = "login")]
        public string LoginName { get; private set; }

        //[JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; private set; }

        public static IEnumerator GetFirstUser(string query, QueryBy queryBy, Action<User> onSuccess, Action<Error> onFailure)
        {
            Logger.LogInfo("GetFirstUser 1");

            string url = "https://api.twitch.tv/helix/users";
            UnityWebRequest request = new UnityWebRequest();

            switch (queryBy)
            {
                case QueryBy.Id:
                    url += $"?id={query}";
                    break;
                case QueryBy.LoginName:
                    url += $"?login={query}";
                    break;
                case QueryBy.AccessToken:
                    request.SetRequestHeader("Authorization", $"Bearer {query}");
                    break;
            }
            request.SetRequestHeader("Client-Id", ScenarioDraftManager.clientID);
            request.url = url;
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.responseCode != 200)
            {
                JSON.DumpJSONData( request.downloadHandler.text);
                //onFailure.Invoke(JsonConvert.DeserializeObject<Error>(request.downloadHandler.text));
            }

            var HTList = JSON.DumpJSONData( request.downloadHandler.text);
            var datadict = HTList.Cast<DictionaryEntry>().ToDictionary(d => d.Key, d => d.Value);
            Dictionary<string,object> dict = new System.Collections.Generic.Dictionary<string, object>();

            foreach(var d in datadict)
            {
                if (d.Value is ArrayList)
                {
                    ArrayList arnew = (ArrayList)d.Value;
                    foreach (object obj in arnew)
                    {
                        Hashtable hstemp = (Hashtable)obj;
                        IDictionaryEnumerator ienum = hstemp.GetEnumerator();
                        while (ienum.MoveNext())
                        {
                            dict.Add((string)ienum.Key, ienum.Value);
                        }
                    }
                }
            }
            User user = new User();
            user.Id = dict["id"].ToString();
            user.DisplayName = dict["display_name"].ToString();
            Logger.LogInfo("GetFirstUser 2");

            onSuccess.Invoke(user);
            Logger.LogInfo("GetFirstUser 3");

        }
    }
}
