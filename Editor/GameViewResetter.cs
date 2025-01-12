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
#if UNITY_EDITOR
    [InitializeOnLoad]
    public class GameViewResetter : Editor
    {
        const string _VERSION = "Version 0.9.3 (2025-01-12).";
        const bool _SHOW_POSITIVE_MESSAGES = true; // If true, the positive message will be shown. If false, only the negative messages will be shown.

        static readonly Dictionary<string, string> _messagesEn = new() // A dictionary for English messages
        {
            {"ABOUT", $"<size=10>** Game View Resetter is free and open source. For updates and feedback, visit https://github.com/JonathanTremblay/UnityGameViewResetter **</size>\n<size=10>** {_VERSION} **</size>"},
            {"SUCCESS", "One or more Game View settings have been reset:"},
            {"NO_GAME_WINDOW", "Game View Resetter: No Game View window found, so no reset could be made."},
            {"NO_CHANGE", "Game View Resetter: No changes have been made to the Game View settings."},
            {"SIZE_ERROR", "Game View Resetter Error: SizeSelectionCallback method not found on GameView window."},
            {"SCALE_AREA_ERROR", "Game View Resetter Error: areaObj object is null on m_ZoomArea field."},
            {"SCALE_ZOOM_ERROR", "Game View Resetter Error: m_Scale field not found on m_ZoomArea object."},
            {"LOW_RES_ERROR", "Game View Resetter Error: m_LowResolutionForAspectRatios field not found on GameView window."},
            {"RATIO_SETTING", "Ratio"},
            {"SCALE_SETTING", "Scale"},
            {"LOW_RES_SETTING", "Low Resolution Aspect Ratios"}
        };

        static readonly Dictionary<string, string> _messagesFr = new() // A dictionary for English messages
        {
            {"ABOUT", $"<size=10>** Game View Resetter est gratuit et open source. Pour les mises à jour et les commentaires, visitez https://github.com/JonathanTremblay/UnityGameViewResetter **</size>\n<size=10>** {_VERSION} **</size>"},
            {"SUCCESS", "Un ou plusieurs paramètres Game View ont été réinitialisés:"},
            {"NO_GAME_WINDOW", "Game View Resetter : Aucune fenêtre Game trouvée, donc aucune réinitialisation n'a pu être faite."},
            {"NO_CHANGE", "Game View Resetter : Aucun changement n'a été apporté aux paramètres Game View."},
            {"SIZE_ERROR", "Erreur de Game View Resetter : La méthode SizeSelectionCallback n'a pas été trouvée sur la fenêtre Game."},
            {"SCALE_AREA_ERROR", "Erreur de Game View Resetter : L'objet areaObj est null sur le champ m_ZoomArea."},
            {"SCALE_ZOOM_ERROR", "Erreur de Game View Resetter : Le champ m_Scale n'a pas été trouvé sur l'objet m_ZoomArea."},
            {"LOW_RES_ERROR", "Erreur de Game View Resetter : Le champ m_LowResolutionForAspectRatios n'a pas été trouvé sur la fenêtre Game."},
            {"RATIO_SETTING", "Ratio"},
            {"SCALE_SETTING", "Scale"},
            {"LOW_RES_SETTING", "Low Resolution Aspect Ratios"}
        };

        // The dictionary to use for messages, depending on the current language:
        static Dictionary<string, string> _messages = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr" ? _messagesFr : _messagesEn;

        const string _RESET_MENU = "Help/Game View Resetter/Reset Game View Settings  %&r";
        const float _DESIRED_ASPECT = 1.777311f;
        const string _SESSION_STATE_KEY = "hasResetGameViewOnce";

        static readonly bool _showDebuggingMessages = false; // For debugging! If true, all messages will be shown. If false, only important messages will be shown.
        static readonly string _greenArrowL = "<color=green>▶</color>";
        static readonly string _greenArrowR = "<color=green>◀</color>";
        static readonly BindingFlags _searchFlags = BindingFlags.NonPublic | BindingFlags.Instance; // Allows to get private fields
        static bool _hasMadeChange = false;
        static List<string> _settingsChanged = new List<string>();
        static float _originalAspect = 0f;
        static readonly bool _forceEnglishMessages = false; // To force the display of messages in English (for testing purposes)

        enum RatioType { Free = 0, SixteenByNine = 1, SixteenByTen = 2 } // Aspect ratios for the GameView (numbers must match Unity's dropdown indexes).

        /// <summary>
        /// Constructor - Called when the script is loaded
        /// Checks if the game view settings have already been reset, and if not, calls the ResetAllGameViewSettings method.
        /// Also registers the ResetAllGameViewSettings method to be called on play mode changes and project changes.
        /// </summary>
        static GameViewResetter()
        {
            if (_forceEnglishMessages) _messages = _messagesEn;
            EditorApplication.playModeStateChanged += ResetAllGameViewSettings;
            EditorApplication.projectChanged += ResetAllGameViewSettings;
            if (SessionState.GetInt(_SESSION_STATE_KEY, 0) == 1) return; // If the game view settings have already been reset, we don't need to do it again
            ResetAllGameViewSettings();
        }

        /// <summary>
        /// Find all open game views and reset their settings (aspect ratio, zoom scale, low resolution aspect ratios).
        /// This method is called when the play mode state changes.
        /// </summary>
        static void ResetAllGameViewSettings(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            ResetAllGameViewSettings();
        }

        /// <summary>
        /// Find all open game views and reset their settings (aspect ratio, zoom scale, low resolution aspect ratios).
        /// This method is called when the project changes, when the script is loaded, and when the menu item is selected.
        /// </summary>
        static void ResetAllGameViewSettings()
        {
            _hasMadeChange = false;
            _originalAspect = 0f;
            _settingsChanged.Clear();
            Assembly assembly = typeof(Editor).Assembly;
            System.Type gameViewType = assembly.GetType("UnityEditor.GameView");

            // Find all open game views... Ok, there should be only one...
            // but this technique prevents Unity from opening a new GameView window (like the GetWindow call):
            Object[] tGameViews = Resources.FindObjectsOfTypeAll(gameViewType);

            if (tGameViews.Length == 0)
            {
                // Debug.Log(_messages["NO_GAME_WINDOW"]);
                return; // No game window, so there's nothing to reset
            }
            foreach (Object gameViewWindow in tGameViews)
            {
                System.Type gameViewWindowType = gameViewWindow.GetType();
                ResetLowRes(gameViewWindow, gameViewWindowType); // Low Resolution Aspect Ratios has to be the first change
                ResetZoomScale(gameViewWindow, gameViewWindowType);
                ResetRatio(gameViewWindow, gameViewWindowType, RatioType.SixteenByNine); // Last, reset to 16:9 aspect
            }

            if (_SHOW_POSITIVE_MESSAGES && _hasMadeChange)
            {
                Debug.Log($"{_greenArrowL} <b>{_messages["SUCCESS"]} {GetSettingsChangedString()}</b> {_greenArrowR}\n{_messages["ABOUT"]}");
                SessionState.SetInt("hasResetGameViewOnce", 1); // The game view settings have been reset, so we don't need to do it again
            }
            else if (!_hasMadeChange && _showDebuggingMessages) Debug.Log(_messages["NO_CHANGE"]);
        }

        /// <summary>
        /// Compare 2 floats with a two decimals precision (useful for aspect ratios)
        /// </summary>
        /// <param name="nb1">The first number</param>
        /// <param name="nb2">The second number</param>
        /// <returns>True if the numbers are equal with a two decimal precision</returns>
        private static bool CheckIfEqualWithTwoDecimals(float nb1, float nb2)
        {
            return (Mathf.Round(nb1 * 100f) / 100f) == (Mathf.Round(nb2 * 100f) / 100f);
        }

        /// <summary>
        /// Reset to free aspect, only once, if at least one setting has to be changed.
        /// This is a workaround to make sure that the other settings are applied correctly.
        /// </summary>
        /// <param name="gameViewWindow">The current Game Window</param>
        /// <param name="gameViewWindowType">The type of the Window</param>
        private static void ForceResetWorkaround(Object gameViewWindow, System.Type gameViewWindowType)
        {
            if (!_hasMadeChange) //if no change has been made yet...
            {
                ResetRatio(gameViewWindow, gameViewWindowType, RatioType.Free, true); // Reset to free aspect (will force other settings to be applyed correctly)
                _hasMadeChange = true; //note that at least one setting has been changed
            }
        }

        /// <summary>
        /// Add which setting has been changed to the list of settings changed
        /// </summary>
        /// <param name="setting">The setting that has been changed</param>
        private static void AddSettingChanged(string setting)
        {
            if (!_settingsChanged.Contains(setting)) _settingsChanged.Add(setting);
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
                System.Object currentLowRes = lowResolutionField.GetValue(gameViewWindow);
                System.Boolean[] currentLowResBool = (System.Boolean[])currentLowRes; //converts currentLowRes to a System.Boolean
                bool isLowRes = currentLowResBool[0];
                if (_showDebuggingMessages) Debug.Log(" • Current LowResolution: " + isLowRes + " (should change: " + isLowRes + ")");
                if (isLowRes)
                {
                    //ForceResetWorkaround(gameViewWindow, gameViewWindowType);
                    // Problem: when the Low Resolution Aspect Ratios setting is greyed out, the current state not always valid
                    // Solution: we set it back to false, but we don't report the change
                    //AddSettingChanged(_messages["LOW_RES_SETTING"]);
                    _hasMadeChange = false; //patch to prevent wrongly reporting a change
                    lowResolutionField.SetValue(gameViewWindow, new bool[] { false });
                }
            }
            else Debug.LogError(_messages["LOW_RES_ERROR"]);
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
                    object currentScale = scaleField.GetValue(areaObj);
                    bool shouldChangeValue = !currentScale.Equals(Vector2.one);
                    if (_showDebuggingMessages) Debug.Log(" • Current Scale: " + currentScale + " (should change: " + shouldChangeValue + ")"); //test
                    if (shouldChangeValue)
                    {
                        ForceResetWorkaround(gameViewWindow, gameViewWindowType);
                        AddSettingChanged(_messages["SCALE_SETTING"]);
                        scaleField.SetValue(areaObj, Vector2.one);
                    }
                }
                else Debug.LogError(_messages["SCALE_AREA_ERROR"]);

            }
            else Debug.LogError(_messages["SCALE_ZOOM_ERROR"]);
        }

        /// <summary>
        /// Get the SizeSelectionCallback method of the Game View Window, 
        /// and call it to change the aspect ratio to 16:9
        /// </summary>
        /// <param name="gameViewWindow">The current Game Window</param>
        /// <param name="gameViewWindowType">The type of the Window</param>
        private static void ResetRatio(Object gameViewWindow, System.Type gameViewWindowType, RatioType ratioType = RatioType.SixteenByNine, bool isWorkaround = false)
        {
            bool shouldChangeValue = true;
            if (_originalAspect == 0) _originalAspect = Camera.main.aspect; //if first call, store the original aspect ratio for later comparison
            if (!isWorkaround)
            {
                shouldChangeValue = !CheckIfEqualWithTwoDecimals(_originalAspect, _DESIRED_ASPECT); //should change if the comparison is false 
                if (_showDebuggingMessages) Debug.Log(" • Current Ratio " + _originalAspect + " (should change: " + shouldChangeValue + ")");
            }
            if (shouldChangeValue || _hasMadeChange) //if the aspect ratio should change or if at least one setting has been changed
            {
                // Change the aspect ratio of the game view window to 16:9
                MethodInfo sizeSelectionCallback = gameViewWindowType.GetMethod("SizeSelectionCallback");
                if (sizeSelectionCallback != null)
                {
                    if (shouldChangeValue && !isWorkaround) //change will be noted only if it is a real change
                    {
                        ForceResetWorkaround(gameViewWindow, gameViewWindowType); //condition to prevent overflows
                        AddSettingChanged(_messages["RATIO_SETTING"]);
                    }
                    sizeSelectionCallback.Invoke(gameViewWindow, new object[] { ratioType, false });
                    if (_showDebuggingMessages) Debug.Log("New aspect ratio: " + ratioType);
                }
                else Debug.LogError(_messages["SIZE_ERROR"]);
            }
        }

        /// <summary>
        /// Returns a string with the list of settings that have been changed seperated by commas and spaces
        /// </summary>
        /// <returns>A string with the list of settings that have been changed</returns>
        private static string GetSettingsChangedString()
        {
            string settingsChangedString = "";
            for (int i = 0; i < _settingsChanged.Count; i++)
            {
                settingsChangedString += _settingsChanged[i];
                if (i < _settingsChanged.Count - 1) settingsChangedString += ", ";
            }
            if (settingsChangedString.Length > 0) settingsChangedString += ".";
            return settingsChangedString;
        }

        /// <summary>
        /// Menu item to reset the game view settings (with keyboard shortcut CTRL+ALT+R)
        /// </summary>
        [MenuItem(_RESET_MENU, false, 21)]
        static void ResetGameViewSettings()
        {
            SessionState.SetInt("hasResetGameViewOnce", 0); // The game view settings should be reset again
            ResetAllGameViewSettings();
        }
    }
#endif
}