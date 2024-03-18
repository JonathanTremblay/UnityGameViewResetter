using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// This Editor script resets the aspect ratio of the GameView windows to 16:9, and sets the zoom to 1.
/// It also turns off the Low Resolution Aspect Ratios setting.
/// This is especially useful when you want these settings despite using retina displays or Windows zoom settings.
/// It also prevents team members from having different aspect ratios and zoom settings.
///  
/// It executes the verification once when the Unity editor is loaded.
/// It can also be executed manually from the Window menu.
/// It is compatible with Windows and MacOS.
/// It must be placed in an Editor folder (inside the Assets or Packages folder).
/// 
/// Created by Jonathan Tremblay, teacher at Cegep de Saint-Jerome.
/// This project is available for distribution and modification under the CC0 License.
/// https://github.com/JonathanTremblay/UnityGameViewResetter 
/// </summary>
namespace GameViewResetter 
{
    [InitializeOnLoad]
    public class GameViewResetter : Editor
    {
#if UNITY_EDITOR
        static readonly string _currentVersion = "Version 0.9.1 (2024-03-18)."; 
        static readonly bool _showPositiveMessages = true; // If true, the positive message will be shown. If false, only the negative messages will be shown.

        static readonly Dictionary<string, string> _messagesEn = new() // A dictionary for English messages
        {
            {"ABOUT", $"** GameViewResetter is free and open source. For updates and feedback, visit https://github.com/JonathanTremblay/UnityGameViewResetter **\n** {_currentVersion} **"},
            {"SUCCESS", "All Game View settings have been reset!"},
            {"DETAILS", "Aspect ratio 16:9, Scale 1, Low Resolution Aspect Ratios off."},
            {"NO_GAME_WINDOW", "GameViewResetter: No Game View window found, so no reset could be made."},
            {"NO_CHANGE_ERROR", "GameViewResetter: No changes have been made to the Game View settings."},
            {"SIZE_ERROR", "GameViewResetter Error: SizeSelectionCallback method not found on GameView window."},
            {"SCALE_AREA_ERROR", "GameViewResetter Error: areaObj object is null on m_ZoomArea field."},
            {"SCALE_ZOOM_ERROR", "GameViewResetter Error: m_Scale field not found on m_ZoomArea object."},
            {"LOW_RES_ERROR", "GameViewResetter Error: m_LowResolutionForAspectRatios field not found on GameView window."}
        };

        static readonly Dictionary<string, string> _messagesFr = new() // A dictionary for French messages
        {
            {"ABOUT", $"** GameViewResetter est gratuit et open source. Pour les mises à jour et les commentaires, visitez https://github.com/JonathanTremblay/UnityGameViewResetter **\n** {_currentVersion} **"},
            {"SUCCESS", "Tous les paramètres Game View ont été réinitialisés!"},
            {"DETAILS", "Ratio 16:9, Scale 1, Low Resolution Aspect Ratios désactivé."},
            {"NO_GAME_WINDOW", "GameViewResetter : Aucune fenêtre Game trouvée, donc aucune réinitialisation n'a pu être faite."},
            {"NO_CHANGE_ERROR", "GameViewResetter : Aucun changement n'a été apporté aux paramètres Game View."},
            {"SIZE_ERROR", "Erreur de GameViewResetter : La méthode SizeSelectionCallback n'a pas été trouvée sur la fenêtre Game."},
            {"SCALE_AREA_ERROR", "Erreur de GameViewResetter : L'objet areaObj est null sur le champ m_ZoomArea."},
            {"SCALE_ZOOM_ERROR", "Erreur de GameViewResetter : Le champ m_Scale n'a pas été trouvé sur l'objet m_ZoomArea."},
            {"LOW_RES_ERROR", "Erreur de GameViewResetter : Le champ m_LowResolutionForAspectRatios n'a pas été trouvé sur la fenêtre Game."}
        };

        // The dictionary to use for messages, depending on the current language:
        static Dictionary<string, string> _messages = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr" ? _messagesFr : _messagesEn;

        const string _RESET_MENU = "Window/Game View Resetter/Reset Game View Settings  %&r";
        const float _DESIRED_ASPECT = 1.777311f;
        const string _SESSION_STATE_KEY = "hasResetGameViewOnce";
        
        static readonly bool _showDebuggingMessages = false; // For debugging! If true, all messages will be shown. If false, only important messages will be shown.
        static readonly string _greenArrowL = "<color=green>▶</color>";
        static readonly string _greenArrowR = "<color=green>◀</color>";
        static readonly BindingFlags _searchFlags = BindingFlags.NonPublic | BindingFlags.Instance; // Allows to get private fields
        static bool _hasMadeOneChange = false;
        static readonly bool _forceEnglishMessages = false; // To force the display of messages in English (for testing purposes)

        enum RatioType { Free = 0, SixteenByNine = 1, SixteenByTen = 2 } // Aspect ratios for the GameView (numbers must match Unity's dropdown indexes).

        /// <summary>
        /// Constructor - Called when the script is loaded
        /// Checks if the game view settings have already been reset, and if not, resets them.
        /// </summary>
        static GameViewResetter ()
        {
            if (_forceEnglishMessages) _messages = _messagesEn;
            EditorApplication.projectChanged += ResetAllGameViewSettings; // Add a listener to the project change event
            EditorApplication.playModeStateChanged += ResetAllGameViewSettings; // Add a listener to the playmode state change event
            ResetAllGameViewSettings();
        }

        /// <summary>
        /// Called when the play mode state changes (when entering play mode, for example).
        /// Required by the event listener.
        /// </summary>
        static void ResetAllGameViewSettings(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            ResetAllGameViewSettings();
        }

        /// <summary>
        /// Find all open game views and reset their settings (aspect ratio, zoom scale, low resolution aspect ratios).
        /// </summary>
        static void ResetAllGameViewSettings()
        {
            if (SessionState.GetInt(_SESSION_STATE_KEY, 0) == 1) return; // If the game view settings have already been reset, we don't need to do it again
            _hasMadeOneChange = false;
            Assembly assembly = typeof(Editor).Assembly;
            System.Type gameViewType = assembly.GetType("UnityEditor.GameView");

            // Find all open game views... Ok, there should be only one...
            // but this technique prevents Unity from opening a new GameView window (like the GetWindow call):
            Object[] tGameViews = Resources.FindObjectsOfTypeAll(gameViewType);

            if (_showDebuggingMessages)
            {
                float aspect = Camera.main.aspect;
                Debug.Log("Initial camera ratio " + aspect + ((aspect == _DESIRED_ASPECT) ? " (16:9)" : " (not 16:9)"));
            }
            if (tGameViews.Length == 0)
            {
                // Debug.Log(_messages["NO_GAME_WINDOW"]); // Desactivated because it's common at project startup
                return; // No game window, so there's nothing to reset
            }
            foreach (Object gameViewWindow in tGameViews)
            {
                System.Type gameViewWindowType = gameViewWindow.GetType();
                ResetRatio(gameViewWindow, gameViewWindowType, RatioType.Free); // First, reset to free aspect (will force other settings to be applyed correctly)
                ResetLowRes(gameViewWindow, gameViewWindowType); // turning off Low Resolution Aspect Ratios before changing zoom scale
                ResetZoomScale(gameViewWindow, gameViewWindowType);
                ResetRatio(gameViewWindow, gameViewWindowType, RatioType.SixteenByNine); // Last, reset to 16:9 aspect
            }

            if (_showPositiveMessages && _hasMadeOneChange)
            {
                Debug.Log($"{_greenArrowL} <b>{_messages["SUCCESS"]}</b> {_greenArrowR} ({_messages["DETAILS"]})\n{_messages["ABOUT"]}");
                SessionState.SetInt("hasResetGameViewOnce", 1); // The game view settings have been reset, so we don't need to do it again
            }
            else if (!_hasMadeOneChange) Debug.LogError(_messages["NO_CHANGE_ERROR"]);
        }

        /// <summary>
        /// Get the SizeSelectionCallback method of the Game View Window, 
        /// and call it to change the aspect ratio to 16:9
        /// </summary>
        /// <param name="gameViewWindow">The current Game Window</param>
        /// <param name="gameViewWindowType">The type of the Window</param>
        private static void ResetRatio(Object gameViewWindow, System.Type gameViewWindowType, RatioType ratioType = RatioType.SixteenByNine)
        {
            // Change the aspect ratio of the game view window to 16:9
            MethodInfo sizeSelectionCallback = gameViewWindowType.GetMethod("SizeSelectionCallback");
            if (sizeSelectionCallback != null)
            {
                sizeSelectionCallback.Invoke(gameViewWindow, new object[] { ratioType, false });
                _hasMadeOneChange = true;
            }
            else Debug.LogError(_messages["SIZE_ERROR"]);
        }

        /// <summary>
        /// Get the m_ZoomArea field of the Game View Window and change the scale to 1
        /// </summary>
        /// <param name="gameViewWindow">The current Game Window</param>
        /// <param name="gameViewWindowType">The type of the Window</param>
        private static void ResetZoomScale(Object gameViewWindow, System.Type gameViewWindowType)
        {
            //change m_ZoomArea:
            FieldInfo zoomAreaField = gameViewWindowType.GetField("m_ZoomArea", _searchFlags);

            if (zoomAreaField != null)
            {
                System.Object areaObj = zoomAreaField.GetValue(gameViewWindow);
                if (areaObj != null)
                {
                    FieldInfo scaleField = areaObj.GetType().GetField("m_Scale", _searchFlags);
                    scaleField.SetValue(areaObj, Vector2.one);
                    _hasMadeOneChange = true;
                    if (_showDebuggingMessages) Debug.Log("ZoomArea scale: " + scaleField.GetValue(areaObj));
                }
                else Debug.LogError(_messages["SCALE_AREA_ERROR"]);

            }
            else Debug.LogError(_messages["SCALE_ZOOM_ERROR"]);
        }

        /// <summary>
        /// Get the m_LowResolutionForAspectRatios field of the Game View Window and turn it off
        /// </summary>
        /// <param name="gameViewWindow">The current Game Window</param>
        /// <param name="gameViewWindowType">The type of the Window</param>
        private static void ResetLowRes(Object gameViewWindow, System.Type gameViewWindowType)
        {
            // Change m_LowResolutionForAspectRatios
            FieldInfo lowResolutionField = gameViewWindowType.GetField("m_LowResolutionForAspectRatios", _searchFlags);
            if (lowResolutionField != null)
            {
                lowResolutionField.SetValue(gameViewWindow, new bool[] { false });
                _hasMadeOneChange = true;
            }
            else Debug.LogError(_messages["LOW_RES_ERROR"]);
        }

        /// <summary>
        /// Menu item to reset the game view settings (with keyboard shortcut CTRL+ALT+R)
        /// </summary>
        [MenuItem(_RESET_MENU, false, 0)]
        static void ResetGameViewSettings()
        {
            SessionState.SetInt("hasResetGameViewOnce", 0); // The game view settings should be reset again
            ResetAllGameViewSettings();
        }
#endif
    }
}