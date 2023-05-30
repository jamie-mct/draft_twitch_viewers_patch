using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace DraftTwitchViewers
{
    public class Chatter
    {
        public static Dictionary<string, Cacheable<PaginatedArray<Chatter>>> ChattersCache;
        public string ID { get; private set; }
        public string LoginName { get; private set; }
        public string DisplayName { get; private set; }

        public Chatter(string str)
        {
            ID = str;
            LoginName = str;
            DisplayName = str;
        }
        public Chatter(string id, string login_name, string user_name)
        {
            ID = id;
            LoginName = login_name;
            DisplayName = user_name;
        }
        public void PopulateFromJSON(object ar)
        {
            Hashtable table = ar as Hashtable;

            try { this.ID = (string)table["ID"]; } catch { }
            try { this.LoginName = (string)table["LoginName"]; } catch { }
            try { this.DisplayName = (string)table["DisplayName"]; } catch { }

        }
        private static void EnsureCache()
        {
            if (ChattersCache == null)
            {
                ChattersCache = new Dictionary<string, Cacheable<PaginatedArray<Chatter>>>();
            }
        }

        public static void ClearCache()
        {
            ChattersCache = new Dictionary<string, Cacheable<PaginatedArray<Chatter>>>();
        }

        public static IEnumerator GetAllChatters(string userId, string accessToken, Action<PaginatedArray<Chatter>> onSuccess, Action<Error> onFailure)
        {
            yield return GetAllChatters(userId, accessToken, 60, onSuccess, onFailure);
        }

        public static IEnumerator GetAllChatters(string userId, string accessToken, double cacheTime, Action<PaginatedArray<Chatter>> onSuccess, Action<Error> onFailure)
        {
            EnsureCache();

            Cacheable<PaginatedArray<Chatter>> cachedChatters;
            if (ChattersCache.TryGetValue(userId, out cachedChatters))
            {
                if (DateTime.UtcNow < cachedChatters.CachedAt.AddSeconds(cachedChatters.CacheSeconds))
                {
                    onSuccess.Invoke(cachedChatters.Data);
                    yield break;
                }
                ChattersCache.Remove(userId);
            }

            // Make a main array which starts out with a zero total until the first request. As each request completes, its page is merged into this main array.
            // Technically, chatter data can move around, grow, or shrink on the Twitch side throughout this process, possibly even resulting in duplicate
            // chatters appearing in the list. But I've already overengineered the hecc out of this, and this is just a mod, so let's not worry about that.
            PaginatedArray<Chatter> chatters = new PaginatedArray<Chatter>();
            string url = $"https://api.twitch.tv/helix/chat/chatters?broadcaster_id={userId}&moderator_id={userId}&first=1000";
            do
            {
                string pageUrl = url;
                if (chatters.Pagination.Cursor != "")
                {
                    pageUrl += $"&after={chatters.Pagination.Cursor}";
                }
                UnityWebRequest request = new UnityWebRequest(pageUrl);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                request.SetRequestHeader("Client-Id", ScenarioDraftManager.clientID);
                yield return request.SendWebRequest();

                if (request.responseCode != 200)
                {
                    var obj = JSON.JsonDecode(request.downloadHandler.text);
                    Error er = new Error(obj);
                    onFailure.Invoke(er);
                    //onFailure.Invoke(JsonConvert.DeserializeObject<Error>(request.downloadHandler.text));
                    yield break;
                }
                var HTList = JSON.DumpJSONData( request.downloadHandler.text);
                var datadict = HTList.Cast<DictionaryEntry>().ToDictionary(d => d.Key, d => d.Value);
                Dictionary<string, object> dict = new System.Collections.Generic.Dictionary<string, object>();

                foreach (var d in datadict)
                {
                    if (d.Value is ArrayList)
                    {
                        ArrayList arnew = (ArrayList)d.Value;
                        string ID = "", LoginName = "", DisplayName = "";
                        foreach (object obj in arnew)
                        {
                            Hashtable hstemp = (Hashtable)obj;
                            IDictionaryEnumerator ienum = hstemp.GetEnumerator();
                            while (ienum.MoveNext())
                            {
                                //dict.Add((string)ienum.Key, ienum.Value);
                                switch ((string)ienum.Key)
                                {
                                    case "user_id":
                                        ID = (string)ienum.Value;
                                        break;
                                    case "user_login":
                                        LoginName = (string)ienum.Value;
                                        break;
                                    case "user_name":
                                        DisplayName = (string)ienum.Value;
                                        break;
                                }
                                if (ID != "" && LoginName != "" && DisplayName != "")
                                {
                                    Chatter chatter = new Chatter(ID, LoginName, DisplayName);
                                    chatters.Data.Add(chatter);
                                    ID = LoginName = DisplayName = "";
                                }
                            }
                        }
                    }
                }

            } while (chatters.Data.Count <= chatters.Total && chatters.Pagination.Cursor != null && chatters.Pagination.Cursor != "");

            // Because whatever total Twitch gave last might not reflect the final list, set the total to the chatter list size.
            chatters.Total = chatters.Data.Count;
            Cacheable<PaginatedArray<Chatter>> cache = new Cacheable<PaginatedArray<Chatter>>(chatters, cacheTime);
            ChattersCache.Add(userId, cache);

            onSuccess.Invoke(chatters);
        }
    }
}
