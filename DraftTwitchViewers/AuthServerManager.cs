using KSP.Localization;
using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace DraftTwitchViewers
{
    // Twitch authorization server for DYV 2. Why is this required? Because getting a list of users in a chat apparently requires the streamer's
    // access token, and I'm not paying for a proper server to do this properly for one tiny little OAuth2 flow that 12 people will use.

    // So what's this do? When the streamer needs to authorize DYV2, this starts a local server and waits until the server gets Twitch's
    // redirect or until the user cancels. If the redirect contains a Twitch access token, it passes it back via Actions.
    public class AuthServerManager : MonoBehaviour
    {
        private Thread serverThread;
        private HttpListener serverListener;

        private string csrfPreventionToken;
        private Action<string> authCompleteCallback;
        private volatile string authTokenReceived;

#if false
        public void  Start()
        {
            Logger.LogInfo("AuthServerManager.Start");  // NO_LOCALIZATION
        }
        private void OnDestroy()
        {
            Logger.LogInfo("AuthServerManager.OnDestroy"); // NO_LOCALIZATION
        }
#endif

        private void FixedUpdate()
        {
            if (serverThread == null || serverThread.IsAlive) { return; }
            serverThread = null;

            string authToken = authTokenReceived;
            authTokenReceived = "";
            if (authToken == "") 
            {
                Logger.LogInfo("authToken is empty, exiting"); // NO_LOCALIZATION
                return; 
            }

            if (authCompleteCallback != null)
            {
                //Logger.LogInfo($"GOT ACCESS TOKEN \"{authToken}\"");
                StartCoroutine(StopServerLater());
                authCompleteCallback.Invoke(authToken);
            }
        }

        private IEnumerator StopServerLater()
        {
            //Logger.LogInfo("StopServerLater");
            yield return new WaitForSeconds(1f);
            serverListener.Stop();
        }

        public void StartAuthRedirectServer(string csrfPreventionToken, Action<string> authCompleteCallback)
        {
            this.csrfPreventionToken = csrfPreventionToken;
            this.authCompleteCallback = authCompleteCallback;
            serverListener = new HttpListener();
            serverListener.Prefixes.Add("http://localhost:2551/"); // NO_LOCALIZATION
            serverListener.Start();
            serverThread = new Thread(ListenForAuth);
            serverThread.Start();
        }

        public void CancelAuth()
        {
            serverThread.Abort();
            serverListener.Stop();
        }

        private void ListenForAuth()
        {
            while (true)
            {
                HttpListenerContext ctx = serverListener.GetContext();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse res = ctx.Response;

                            string str = Localizer.Format("#LOC_DTW_1");
                switch (req.Url.AbsolutePath)
                {
                    case "/authorize":
                        if (req.HttpMethod != "GET") // NO_LOCALIZATION
                        {
                            ErrorPage(res, 405, str + $" {req.HttpMethod} KEKW");  // NO_LOCALIZATION
                            break;
                        }
                        SuccessPage(res);
                        break;
                    case "/data":
                        if (req.HttpMethod != "GET") // NO_LOCALIZATION
                        {
                            ErrorResponse(res, 405, str + $" {req.HttpMethod} KEKW"); // NO_LOCALIZATION
                            break;
                        }
                        if (req.QueryString["state"] != csrfPreventionToken) // NO_LOCALIZATION
                        {
                            ErrorResponse(res, 403, Localizer.Format("#LOC_DTW_2"));
                            break;
                        }
                        if (req.QueryString["access_token"] == null) // NO_LOCALIZATION
                        {
                            ErrorResponse(res, 400, Localizer.Format("#LOC_DTW_3"));
                            break;
                        }
                        authTokenReceived = string.Copy(req.QueryString["access_token"]); // NO_LOCALIZATION
                        SuccessResponse(res);
                        return;
                    default:
                        str = Localizer.Format("#LOC_DTW_4");
                        string str2 = Localizer.Format("#LOC_DTW_5");
                        ErrorPage(res, 404,str + $"{req.Url.AbsolutePath}" + str2); // NO_LOCALIZATION
                        break;
                }
            }
        }

        private void SuccessPage(HttpListenerResponse res)
        {
            byte[] content = Encoding.UTF8.GetBytes(string.Format(successTemplate, header));
            res.ContentType = "text/html"; // NO_LOCALIZATION
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = content.LongLength;
            res.OutputStream.Write(content, 0, content.Length);
            res.Close();
        }

        private void SuccessResponse(HttpListenerResponse res)
        {
            byte[] content = Encoding.UTF8.GetBytes("ok"); // NO_LOCALIZATION
            res.ContentType = "text/plain"; // NO_LOCALIZATION
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = content.LongLength;
            res.OutputStream.Write(content, 0, content.Length);
            res.Close();
        }

        private void ErrorPage(HttpListenerResponse res, int status, string error)
        {
            byte[] content = Encoding.UTF8.GetBytes(string.Format(bodyTemplate, header, status, error));
            res.StatusCode = status;
            res.ContentType = "text/html"; // NO_LOCALIZATION
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = content.LongLength;
            res.OutputStream.Write(content, 0, content.Length);
            res.Close();
        }

        private void ErrorResponse(HttpListenerResponse res, int status, string error)
        {
            byte[] content = Encoding.UTF8.GetBytes($"{status} - {error}"); // NO_LOCALIZATION
            res.StatusCode = status;
            res.ContentType = "text/plain"; // NO_LOCALIZATION
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = content.LongLength;
            res.OutputStream.Write(content, 0, content.Length);
            res.Close();
        }

        #region NO_LOCALIZATION
        // HTML stuff aaaaaall the way down here so it doesn't clutter the actual code.
        private const string header = @"<head>
        <title>Draft Your Viewers 2</title>
        <style>
            html,body {
                border: 0;
                background-color: #22262e;
                color: #d6e0ff;
                font-family: Roboto, Helvetica, sans-serif;
            }
            .bordered {
                width: 600px;
                margin: 48px auto;
                background-color: #2e3540;
                border: 4px solid #10182c;
                border-radius: 12px;
            }
            .padded {
                padding: 12px;
            }
            .centered {
                text-align: center;
            }
        </style>
        <script src='https://code.jquery.com/jquery-3.6.3.min.js' integrity='sha256-pvPw+upLPUjgMXY0G+8O0xUf+/Im1MZjXxxgOcBQBXU=' crossorigin='anonymous'></script>
        <script>
            $(document).ready(() => {
                $.ajax({
                    url: `/data?${$(location).attr('hash').substring(1)}`,
                    success: (data) => {
                        $('#header').text('Success!');
                        $('#detail').text('You may now close this page and return to the game.');
                    },
                    error: (xhr) => {
                        $('#header').text(`Well that's not right...`);
                        $('#detail').text(`${xhr.status} - ${xhr.responseText}`);
                    }
                });
            });
        </script>
    </head>";

        private const string successTemplate = @"
<html>
    {0}
    <body>
        <section class='bordered padded centered'>
            <h1 id='header'>One sec...</h1>
        </section>
        <section class='bordered padded centered'>
            <p id='detail'>Authorizing...</p>
        </section>
    </body>
</html>
";

        private const string bodyTemplate = @"
<html>
    {0}
    <body>
        <section class='bordered padded centered'>
            <h1 class='header'>Well that's not right...</h1>
        </section>
        <section class='bordered padded centered'>
            <p>{1} - {2}</p>
        </section>
    </body>
</html>
";
    }
}
#endregion
