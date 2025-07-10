
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

namespace Elements.Client
{

    public class ElementsClient
    {

        private static Api.DefaultApi api;

        private static Model.Session session;

        private static bool shouldCacheSession;

        private static bool initialized;

        public static Api.DefaultApi Api
        {
            get
            {

                if (api == null)
                {
                    Debug.LogError("Api not initliazed.You must call Elements.Initialize with the root url of your Elements instance first.");
                }

                return api;
            }
        }

        /// <summary>
        /// Initializes the API with the rool url.
        /// Will save session data to Application.persistentDataPath/Application.productName/ElementsSessionCache.json
        /// </summary>
        /// <param name="rootUrl">The root url of the instance of Elements that you are running.</param>
        public static void Initialize(string rootUrl)
        {
            Initialize(rootUrl, true);
        }

        /// <summary>
        /// Initializes the API with the rool url.
        /// Will save session data to Application.persistentDataPath/Application.productName/ElementsSessionCache.json
        /// </summary>
        /// <param name="rootUrl">The root url of the instance of Elements that you are running.</param>
        /// <param name="cacheSession">If true, will attempt to load the session data from, and save it to disk as appropriate.
        /// If not, the session will be only be stored in memory and you will have to log in every time unless you manually store the session yourself.</param>
        public static void Initialize(string rootUrl, bool cacheSession)
        {            
            api = new Api.DefaultApi(rootUrl);
            api.Configuration.DefaultHeaders["Content-Type"] = "application/json";
            api.ApiClient.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            api.ApiClient.SerializerSettings.Error += (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e) => e.ErrorContext.Handled = true;

            shouldCacheSession = cacheSession;

            if (cacheSession)
            {
                LoadSessionData();
            }

            initialized = true;
        }

        /// <summary>
        /// Adds the session header to subsequent requests
        /// </summary>
        /// <param name="session">The SessionCreation object returned by the Elements server.</param>
        public static void SetSession(Model.SessionCreation sessionCreation)
        {
            Api.Configuration.ApiKey["Elements-SessionSecret"] = sessionCreation.SessionSecret;

            session = sessionCreation.Session;

            if (shouldCacheSession)
            {
                SaveSessionData(sessionCreation);
            }
        }

        /// <summary>
        /// Determines if an non-expired session has been loaded into memory.
        /// </summary>
        /// <returns>true if the session is active</returns>
        public static bool IsSessionActive()
        {
            return session != null && session.Expiry > System.DateTime.UtcNow.Millisecond;
        }

        /// <summary>
        /// Returns a value indicating if ElementsClient.Initialize has been called before.
        /// </summary>
        /// <returns>true if ElementsClient has been initialized</returns>
        public static bool IsInitialized()
        {
            return initialized;
        }

        /// <summary>
        /// Removes the session token from the Api Configuration headers and clears the session in memory.
        /// </summary>
        public static void ClearSession()
        {
            session = null;
            Api.Configuration.ApiKey.Remove("Elements-SessionSecret");            
        }

        /// <summary>
        /// Clears the session from memory and deletes it from disk.
        /// </summary>
        public static void LogOut()
        {
            ClearSession();
            DeleteSessionData();
        }

        public static Model.Session GetSession()
        {
            return session;
        }

        private static string GetSessionCachePath()
        {
            return Path.Combine(Application.persistentDataPath, "ElementsSessionCache.json");
        }

        private static void SaveSessionData(Model.SessionCreation session)
        {
            var json = JsonConvert.SerializeObject(session);

            File.WriteAllText(GetSessionCachePath(), json);
        }

        private static void LoadSessionData()
        {
            var path = GetSessionCachePath();

            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                var sessionCreation = JsonConvert.DeserializeObject<Model.SessionCreation>(text);

                session = sessionCreation.Session;
                Api.Configuration.ApiKey.Add("Elements-SessionSecret", sessionCreation.SessionSecret);
            }
        }

        private static void DeleteSessionData()
        {
            var path = GetSessionCachePath();

            if (File.Exists(path))
            {
                File.Delete(path);
            }

        }

    }
}
