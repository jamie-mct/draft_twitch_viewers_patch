using KSP.Localization;
using ClickThroughFix;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using ToolbarControl_NS;
using UnityEngine;

namespace DraftTwitchViewers
{
    /// <summary>
    /// The Draft Manager App. This app is used to connect to twitch and draft users into the game as Kerbals.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class DraftManagerApp : MonoBehaviour
    {
        #region Variables

        /// <summary>
        /// The instance of this object.
        /// </summary>
        internal static DraftManagerApp instance;

        #region UI Management

        /// <summary>
        /// The App Launcher Button
        /// </summary>
        //private ApplicationLauncherButton draftManagerButton;
        ToolbarControl toolbarControl;

        /// <summary>
        /// Is the game UI hidden?
        /// </summary>
        private bool isUIHidden = false;
        /// <summary>
        /// Is the app showing?
        /// </summary>
        private bool isShowing = false;
        /// <summary>
        /// Is the app button being hovered over?
        /// </summary>
        private bool isHovering = false;
        /// <summary>
        /// Is  the user customizing?
        /// </summary>
        private bool isCustomizing = false;
        /// <summary>
        /// The window bounds.
        /// </summary>
        private Rect windowRect;
        /// <summary>
        /// The alert bounds.
        /// </summary>
        private Rect alertRect;
        /// <summary>
        /// The width of the window.
        /// </summary>
        private float windowWidth = 350;
        /// <summary>
        /// The height of the window.
        /// </summary>
        private float windowHeight = 250f;
        /// <summary>
        /// Is the draft failure alert showing?
        /// </summary>
        private bool alertShowing = false;
        /// <summary>
        /// The message the alert is showing for.
        /// </summary>
        private string alertingMsg = "";
        /// <summary>
        /// Did the draft fail?
        /// </summary>
        private bool failedToDraft = false;
        /// <summary>
        /// Is the draft busy?
        /// </summary>
        private bool draftBusy = false;

        #endregion

        #region Audio

        /// <summary>
        /// The AudioSource played when a draft is started.
        /// </summary>
        private AudioSource startClip;
        /// <summary>
        /// The AudioSource played when a draft succeeds.
        /// </summary>
        private AudioSource successClip;
        /// <summary>
        /// THe AudioSource played when a draft fails.
        /// </summary>
        private AudioSource failureClip;

        #endregion

        #region Settings

        /// <summary>
        /// Use the hotkey to draft?
        /// </summary>
        private bool useHotkey = true;
        /// <summary>
        /// Skin selection
        /// </summary>
        private bool useKSPSkin = true;

        /// <summary>
        /// Add the kerbal to the current craft when drafted?
        /// </summary>
        private bool addToCraft = false;
        /// <summary>
        /// The message used when a draft succeeds.
        /// </summary>
        private string draftMessage = "&user " + // NO_LOCALIZATION
            Localizer.Format("#LOC_DTW_6") +
            "&skill!"; // NO_LOCALIZATION
        /// <summary>
        /// The message used when a user is pulled in a drawing.
        /// </summary>
        private string drawMessage = "&user " + // NO_LOCALIZATION
            Localizer.Format("#LOC_DTW_7");

        #endregion

        #region Misc Variables

        /// <summary>
        /// The settings save location.
        /// </summary>
        private string settingsLocation { get { return KSPUtil.ApplicationRootPath + "GameData/DraftTwitchViewers/"; } }
        /// <summary>
        /// Has something changed that we need to save?
        /// </summary>
        private bool needSave = false;
        /// <summary>
        /// The current delay time for saving.
        /// </summary>
        private float currentSaveDelay = 1f;
        /// <summary>
        /// The max delay time for saving.
        /// </summary>
        private const float maxSaveDelay = 1f;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// UseHotkey property. Triggers autosave when changed.
        /// </summary>
        private bool UseHotkey
        {
            get { return useHotkey; }
            set { if (useHotkey != value) { useHotkey = value; SaveSettings(); } }
        }

        /// <summary>
        /// UseHotkey property. Triggers autosave when changed.
        /// </summary>
        private bool UseKSPSkin
        {
            get { return useKSPSkin; }
            set { if (useKSPSkin != value) { useKSPSkin = value; SaveSettings(); } }
        }

        /// <summary>
        /// AddToCraft property. Triggers autosave when changed.
        /// </summary>
        private bool AddToCraft
        {
            get { return addToCraft; }
            set { if (addToCraft != value) { addToCraft = value; SaveSettings(); } }
        }

        /// <summary>
        /// DraftMessage property. Triggers autosave (after delay) when changed.
        /// </summary>
        private string DraftMessage
        {
            get { return draftMessage; }
            set { if (draftMessage != value) { draftMessage = value; needSave = true; currentSaveDelay = 0f; } }
        }

        /// <summary>
        /// DrawMessage property. Triggers autosave (after delay) when changed.
        /// </summary>
        private string DrawMessage
        {
            get { return drawMessage; }
            set { if (drawMessage != value) { drawMessage = value; needSave = true; currentSaveDelay = 0f; } }
        }

        #endregion

        #region Unity Functions

        internal const string MODID = "DraftTwitchViewers_NS";
        internal const string MODNAME = "Draft Twitch Viewers";

        /// <summary>
        /// Called when the MonoBehavior is awakened.
        /// </summary>
        private void Awake()
        {
            // If there's already an instance, delete this instance.
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            // Do not destroy this instance.
            DontDestroyOnLoad(gameObject);
            // Save this instance so others can detect it.
            instance = this;

            #region NO_LOCALIZATION
            SoundManager.LoadSound("DraftTwitchViewers/Sounds/", "Start");
            SoundManager.LoadSound("DraftTwitchViewers/Sounds/Success", "Success");
            SoundManager.LoadSound("DraftTwitchViewers/Sounds/Failure", "Failure");
            startClip = SoundManager.CreateSound("Start", false);
            successClip = SoundManager.CreateSound("Success", false);
            failureClip = SoundManager.CreateSound("Failure", false);

            // Load global settings.
            ConfigNode globalSettings = ConfigNode.Load(settingsLocation + "GlobalSettings.cfg");
            #endregion

            // If the file exists,
            if (globalSettings != null)
            {
                #region Draft Settings Load
                #region NO_LOCALIZATION

                // Get the DRAFT node.
                ConfigNode draftSettings = globalSettings.GetNode("DRAFT");

                // If the DRAFT node exists,
                if (draftSettings != null)
                {
                    // Get the global settings.
                    if (draftSettings.HasValue("addToCraft")) { try { addToCraft = bool.Parse(draftSettings.GetValue("addToCraft")); } catch { } }
                }
                // If the DRAFT node doesn't exist,
                else
                {
                    // Log a warning that is wasn't found.
                    Logger.DebugWarning("GlobalSettings.cfg WAS found, but the DRAFT node was not. Using defaults.");
                }
                #endregion
                #endregion

                #region Message Settings Load
                #region NO_LOCALIZATION

                // Get the MESSAGES node.
                ConfigNode messageSettings = globalSettings.GetNode("MESSAGES");

                // If the MESSAGES node exists,
                if (messageSettings != null)
                {
                    // Get the global settings.
                    if (messageSettings.HasValue("draftMessage")) { draftMessage = messageSettings.GetValue("draftMessage"); }
                    if (messageSettings.HasValue("drawMessage")) { drawMessage = messageSettings.GetValue("drawMessage"); }
                }
                // If the DRAFT node doesn't exist,
                else
                {
                    // Log a warning that is wasn't found.
                    Logger.DebugWarning("GlobalSettings.cfg WAS found, but the MESSAGES node was not. Using defaults.");
                }
                #endregion
                #endregion
            }
            #region NO_LOCALIZATION
            // If the file doesn't exist,
            else
            {
                // Log a warning that it wasn't found.
                Logger.DebugWarning("GlobalSettings.cfg wasn't found. Using defaults.");
            }

            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(DisplayApp,
                       HideApp,
                       HoverApp,
                       UnhoverApp, DummyVoid,
                       Disable,
                ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT,
                MODID,
                "draftTwitchViewersButton",
                "DraftTwitchViewers/Textures/Toolbar-38",
                "DraftTwitchViewers/Textures/Toolbar-24",
                MODNAME
            );
            toolbarControl.AddLeftRightClickCallbacks(null, DoRightClick);


            // This app should be mutually exclusive. (It should disappear when the player clicks on another app.
            toolbarControl.EnableMutuallyExclusive();

            // Set up the window bounds.
            windowRect = new Rect(Screen.width - windowWidth, 40f, windowWidth, windowHeight);
            // Set up the alert bounds.
            alertRect = new Rect(Screen.width / 2 - windowWidth / 2, Screen.height / 2 - windowHeight / 4, windowWidth, 1f);

            // Set up app destroyer.
            GameEvents.onGameSceneLoadRequested.Add(DestroyApp);
            Logger.DebugLog("DTV App Created.");
        }
        #endregion

        static GameObject myplayer = new GameObject();

        /// <summary>
        /// Called when the MonoBehaviour is started.
        /// </summary>
        private void Start()
        {
            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onHideUI.Add(OnHideUI);


            myplayer.AddComponent<TextTyper>();
            StatusTextTyper = myplayer.GetComponent<TextTyper>();
            AppOnStreamerAuthenticated = new Action<string>(OnStreamerAuthenticated);

            authServerManager = myplayer.GetComponent<AuthServerManager>();

        }
        bool authWinVisible = true;
        void AuthWinWindow(int id)
        {
            using (new GUILayout.VerticalScope())
            {

                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(Localizer.Format("#LOC_DTW_8"), GUILayout.Height(40), GUILayout.Width(240)))
                    {
                        AuthorizeStreamer();
                        authWinVisible = false;
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(Localizer.Format("#LOC_DTW_9"));
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();
            }
        }

        public static AuthServerManager authServerManager;
        //private string streamerUserId;
        public Action<string> AppOnStreamerAuthenticated;

        string streamerDisplayName;
        public TextTyper StatusTextTyper;

        public void OnStreamerAuthenticated(string displayName)
        {
            streamerDisplayName = displayName;

            myplayer.AddComponent<TextTyper>();
            StatusTextTyper = myplayer.GetComponent<TextTyper>();

            StatusTextTyper.FullText = Localizer.Format("#LOC_DTW_10") +
                $"{streamerDisplayName}";  // NO_LOCALIZATION
            ScenarioDraftManager.Instance.AuthNeeded = false;
        }

        private void AuthorizeStreamer()
        {
            Logger.LogInfo("AuthorizeStreamer 1"); // NO_LOCALIZATION
            string csrfPreventionToken = Guid.NewGuid().ToString();
            if (authServerManager == null)
            {
                Logger.LogInfo("AuthorizeStreamer 2"); // NO_LOCALIZATION
                myplayer = new GameObject();
                myplayer.AddComponent<AuthServerManager>();
                authServerManager = myplayer.GetComponent<AuthServerManager>();

                if (authServerManager == null)
                {
                    Logger.LogError("Unable to get the AuthServerManager component"); // NO_LOCALIZATION
                    return;
                }
            }
            authServerManager.StartAuthRedirectServer(csrfPreventionToken, new Action<string>((string accessToken) =>
                {
                    ScenarioDraftManager.Instance.StreamerAccessToken = accessToken;
                    AuthenticateStreamer();
                }));
            Application.OpenURL($"https://id.twitch.tv/oauth2/authorize?client_id={ScenarioDraftManager.clientID}&redirect_uri=http://localhost:2551/authorize&response_type=token&scope=moderator:read:chatters&state={csrfPreventionToken}"); // NO_LOCALIZATION
        }

        private void AuthenticateStreamer()
        {
            Logger.LogInfo("AuthenticateStreamer 1"); // NO_LOCALIZATION
            if (ScenarioDraftManager.Instance.StreamerAccessToken == "")
            {
                ScenarioDraftManager.Instance.AuthNeeded = true;

                return;
            }
            Logger.LogInfo("AuthenticateStreamer 2"); // NO_LOCALIZATION

            // Web requests are an async process, and coroutines don't like giving back data, so Action callbacks must be used.
            StartCoroutine(
            User.GetFirstUser(
                     ScenarioDraftManager.Instance.StreamerAccessToken,
                    User.QueryBy.AccessToken,
                    new Action<User>((User user) =>
                    {
                        //Logger.LogInfo($"User {user.DisplayName} authenticated.");
                        ScenarioDraftManager.Instance.StreamerUserId = user.Id;
                        AppOnStreamerAuthenticated.Invoke(user.DisplayName);
                    }), new Action<Error>((Error err) =>
                    {
                        Logger.LogError($"Failed to authenticate with status {err.Status}: {err.ErrorText} - {err.Message}\nClearing config user data."); // NO_LOCALIZATION
                        /* Config. */
                        ScenarioDraftManager.Instance.StreamerAccessToken = "";
                        //FileManager.SaveDraftConfig(Config);
                    })
                )
            );
        }

        public void GetAllViewers(Action<PaginatedArray<Chatter>> onViewerDrafted, Action<string> onError, Predicate<Chatter> canUseViewer = null)
        {
            if (ScenarioDraftManager.Instance.StreamerUserId == "")
            {
                return;
            }

            // Web requests are an async process, and coroutines don't like giving back data, so Action callbacks must be used.
            StartCoroutine(
                Chatter.GetAllChatters(
                    ScenarioDraftManager.Instance.StreamerUserId,
                    ScenarioDraftManager.Instance.StreamerAccessToken,
                    new Action<PaginatedArray<Chatter>>((PaginatedArray<Chatter> chatters) =>
                    {
                        Logger.LogInfo($"{chatters.Data.Count} pulled."); // NO_LOCALIZATION

                        if (chatters.Data.Count == 0)
                        {
                            Logger.LogWarn($"Failed to pick viewer. Empty chat."); // NO_LOCALIZATION
                            onError.Invoke(Localizer.Format("#LOC_DTW_11"));
                            return;
                        }
                        onViewerDrafted.Invoke(chatters);
                    }), new Action<Error>((Error err) =>
                    {
                        Logger.LogError($"Failed to pick viewer with status {err.Status}: {err.ErrorText} - {err.Message}"); // NO_LOCALIZATION
                        onError.Invoke(Localizer.Format("#LOC_DTW_12"));
                    })
                )
            );
        }


        /// <summary>
        /// Called when Unity updates.
        /// </summary>
        void Update()
        {
            if (UseHotkey && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyUp(KeyCode.Insert))
            {
                // Perform the draft.
                DoDraft(false);
            }

            // Update the save delay if needed.
            if (currentSaveDelay < maxSaveDelay)
            {
                currentSaveDelay += Time.deltaTime;
            }
            // Save if the delay has been reached.
            else if (needSave)
            {
                SaveSettings();
                needSave = false;
            }
        }

        private void OnDestroy()
        {
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onHideUI.Remove(OnHideUI);
        }

        #endregion

        #region App Functions

        void DoRightClick()
        {
            // Perform the draft.
            DoDraft(false);
        }


        /// <summary>
        /// Displays the app when the player clicks.
        /// </summary>
        private void DisplayApp()
        {
            isShowing = true;
        }

        /// <summary>
        /// Displays the app while the player hovers.
        /// </summary>
        private void HoverApp()
        {
            isHovering = true;
        }

        /// <summary>
        /// Hides the app when the player clicks a second time.
        /// </summary>
        private void HideApp()
        {
            isShowing = false;
        }

        /// <summary>
        /// Hides the app when the player unhovers.
        /// </summary>
        private void UnhoverApp()
        {
            isHovering = false;
        }

        /// <summary>
        /// Hides the app when it is disabled.
        /// </summary>
        private void Disable()
        {
            isShowing = false;
            isHovering = false;
        }

        /// <summary>
        /// Repositions the app.
        /// </summary>
        private void Reposition(bool authWin = false)
        {
            //// Gets the button's anchor in 3D space.
            //float anchor = draftManagerButton.GetAnchor().x;

            //// Adjusts the window bounds.
            //windowRect = new Rect(Mathf.Min(anchor + 1210.5f - (windowWidth * (isCustomizing ? 2 : 1)), Screen.width - (windowWidth * (isCustomizing ? 2 : 1))), 40f, (windowWidth * (isCustomizing ? 2 : 1)), windowHeight);
            if (authWin)
            {
                windowRect = new Rect((Screen.width - windowWidth) / 2, (Screen.height - windowHeight) / 2, windowWidth, windowHeight);
                return;
            }
            // If the current scene is flight,
            if (HighLogic.LoadedSceneIsFlight)
            {
                // Set the window to the top right, offsetting for the size and launcher area.
                windowRect = new Rect(Screen.width - (windowWidth /* * (isCustomizing ? 2 : 1) */ ) - 42, 0f, (windowWidth /* * (isCustomizing ? 2 : 1) */ ), windowHeight);
            }
            // Else, if the current scene is the Space Center,
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                // Set the window to the bottom right, offsetting for the size and launcher area.
                windowRect = new Rect(Screen.width - (windowWidth /* * (isCustomizing ? 2 : 1) */ ), 42f, (windowWidth /* * (isCustomizing ? 2 : 1) */ ), windowHeight);
            }
        }

        /// <summary>
        /// Destroys the app.
        /// </summary>
        private void DestroyApp(GameScenes data)
        {
            if (data == GameScenes.MAINMENU)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);

                instance = null;
                Destroy(gameObject);
                Logger.DebugLog(Localizer.Format("#LOC_DTW_13"));

            }
        }

        /// <summary>
        /// A dummy method which returns nothing. 
        /// </summary>
        private void DummyVoid() { /* I do nothing!!! \('o')/ */ }

        #endregion

        #region GUI Functions

        GUISkin ActiveSkin;

        /// <summary>
        /// Called when Unity reaches the GUI phase.
        /// </summary>
        private void OnGUI()
        {
            if (ScenarioDraftManager.Instance == null)
                return;
            if (UseKSPSkin)
                ActiveSkin = HighLogic.Skin;
            else
                ActiveSkin = GUI.skin;
            if (ScenarioDraftManager.Instance.AuthNeeded && ((isShowing || isHovering) && !isUIHidden))
            {
                if (authWinVisible)
                    ClickThruBlocker.GUILayoutWindow(GetInstanceID(), windowRect, AuthWinWindow, Localizer.Format("#LOC_DTW_14"), ActiveSkin.window);
                Reposition(true);

                return;
            }
            // If the app is showing ir hovered over,
            if ((isShowing || isHovering) && !isUIHidden)
            {
                // Display the window.
                ClickThruBlocker.GUILayoutWindow(GetInstanceID(), windowRect, AppWindow, Localizer.Format("#LOC_DTW_15"), ActiveSkin.window);
            }

            // If the alert is showing,
            if (alertShowing && !isUIHidden)
            {
                // Display the window.
                ClickThruBlocker.GUILayoutWindow(GetInstanceID() + 1, alertRect, AlertWindow, Localizer.Format("#LOC_DTW_16") + (failedToDraft ? Localizer.Format("#LOC_DTW_17") : (draftBusy ? Localizer.Format("#LOC_DTW_18") : Localizer.Format("#LOC_DTW_19"))), ActiveSkin.window);
            }

            Reposition();
        }

        /// <summary>
        /// Draws the app window.
        /// </summary>
        /// <param name="windowID">The windiw ID.</param>
        private void AppWindow(int windowID)
        {
            if (ScenarioDraftManager.Instance == null)
                return;
            if (GUI.Button(new Rect(windowRect.width - 20, 2, 18, 18), "x"))

            {
                toolbarControl.SetFalse(true);
                return;
            }
            GUILayout.BeginVertical(ActiveSkin.box);

            // Show draft shortcut (Alt+D)
            GUILayout.Label(Localizer.Format("#LOC_DTW_20") + (UseHotkey ? Localizer.Format("#LOC_DTW_21") : Localizer.Format("#LOC_DTW_22")) + Localizer.Format("#LOC_DTW_23"), ActiveSkin.label);
            GUILayout.Label("", ActiveSkin.label);


            //Spacer Label
            GUILayout.Label("", ActiveSkin.label);

            // If career, display the cost of next draft.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                GUILayout.Label(Localizer.Format("#LOC_DTW_24") + (GameVariables.Instance.GetRecruitHireCost(HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount())).ToString("N0") + Localizer.Format("#LOC_DTW_25"), ActiveSkin.label);
            }

            if (PartSelectionManager.Instance != null && PartSelectionManager.Instance.toAdd != null)
                GUI.enabled = false;


            // Draft a Viewer from Twitch, skipping viewers who aren't Pilots.
            if (GUILayout.Button(Localizer.Format("#LOC_DTW_26"), ActiveSkin.button))
            {
                // Perform the draft.
                DoDraft(false, Localizer.Format("#LOC_DTW_27"));
            }

            // Draft a Viewer from Twitch, skipping viewers who aren't Engineers.
            if (GUILayout.Button(Localizer.Format("#LOC_DTW_28"), ActiveSkin.button))
            {
                // Perform the draft.
                DoDraft(false, Localizer.Format("#LOC_DTW_29"));
            }

            // Draft a Viewer from Twitch, skipping viewers who aren't Scientists.
            if (GUILayout.Button(Localizer.Format("#LOC_DTW_30"), ActiveSkin.button))
            {
                // Perform the draft.
                DoDraft(false, Localizer.Format("#LOC_DTW_31"));
            }

            // Draft a Viewer from Twitch, with any job.
            if (GUILayout.Button(Localizer.Format("#LOC_DTW_32"), ActiveSkin.button))
            {
                // Perform the draft.
                DoDraft(false);
            }

            GUI.enabled = true;

            //Spacer Label
            GUILayout.Label("", ActiveSkin.label);

            // Pull a name for a drawing
            if (GUILayout.Button(Localizer.Format("#LOC_DTW_33"), ActiveSkin.button))
            {
                // Perform the draft.
                DoDraft(true);
            }

            GUI.enabled = (ScenarioDraftManager.Instance.DrawnUsers.Count > 0);

            // Reset drawing list
            if (GUILayout.Button((ScenarioDraftManager.Instance.DrawnUsers.Count == 0 ? Localizer.Format("#LOC_DTW_34") : Localizer.Format("#LOC_DTW_35")), ActiveSkin.button))
            {
                // Empty the list.
                ScenarioDraftManager.Instance.DrawnUsers = new List<string>();
                // Save the list.
                ScenarioDraftManager.Instance.SaveDrawn();
            }

            GUI.enabled = (ScenarioDraftManager.Instance.AlreadyDrafted.Count > 0);
            // Reset drafting list
            if (GUILayout.Button((ScenarioDraftManager.Instance.AlreadyDrafted.Count == 0 ? Localizer.Format("#LOC_DTW_34") : Localizer.Format("#LOC_DTW_36")), ActiveSkin.button))
            {
                // Empty the list.
                ScenarioDraftManager.Instance.AlreadyDrafted = new List<string>();
                // Save the list.
                //ScenarioDraftManager.Instance.SaveDrafted();
            }

            GUI.enabled = true;

            //Spacer Label
            GUILayout.Label("", ActiveSkin.label);

            // Customize
            if (GUILayout.Button(Localizer.Format("#LOC_DTW_37"), ActiveSkin.button))
            {
                isCustomizing = !isCustomizing;
            }
            if (isCustomizing)
            {
                // Use hotkey toggle.
                UseHotkey = GUILayout.Toggle(UseHotkey, Localizer.Format("#LOC_DTW_38"), ActiveSkin.toggle);

                // Use UseKSPSkin toggle.
                UseKSPSkin = GUILayout.Toggle(UseKSPSkin, Localizer.Format("#LOC_DTW_39"), ActiveSkin.toggle);

                // Add drafted to craft toggle.
                AddToCraft = GUILayout.Toggle(AddToCraft, Localizer.Format("#LOC_DTW_40"), ActiveSkin.toggle);

                // Add "Kerman" toggle.
                ScenarioDraftManager.Instance.AddKerman = GUILayout.Toggle(ScenarioDraftManager.Instance.AddKerman, Localizer.Format("#LOC_DTW_41"), ActiveSkin.toggle);

                // On successful draft.
                GUILayout.Label(Localizer.Format("#LOC_DTW_42"), ActiveSkin.label);
                DraftMessage = GUILayout.TextField(DraftMessage, ActiveSkin.textField);

                // On successful draw.
                GUILayout.Label(Localizer.Format("#LOC_DTW_43"), ActiveSkin.label);
                DrawMessage = GUILayout.TextField(DrawMessage, ActiveSkin.textField);

                // $user Explanation
                GUILayout.Label("", ActiveSkin.label);
                GUILayout.Label("\"&user\" = " + // NO_LOCALIZATION
                    Localizer.Format("#LOC_DTW_44"), ActiveSkin.label);
                GUILayout.Label("\"&skill\" = " +  // NO_LOCALIZATION
                    Localizer.Format("#LOC_DTW_45"), ActiveSkin.label);

                // Bots to remove
                GUILayout.Label("", ActiveSkin.label);
                GUILayout.Label(Localizer.Format("#LOC_DTW_46"), ActiveSkin.label);
                string botsString = string.Join("\n", ScenarioDraftManager.Instance.BotsToRemove.ToArray());
                botsString = GUILayout.TextArea(botsString, ActiveSkin.textArea);
                ScenarioDraftManager.Instance.BotsToRemove = new List<string>();
                if (botsString != "") { ScenarioDraftManager.Instance.BotsToRemove.AddRange(botsString.Split('\n')); }

                // Save
                if (GUILayout.Button(Localizer.Format("#LOC_DTW_47"), ActiveSkin.button))
                {
                    SaveSettings();
                    ScenarioDraftManager.Instance.SaveGlobalSettings();
                }
            }

            //Version Label
            GUILayout.Label(Localizer.Format("#LOC_DTW_48") + (typeof(DraftManagerApp).Assembly.GetName().Version.ToString()), ActiveSkin.label);
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the alert window.
        /// </summary>
        /// <param name="windowID">The windiw ID.</param>
        private void AlertWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Alert text.
            GUILayout.Label(alertingMsg, ActiveSkin.label);

            // The close button.
            GUILayout.Label("", ActiveSkin.label);
            if (GUILayout.Button(Localizer.Format("#LOC_DTW_49"), ActiveSkin.button))
            {
                alertingMsg = "";
                alertShowing = false;
                failedToDraft = false;
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Called when the game UI is shown.
        /// </summary>
        private void OnShowUI()
        {
            isUIHidden = false;
        }

        /// <summary>
        /// Called when the game UI is hidden.
        /// </summary>
        private void OnHideUI()
        {
            isUIHidden = true;
        }

        #endregion

        #region KSP Functions

        /// <summary>
        /// Sets up for a draft.
        /// </summary>
        /// <param name="forDrawing">Whether this draft is for a plain drawing or an actual draft.</param>
        /// <param name="job">The job for the Kerbal. Optional and defaults to "Any" and is not needed if forDrawing is true.</param>
        private void DoDraft(bool forDrawing, string job = "Any") // NO_LOCALIZATION
        {

            SaveSettings();
            ScenarioDraftManager.Instance.SaveGlobalSettings();

            // Shows the alert as working.
            alertShowing = true;
            draftBusy = true;
            startClip.Play();

            if (forDrawing)
            {
                StartCoroutine(ScenarioDraftManager.DraftKerbal(DrawingSuccess, DraftFailure, forDrawing, false, job));
            }
            else
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && !Funding.CanAfford(GameVariables.Instance.GetRecruitHireCost(HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount())))
                {
                    DraftFailure(Localizer.Format("#LOC_DTW_50"));
                }
                else
                {
                    StartCoroutine(ScenarioDraftManager.DraftKerbal(DraftSuccess, DraftFailure, forDrawing, false, job));
                }
            }
        }

        /// <summary>
        /// Creates a new Kerbal based on the provided name.
        /// </summary>
        /// <param name="kerbalName">The name of the new Kerbal.</param>
        private void DraftSuccess(Draftee info)
        {
            ProtoCrewMember newKerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
            newKerbal.ChangeName(info.name);
            KerbalRoster.SetExperienceTrait(newKerbal, info.job);

            // If the game is career, subtract the cost of hiring.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                Funding.Instance.AddFunds(-GameVariables.Instance.GetRecruitHireCost(HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount() - 1), TransactionReasons.CrewRecruited);
            }
            // If the game mode is not Career, set the skill level to maximum possible.
            else
            {
                newKerbal.experienceLevel = 5;
                newKerbal.experience = 9999;
            }

            // If the game is Preflight and the user wants to automatically add to the craft,
            if (IsPreflight && addToCraft)
            {
                // Generate a selection of parts available for adding.
                PartSelectionManager.Instance.GenerateSelection(newKerbal);

                // Remove the in-game alert but still play the success tone.
                alertingMsg = "";
                draftBusy = false;
                alertShowing = false;
                failedToDraft = false;
                if (startClip.isPlaying) { startClip.Stop(); }
                successClip.Play();
            }
            // Otherwise,
            else
            {
                // Alert in-game.
                alertingMsg = draftMessage.Replace("&user", info.name).Replace("&skill", newKerbal.experienceTrait.Title); // NO_LOCALIZATION
                draftBusy = false;
                failedToDraft = false;
                alertShowing = true;
                if (startClip.isPlaying) { startClip.Stop(); }
                successClip.Play();
            }
        }

        /// <summary>
        /// Displays the winner of the drawing.
        /// </summary>
        /// <param name="winner">The winner of the drawing.</param>
        private void DrawingSuccess(Draftee info)
        {
            // Alert in-game.
            alertingMsg = drawMessage.Replace("&user", info.name); // NO_LOCALIZATION
            draftBusy = false;
            failedToDraft = false;
            alertShowing = true;
            if (startClip.isPlaying) { startClip.Stop(); }
            successClip.Play();
        }

        /// <summary>
        /// Indicates draft failure.
        /// </summary>
        /// <param name="reason">The reason for failure.</param>
        private void DraftFailure(string reason)
        {
            // Alert in-game.
            alertingMsg = reason;
            draftBusy = false;
            failedToDraft = true;
            alertShowing = true;
            if (startClip.isPlaying) { startClip.Stop(); }
            failureClip.Play();
        }

        /// <summary>
        /// Saves the settings.
        /// </summary>
        #region NO_LOCALIZATION
        private void SaveSettings()
        {
            // Load global settings.
            ConfigNode globalSettings = ConfigNode.Load(settingsLocation + "GlobalSettings.cfg");
            // If the file exists,
            if (globalSettings != null)
            {
                // Get the draft settings node.
                ConfigNode draftSettings = globalSettings.GetNode("DRAFT");

                // If the DRAFT node doesn't exist,
                if (draftSettings == null)
                {
                    // Create a new DRAFT node to write to.
                    draftSettings = globalSettings.AddNode("DRAFT");
                }

                // Write the useHotkey setting to it.
                draftSettings.SetValue("useHotkey", useHotkey.ToString(), true);

                // Write the UseKSPSkin setting to it.
                draftSettings.SetValue("UseKSPSkin", UseKSPSkin, true);



                // Write the addToCraft setting to it.
                draftSettings.SetValue("addToCraft", addToCraft.ToString(), true);

                // Get the message settings node.
                ConfigNode messageSettings = globalSettings.GetNode("MESSAGES");

                // If the MESSAGES node doesn't exist,
                if (messageSettings == null)
                {
                    // Create a new MESSAGES node to write to.
                    messageSettings = globalSettings.AddNode("MESSAGES");
                }

                // Write the messages to it.
                messageSettings.SetValue("draftMessage", draftMessage, true);
                messageSettings.SetValue("drawMessage", drawMessage, true);

                // Save the file.
                globalSettings.Save(settingsLocation + "GlobalSettings.cfg");
            }
            // If the file doesn't exist,
            else
            {
                // Log a warning that it wasn't found.
                Logger.DebugWarning("(During save) GlobalSettings.cfg wasn't found. Generating to save settings.");

                // Create a new root node.
                ConfigNode root = new ConfigNode();

                // Create a new DRAFT node to write the general settings to.
                ConfigNode draftSettings = root.AddNode("DRAFT");

                // Write the addToCraft setting to it.
                draftSettings.AddValue("addToCraft", addToCraft.ToString());

                // Create a new MESSAGES node.
                ConfigNode messageSettings = root.AddNode("MESSAGES");

                // Write the messages to it.
                messageSettings.AddValue("draftMessage", draftMessage);
                messageSettings.AddValue("drawMessage", drawMessage);

                // Save the file.
                root.Save(settingsLocation + "GlobalSettings.cfg");
            }
        }

        #endregion
        #endregion

        #region Misc Methods
        #region NO_LOCALIZATION
        /// <summary>
        /// Determines if the game is currently in Preflight status (The loaded scene is Flight and the active vessel is on the LaunchPad or Runway).
        /// </summary>
        /// <returns>True if the game is currently in Preflight status.</returns>
        bool IsPreflight
        {
            get
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    return (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH);
                    //return FlightGlobals.ActiveVessel.landedAt == "KSC_LaunchPad_Platform" || FlightGlobals.ActiveVessel.landedAt == "Runway";
                }

                return false;
            }
        }

        #endregion
        #endregion
    }
}
