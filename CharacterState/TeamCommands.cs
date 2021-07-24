using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using CombatHandler.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AOSharp.Core.Inventory;

namespace Character.State
{
    public class TeamCommands
    {
        public void RegisterCommands()
        {
            Chat.RegisterCommand("reform", ReformCommand);
            Chat.RegisterCommand("form", FormCommand);
            Chat.RegisterCommand("disband", DisbandCommand);
            Team.TeamRequest = Team_TeamRequest;
        }

        private void DisbandCommand(string command, string[] param, ChatWindow chatWindow)
        {
            Team.Disband();
            CharacterState.BroadcastDisband();
        }

        private void ReformCommand(string command, string[] param, ChatWindow chatWindow)
        {
            Team.Disband();
            CharacterState.BroadcastDisband();
            Task task = new Task(() =>
            {
                Thread.Sleep(1000);
                FormCommand("form", param, chatWindow);
            });
            task.Start();
        }

        private void FormCommand(string command, string[] param, ChatWindow chatWindow)
        {
            if (!DynelManager.LocalPlayer.IsInTeam())
            {
                SendTeamInvite(GetRegisteredCharacters());

                if (IsRaidEnabled(param))
                {
                    Task task = new Task(() =>
                    {
                        Thread.Sleep(1000);
                        Team.ConvertToRaid();
                        Thread.Sleep(1000);
                        SendTeamInvite(GetRemainingRegisteredCharacters());
                    });
                    task.Start();
                }
            }
            else
            {
                Chat.WriteLine("Cannot form a team. Character already in team. Disband first.");
            }
        }

        private bool IsRaidEnabled(string[] param)
        {
            return param.Length > 0 && "raid".Equals(param[0]);
        }

        private Identity[] GetRegisteredCharacters()
        {
            Identity[] registeredCharacters = CharacterState.GetRegisteredCharacters();
            int firstTeamCount = registeredCharacters.Length > 6 ? 6 : registeredCharacters.Length;
            Identity[] firstTeamCharacters = new Identity[firstTeamCount];
            Array.Copy(registeredCharacters, firstTeamCharacters, firstTeamCount);
            return firstTeamCharacters;
        }

        private Identity[] GetRemainingRegisteredCharacters()
        {
            Identity[] registeredCharacters = CharacterState.GetRegisteredCharacters();
            int characterCount = registeredCharacters.Length - 6;
            Identity[] remainingCharacters = new Identity[characterCount];
            if(characterCount > 0)
            {
                Array.Copy(registeredCharacters, 6, remainingCharacters, 0, characterCount);
            }
            return remainingCharacters;
        }

        private void SendTeamInvite(Identity[] targets)
        {
            foreach(Identity target in targets)
            {
                Team.Invite(target);
            }
        }

        private void Team_TeamRequest(object s, TeamRequestEventArgs e)
        {
            if (CharacterState.IsCharacterRegistered(e.Requester))
            {
                e.Accept();
            }
        }
    }
}
