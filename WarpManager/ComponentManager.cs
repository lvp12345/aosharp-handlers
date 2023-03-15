using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarpManager.Components;
using WarpManager.Models;

namespace WarpManager.Services
{
    public class ComponentManager
    {
        private static WarpLogger logger = WarpLogger.GetLogger("ComponentManager");
        private static ComponentManager instance = null;

        // UI UI
        public static Window MAIN_WINDOW;

        // - Warp Manager stuff
        public static Quaternion WM_ROTATION { get; set; }
        public static Vector3 WM_PLAYER_LOCATION { get; set; }
        public static Vector3 WM_TARGET_LOCATION { get; set; }
        public static float WM_PLAYER_ANGLE_TO_TARGET { get; set; }


        public ComponentManager() {
            //Default view states
            SetDefaultState();
        }

        public static void DestroyInstance() {
            logger.info($"Destroying ComponentManager Services...");
            if (instance != null)
            {
                MainWindowComponent.DestroyInstance();
                WarpManagerComponent.DestroyInstance();
                instance = null;
            }
        }

        /// <summary>
        /// Set the defautl state for the UI
        /// </summary>
        private static void SetDefaultState() 
        {
            //Default value states (some of these will be set once the other service's have initialized)
            MAIN_WINDOW = Window.CreateFromXml("Main Window", $"{Main.PLUGIN_DIR}\\UI\\MainWindow.xml");

            //WM
            WM_ROTATION = new Quaternion();
            WM_PLAYER_LOCATION = new Vector3();
            WM_TARGET_LOCATION = new Vector3();
            WM_PLAYER_ANGLE_TO_TARGET = 0;

            logger.debug($"ComponentManager initialized!");

        }

        /// <summary>
        /// Fetch an instance of the UIManager ( there can only be one!)
        /// </summary>
        /// <returns></returns>
        public static ComponentManager CreateInstance() 
        {
            if (instance != null)
                return instance;

            instance = new ComponentManager();
            
            logger.info($"Initilizing Services...");
            MainWindowComponent.CreateInstance();
            MainWindowComponent.Render();
            WarpManagerComponent.CreateInstance();

            return instance;
        }

        /// <summary>
        /// Nested within the plugin's OnUpdate, UI updates should happen here.
        /// </summary>
        public static void UIUpdate() 
        {
            if (MAIN_WINDOW.IsValid)
            {
                //Main window updates
                MainWindowComponent.Updates();

                //WM Updates
                WarpManagerComponent.Updates();
            }
        }
    }
}
