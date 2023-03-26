using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarpManager
{
    
    //public class SettingsController
    //{
    //    private static WarpLogger logger = WarpLogger.GetLogger("ComponentManager");
    //    private static SettingsController instance = null;

    //    // UI UI
    //    public static Window settingsWindow;

    //    // - Warp Manager stuff
    //    public static Quaternion WM_ROTATION { get; set; }
    //    public static Vector3 Player_Location { get; set; }
    //    public static Vector3 Target_Location { get; set; }
    //    public static float WM_PLAYER_ANGLE_TO_TARGET { get; set; }


    //    public static void DestroyInstance() {
    //        logger.info($"Destroying ComponentManager Services...");
    //        if (instance != null)
    //        {
    //            MainWindowComponent.DestroyInstance();
    //            WarpManagerComponent.DestroyInstance();
    //            instance = null;
    //        }
    //    }

    //    /// <summary>
    //    /// Fetch an instance of the UIManager ( there can only be one!)
    //    /// </summary>
    //    /// <returns></returns>
    //    public static SettingsController CreateInstance()
    //    {
    //        if (instance != null)
    //            return instance;

    //        instance = new SettingsController();

    //        logger.info($"Initilizing Services...");
    //        MainWindowComponent.CreateInstance();
    //        MainWindowComponent.Render();
    //        WarpManagerComponent.CreateInstance();

    //        return instance;
    //    }


    //    public static void UIUpdate()
    //    {
    //        if (settingsWindow.IsValid)
    //        {
    //            //Main window updates
    //            MainWindowComponent.Updates();

    //            //WM Updates
    //            WarpManagerComponent.Updates();
    //        }
    //    }
    

    
        public static class SettingsController
        {
            private static List<Settings> settingsToSave = new List<Settings>();
            public static Dictionary<string, string> settingsWindows = new Dictionary<string, string>();
            private static bool IsCommandRegistered;

            public static string _staticName = string.Empty;

            public static Window settingsWindow;

            public static Config Config { get; private set; }

            public static void RegisterCharacters(Settings settings)
            {
                RegisterChatCommandIfNotRegistered();
                settingsToSave.Add(settings);
            }

            public static void RegisterSettingsWindow(string settingsName, string settingsWindowPath, Settings settings)
            {
                RegisterChatCommandIfNotRegistered();
                settingsWindows[settingsName] = settingsWindowPath;
                settingsToSave.Add(settings);
            }

            public static void RegisterSettings(Settings settings)
            {
                RegisterChatCommandIfNotRegistered();
                settingsToSave.Add(settings);
            }

            public static void CleanUp()
            {
                settingsToSave.ForEach(settings => settings.Save());
            }

            private static void RegisterChatCommandIfNotRegistered()
            {
                if (!IsCommandRegistered)
                {
                    Chat.RegisterCommand("warpmanager", (string command, string[] param, ChatWindow chatWindow) =>
                    {
                        try
                        {
                            Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\WarpManager\\{Game.ClientInst}\\Config.json");

                            settingsWindow = Window.Create(new Rect(50, 50, 300, 300), "Warp Manager", "Settings", WindowStyle.Default, WindowFlags.AutoScale);

                            if (settingsWindow != null && !settingsWindow.IsVisible)
                            {
                                AppendSettingsTab("Warp Manager", settingsWindow);

                                settingsWindow.FindView("ChannelBox", out TextInputView channelInput);

                                if (channelInput != null)
                                    channelInput.Text = $"{Config.CharSettings[Game.ClientInst].IPCChannel}";
                            }
                        }
                        catch (Exception e)
                        {
                            Chat.WriteLine(e);
                        }
                    });

                    IsCommandRegistered = true;
                }
            }
            public static Window FindValidWindow(Window[] allWindows)
            {
                foreach (var window in allWindows)
                {
                    if (window?.IsValid == true)
                        return window;
                }

                return null;
            }


            public static void AppendSettingsTab(String settingsName, Window testWindow)
            {
                String settingsWindowXmlPath = settingsWindows[settingsName];
                View settingsView = View.CreateFromXml(settingsWindowXmlPath);

                if (settingsView != null)
                {
                    testWindow.AppendTab(settingsName, settingsView);
                    testWindow.Show(true);
                }
                else
                {
                    Chat.WriteLine($"{settingsWindows[settingsName]}");
                    Chat.WriteLine("Failed to load settings schema from " + settingsWindowXmlPath);
                }
            }
        }
}

