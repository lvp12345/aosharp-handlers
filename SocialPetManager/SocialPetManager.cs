using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialPetManager
{
    public class SocialPetManager : AOPluginEntry
    {

        protected Settings _settings;

        public static Window _infoWindow;

        public static View _infoView;

        public static string PluginDir;

        protected int SelectedSocialPet;

        protected int SavedSocialPetID = 0;

        protected int CurrentSocialPetID = 0;

        protected bool CurrentlyBusy = false;

        protected double _lastZonedTime = Time.NormalTime;

        protected double _timer = 0f;

        public override void Run()
        {
            try
            {
                
                if (Game.IsNewEngine)
                {
                    Chat.WriteLine("Does not work on this engine!");
                    return;
                }

                Chat.WriteLine("Social Pet Manager Loaded!");
                Chat.WriteLine("/SocialPetManager for settings");

                _settings = new Settings("SocialPetManager");
                _settings.AddVariable("SelectedSocialPet", 287160);

                RegisterSettingsWindow("Social Pet Manager", "SocialPetManagerSettingWindow.xml");

                if (PetCurrentSocial() != null)
                    CurrentSocialPetID = 1; //we know that we have a pet, but we do not know which one

                Game.OnUpdate += OnUpdate;
                Game.TeleportEnded += TeleportEnded;

            }
            catch (Exception ex)
            {
                var errorMessage = "An error occurred: " + ex.Message;
                Chat.WriteLine(errorMessage);
                Chat.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }

        private void OnUpdate(object s, float deltaTime)
        {
            if (Game.IsZoning || Time.NormalTime < _lastZonedTime + 2.0) { return; }

            if (Time.NormalTime < _timer + 0.4) // 0.4 sec tick, low profile manager
                return;

            if (CurrentlyBusy)
                return;

            _timer = Time.NormalTime;
            CastSocialPet();

        }

        private void CastSocialPet()
        {
            var pet = PetCurrentSocial();

            // user input
            SavedSocialPetID = (int)_settings["SelectedSocialPet"].AsInt32();

            // we do not have a pet
            if (pet == null)
            {
                // and we do not want a pet
                if (SavedSocialPetID == 0)
                {
                    CurrentSocialPetID = 0;
                    return;
                }
                // we want a pet
                else
                {
                    // so proceed to the casting
                }
            }
            // we have a pet already
            else
            {
                // we are content with our pet
                if (SavedSocialPetID == CurrentSocialPetID)
                {
                    return;
                }
                // we just want to get rid of it
                else if (SavedSocialPetID == 0 && CurrentSocialPetID > 0)
                {
                    _timer += 0.4;
                    CurrentSocialPetID = 0;
                    PetTerminate(pet);
                    return;
                }
                // we want a new pet
                else
                {
                    // so proceed to the casting
                }
            }

            // Find the pet spell
            if (!Spell.Find(SavedSocialPetID, out Spell newPetSpell))
                return;

            // We have a replacement pet, so let's terminate the old one
            if (pet != null && CurrentSocialPetID > 0)
            {
                _timer += 0.4;
                CurrentSocialPetID = 0;
                PetTerminate(pet);
                return;
            }

            // check if we are ready to cast the new pet
            if (!CanCast(newPetSpell))
                return;

            // cast
            _timer += 2.1;
            newPetSpell.Cast(false);
            CurrentSocialPetID = SavedSocialPetID;

        }

        public Pet PetCurrentSocial()
        {
            if (DynelManager.LocalPlayer.Pets.Count() == 0)
                return null;

            foreach (Pet pet in DynelManager.LocalPlayer.Pets)
            {
                if (pet.Type == PetType.Social)
                {
                    return pet;
                }
            }
            return null;
        }

        private void PetTerminate(Pet pet)
        {
            if (pet == null)
                return;

            Network.Send(new PetCommandMessage()
            {
                Command = PetCommand.Terminate,
                Pets = new PetBase[1] {
                    pet
                }
            });
        }

        public static bool CanCast(Spell spell)
        {
            var localPlayer = DynelManager.LocalPlayer;

            if (Playfield.ModelIdentity.Instance == 152)
                return false;

            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 0)
                return false;

            if (localPlayer.MovementState == MovementState.Fly || localPlayer.IsFalling)
                return false;

            if (!Spell.List.Any(cast => cast.IsReady) || Spell.HasPendingCast)
                return false;

            if (!localPlayer.MovementStatePermitsCasting)
                return false;

            if (DynelManager.LocalPlayer.GetStat(Stat.TemporarySkillReduction) > 1)
                return false;

            return spell.Cost < DynelManager.LocalPlayer.Nano;
        }

        private void TeleportEnded(object sender, EventArgs e)
        {
            _lastZonedTime = Time.NormalTime;
        }

        protected void RegisterSettingsWindow(string settingsName, string xmlName)
        {
            SettingsController.RegisterSettingsWindow(settingsName, PluginDir + "\\UI\\" + xmlName, _settings);
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }

        public static Array SocialPets = new int[]{
            300453,
            253168,
            287160,
            295322,
            303127,
            294060,
            300803,
            300850,
            290645,
            300806,
            301663,
            248386,
            296513,
            302548,
            295315,
            290122,
            293828,
            290062,
            295327,
            300571,
            277450,
            300439,
            304789,
            303649,
            289971,
            288561,
            285678,
            285056,
            294056
        };
    }
}

