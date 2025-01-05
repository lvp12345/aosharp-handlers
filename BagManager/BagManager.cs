using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BagManager
{
    public class BagManager : AOPluginEntry
    {
        protected Settings _settings;

        public static Window _infoWindow;

        public static View _infoView;

        private static bool _init = false;

        public static string PluginDir;

        private CancellationTokenSource _cancellationToken1 = new CancellationTokenSource();
        private CancellationTokenSource _cancellationToken2 = new CancellationTokenSource();

        
        public override void Run()
        {
            _settings = new Settings("BagManager");
            base.Run();

            Game.OnUpdate += OnUpdate;
            Network.N3MessageSent += Network_N3MessageSent;

            _settings.AddVariable("Toggle", false);
            _settings["Toggle"] = false;

            Chat.RegisterCommand("manager", (string command, string[] param, ChatWindow chatWindow) =>
            {
                _settings["Toggle"] = !_settings["Toggle"].AsBool();
                Chat.WriteLine($"Bag : {_settings["Toggle"].AsBool()}");
            });

            Chat.RegisterCommand("delete", (string command, string[] param, ChatWindow chatWindow) =>
            {
                Backpack bag = Inventory.Backpacks.Where(c => c.Name.Contains("Delete") && c.IsOpen && c.Items.Count > 0).FirstOrDefault();

                if (bag != null)
                {
                    foreach (Item item in bag?.Items)
                    {
                        item?.Delete();
                    }

                    Chat.WriteLine("Deleted.");
                }
            });

            _settings.AddVariable("ModeSelection", (int)ModeSelection.Swap);

            RegisterSettingsWindow("Bag Manager", "BagManagerSettingsView.xml");

            if (Game.IsNewEngine)
            {
                Chat.WriteLine("Does not work on this engine!");
            }
            else
            {
                Chat.WriteLine("BagManager Loaded!");
                Chat.WriteLine("/bagmanager for settings.");
            }
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        private void HandleInfoViewClick(object s, ButtonBase button)
        {
            _infoWindow = Window.CreateFromXml("Info", PluginDir + "\\UI\\BagManagerInfoView.xml",
                windowSize: new Rect(0, 0, 440, 510),
                windowStyle: WindowStyle.Default,
                windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

            _infoWindow.Show(true);
        }

        private void HandleDeleteButtonClick(object s, ButtonBase button)
        {
            Backpack bag = Inventory.Backpacks.Where(c => c.Name.Contains("Delete") && c.IsOpen && c.Items.Count > 0).FirstOrDefault();

            if (bag != null)
            {
                foreach (Item item in bag?.Items)
                {
                    item?.Delete();
                }

                Chat.WriteLine("Deleted.");
            }
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }

        private void Network_N3MessageSent(object s, N3Message n3Msg)
        {
            if (n3Msg.Identity != DynelManager.LocalPlayer.Identity) { return; }

            if (!_settings["Toggle"].AsBool()) { return; }

            if (ModeSelection.Swap != (ModeSelection)_settings["ModeSelection"].AsInt32()) { return; }

            if (n3Msg.N3MessageType == N3MessageType.CharacterAction)
            {
                CharacterActionMessage charActionMsg = (CharacterActionMessage)n3Msg;

                if (charActionMsg.Action == CharacterActionType.UseItemOnItem)
                {
                    Backpack source = Inventory.Backpacks.Where(x => x.Slot.Instance == charActionMsg.Parameter2).FirstOrDefault();
                    Backpack target = Inventory.Backpacks.Where(x => x.Slot == charActionMsg.Target).FirstOrDefault();

                    if (target.Items.Count >= 1)
                    {
                        foreach (Item item in target.Items)
                        {
                            Network.Send(new ClientContainerAddItem()
                            {
                                Target = source.Identity,
                                Source = item.Slot
                            });
                        }
                    }
                }
            }
        }

        private void OnUpdate(object s, float deltaTime)
        {

            if (ModeSelection.Trade == (ModeSelection)_settings["ModeSelection"].AsInt32()
                && _settings["Toggle"].AsBool()
                && !_init)
            {
                _init = true;

                Backpack bag = Inventory.Backpacks.Where(c => c.Name.Contains("Trade") && c.IsOpen && c.Items.Count > 0).FirstOrDefault();

                Chat.WriteLine("Started.");

                //Task.Delay(2 * 1000).ContinueWith(x =>
                //{
                //    Task.Delay(150);

                //    foreach (Item bagItem in bag?.Items)
                //    {
                //        Task.Delay(50);

                //        bagItem.MoveToInventory();

                //        Task.Delay(1000);

                //        foreach (Item invItem in Inventory.Items.Where(c => c.Slot.Type == IdentityType.Inventory && c.Name == bagItem.Name))
                //        {
                //            Network.Send(new TradeMessage()
                //            {
                //                Unknown1 = 2,
                //                Action = (TradeAction)5,
                //                Param1 = (int)DynelManager.LocalPlayer.Identity.Type,
                //                Param2 = DynelManager.LocalPlayer.Identity.Instance,
                //                Param3 = (int)IdentityType.Inventory,
                //                Param4 = invItem.Slot.Instance
                //            });
                //        }
                //    }

                //    Task.Delay(200);
                //    _init = false;
                //    _settings["Toggle"] = false;
                //    Chat.WriteLine("Traded.");
                //    Chat.WriteLine("Toggle : False");
                //}, _cancellationToken1.Token);

                Task.Factory.StartNew(
                async () =>
                {
                    await Task.Delay(150);

                    foreach (Item bagItem in bag?.Items)
                    {
                        await Task.Delay(50);

                        bagItem.MoveToInventory();

                        await Task.Delay(1000);

                        foreach (Item invItem in Inventory.Items
                                                    .Where(c => c.Slot.Type == IdentityType.Inventory && c.Name == bagItem.Name && c.QualityLevel == bagItem.QualityLevel))
                        {
                            Network.Send(new TradeMessage()
                            {
                                Unknown1 = 2,
                                Action = (TradeAction)5,
                                Param1 = (int)DynelManager.LocalPlayer.Identity.Type,
                                Param2 = DynelManager.LocalPlayer.Identity.Instance,
                                Param3 = (int)IdentityType.Inventory,
                                Param4 = invItem.Slot.Instance
                            });
                        }
                    }

                    await Task.Delay(200);
                    _init = false;
                    _settings["Toggle"] = false;
                    Chat.WriteLine("Finished.");
                    Chat.WriteLine("Toggle : False");
                }, _cancellationToken1.Token);
            }

            if (ModeSelection.Sort == (ModeSelection)_settings["ModeSelection"].AsInt32()
                && _settings["Toggle"].AsBool()
                && !_init)
            {
                _init = true;

                List<Backpack> bags = Inventory.Backpacks.Where(c => c.Name.Contains("Sort")).ToList();

                Chat.WriteLine("Started.");

                //Task.Delay(1 * 100).ContinueWith(x =>
                //{
                //    foreach (Backpack bag in bags)
                //    {
                //        Task.Delay(50);
                //        foreach (Item item in bag.Items)
                //        {
                //            Task.Delay(50);
                //            Backpack backpack = Inventory.Backpacks.Where(c => item.Name.Contains(c.Name)).FirstOrDefault();
                //            Task.Delay(50);

                //            if (backpack != null)
                //                item.MoveToContainer(backpack);
                //            Chat.WriteLine("Item moved.");
                //            Task.Delay(50);
                //        }
                //        Task.Delay(50);
                //    }

                //    Task.Delay(50);
                //    _init = false;
                //    _settings["Toggle"] = false;
                //    Chat.WriteLine("Sorted.");
                //    Chat.WriteLine("Toggle : False");
                //}, _cancellationToken2.Token);

                Task.Factory.StartNew(
                async () =>
                {
                    foreach (Backpack bag in bags)
                    {
                        await Task.Delay(50);
                        foreach (Item item in bag.Items)
                        {
                            await Task.Delay(50);
                            Backpack backpack = Inventory.Backpacks.Where(c => item.Name.Contains(c.Name)).FirstOrDefault();
                            await Task.Delay(50);

                            if (backpack != null)
                                item.MoveToContainer(backpack);
                            await Task.Delay(50);
                        }
                        await Task.Delay(50);
                    }

                    await Task.Delay(50);
                    _init = false;
                    _settings["Toggle"] = false;
                    Chat.WriteLine("Finished.");
                    Chat.WriteLine("Toggle : False");
                }, _cancellationToken2.Token);
            }

            if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
            {
                if (SettingsController.settingsWindow.FindView("BagManagerInfoView", out Button infoView))
                {
                    infoView.Tag = SettingsController.settingsWindow;
                    infoView.Clicked = HandleInfoViewClick;
                }

                if (SettingsController.settingsWindow.FindView("DeleteButton", out Button deleteButton))
                {
                    deleteButton.Tag = SettingsController.settingsWindow;
                    deleteButton.Clicked = HandleDeleteButtonClick;
                }
            }
        }

        public static bool IsBackpack(Item item)
        {
            return item.Id == 275381 || item.Id == 143832 || item.Id == 157684 || item.Id == 157689 || item.Id == 157686 ||
                item.Id == 157691 || item.Id == 157692 || item.Id == 157693 || item.Id == 157683 || item.Id == 157682 ||
                item.Id == 287434 || item.Id == 157687 || item.Id == 157688 || item.Id == 157694 || item.Id == 157695 ||
                item.Id == 157690 || item.Id == 99241 || item.Id == 304586 || item.Id == 158790 || item.Id == 99228 ||
                item.Id == 223770 || item.Id == 152039 || item.Id == 156831 || item.Id == 259016 || item.Id == 259382 ||
                item.Id == 287419 || item.Id == 287420 || item.Id == 287421 ||
                item.Id == 287422 || item.Id == 287423 || item.Id == 287424 || item.Id == 287425 ||
                item.Id == 287427 || item.Id == 287429 || item.Id == 287430 ||
                item.Id == 287432 || item.Id == 287434 ||
                item.Id == 287440 || item.Id == 287441 ||
                item.Id == 287442 || item.Id == 287443 || item.Id == 287444 || item.Id == 287445 || item.Id == 287446 ||
                item.Id == 287447 || item.Id == 287448 || item.Id == 287610 ||
                item.Id == 287615 ||
                item.Id == 287617 || item.Id == 287619 || item.Id == 287620 || item.Id == 296977;
        }

        public enum ModeSelection
        {
            Swap, Sort, Trade
        }
    }
}
