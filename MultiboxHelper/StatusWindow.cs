using AOSharp.Common.GameData;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiboxHelper
{
    public class StatusWindow
    {
        private Window _window;
        private Dictionary<int, CharacterStatus> _statuses = new Dictionary<int, CharacterStatus>();
        private Dictionary<int, View> _statusViews = new Dictionary<int, View>();

        public StatusWindow()
        {
        }

        public void Open()
        {
            WindowFlags winFlags = WindowFlags.AutoScale | WindowFlags.NoFade | WindowFlags.NoExit;
            _window = Window.CreateFromXml("MBStatus", $"{MultiboxHelper.PluginDir}\\StatusWindow.xml", windowSize: new Rect(0,0,150,0), windowStyle: WindowStyle.Default, windowFlags: winFlags);

            if(_window == null)
            {
                Chat.WriteLine("Failed to create status window.");
                return;
            }

            _statusViews.Clear();

            foreach(var status in _statuses)
                AddStatusView(status.Key, status.Value);

            _window.Show(true);
        }

        private void AddStatusView(int charId, CharacterStatus status)
        {
            if (_window != null && _window.FindView("charStatusContainer", out View rootView))
            {
                View statusView = View.CreateFromXml($"{MultiboxHelper.PluginDir}\\CharStatusView.xml");
                _statusViews.Add(charId, statusView);
                rootView.AddChild(statusView, true);
                rootView.FitToContents();
            }
        }

        public void SetCharStatus(int charId, CharacterStatus status)
        {
            if (_statuses.ContainsKey(charId))
                _statuses[charId] = status;
            else
                _statuses.Add(charId, status);

            if (!_statusViews.ContainsKey(charId))
                AddStatusView(charId, status);

            if(_statusViews.TryGetValue(charId, out View statusView))
                UpdateView(_statusViews[charId], status);
        }

        public void RemoveChar(int charId)
        {
            _statuses.Remove(charId);

            if (_window == null)
                return;

            if (_statusViews.TryGetValue(charId, out View charView) && _window.FindView("charStatusContainer", out View rootView))
            {
                rootView.RemoveChild(charView);
                rootView.FitToContents();
                _statusViews.Remove(charId);
            }
        }

        private void UpdateView(View statusView, CharacterStatus status)
        {
            if (statusView.FindChild("charNameLabel", out TextView nameLabel))
                nameLabel.Text = status.Name;

            if (statusView.FindChild("charHPBar", out PowerBarView hpBar))
                hpBar.Value = (float)status.Health / status.MaxHealth;

            if (statusView.FindChild("charNanoBar", out PowerBarView nanoBar))
                nanoBar.Value = (float)status.Nano / status.MaxNano;
        }
    }

    public class CharacterStatus
    {
        public string Name;
        public int Health;
        public int MaxHealth;
        public int Nano;
        public int MaxNano;
    }
}
