using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System.Collections.Generic;
using WarpManager.Components;


namespace WarpManager.Services
{
    public class MainWindowComponent
    {
        
        private static WarpLogger logger = WarpLogger.GetLogger("MainWindowService");
        private static MainWindowComponent instance = null;

        

        private MainWindowComponent() 
        {
            logger.debug($"MainWindowComponent initialized!");
        }

        public static MainWindowComponent CreateInstance() 
        {
            if (instance == null)
                instance = new MainWindowComponent();

            return instance;
        }

        public static void DestroyInstance()
        {
            logger.info($"Destroying MainWindowComponent Services...");
            if (instance != null)
                instance = null;
        }

        public static void Render() 
        {
            ComponentManager.MAIN_WINDOW.Show(true);
            if (ComponentManager.MAIN_WINDOW.IsValid)
            {

                if (ComponentManager.MAIN_WINDOW.FindView("warpForwardButton", out ButtonBase warpForwardButton))
                {
                    warpForwardButton.Clicked += WarpManagerComponent.WarpForwardButtonCallBack;
                }

                if (ComponentManager.MAIN_WINDOW.FindView("warpToTargetButton", out ButtonBase warpToTargetButton))
                {
                    warpToTargetButton.Clicked += WarpManagerComponent.WarpToTargetButtonCallBack;
                }
            }
        }
        public static void Updates()
        {
            if (ComponentManager.MAIN_WINDOW.IsValid)
            {
                
                
                if (ComponentManager.MAIN_WINDOW.FindView("infoLabel", out TextView infoLabel))
                    infoLabel.Text = $"Information:";

                if (ComponentManager.MAIN_WINDOW.FindView("versionLabel", out TextView versionLabel))
                    versionLabel.Text = $"WarpManager v1.0.0";

            }
        }


        
    }
}
