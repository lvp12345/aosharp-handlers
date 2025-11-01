using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Common.Unmanaged.Interfaces;
using AOSharp.Core;
using AOSharp.Core.UI;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ResearchManager
{
    public class ResearchManager : AOPluginEntry
    {
        protected Settings _settings;

        double lastUpdateTime;
        bool enabled => _settings["Toggle"].AsBool();
        bool includeApotheosis => _settings["IncludeApotheosis"].AsBool();
        ModeSelection mode => (ModeSelection)_settings["ModeSelection"].AsInt32();
        float updateInterval => _settings["UpdateInterval"].AsFloat();

        // Track settings changes to reinitialize display when needed
        private bool _lastIncludeApotheosis = false;

        static List<int> apotheosis = Enumerable.Range(10002, 10).ToList();

        // Research levels display
        private MultiListView _researchLevelsList;
        private Dictionary<int, string> _researchNames;
        private List<int> _allResearchIds;
        private bool _windowAutoOpened = false;

        // Cache for live updates (similar to stack manager)
        private Dictionary<string, int> _lastResearchLevels = new Dictionary<string, int>();
        private Dictionary<string, float> _lastResearchProgress = new Dictionary<string, float>();
        private Dictionary<string, View> _researchEntryViews = new Dictionary<string, View>();
        private bool _researchDisplayInitialized = false;

        public override void Run()
        {
            try
            {
                // Very first debug output to see if Run() is called
                Chat.WriteLine("ResearchManager Run() method called!");

                _settings = new Settings("Research");

            _settings.AddVariable("Toggle", false);
            _settings.AddVariable("IncludeApotheosis", false);
            _settings.AddVariable("ModeSelection", (int)ModeSelection.HighestFirst);
            _settings.AddVariable("UpdateInterval", 10);

            InitializeResearchData();

            // Register the chat command directly
            try
            {
                // Try SettingsController approach with better cleanup
                Chat.RegisterCommand("researchmanager", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    OpenResearchWindowSafe();
                });

                // Add short command alias
                Chat.RegisterCommand("rm", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    OpenResearchWindowSafe();
                });

                // Debug command to examine available research goals
                Chat.RegisterCommand("rmtest", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Chat.WriteLine("=== ResearchManager Debug Info ===");
                    Chat.WriteLine($"Total Research Goals: {Research.Goals?.Count() ?? 0}");

                    if (Research.Goals != null)
                    {
                        foreach (var goal in Research.Goals.Where(g => g.Available))
                        {
                            Chat.WriteLine($"ID: {goal.ResearchId}, Available: {goal.Available}");

                            // Try to get research name from game data if possible
                            try
                            {
                                // Check ALL properties of the goal
                                var goalType = goal.GetType();
                                var properties = goalType.GetProperties();
                                Chat.WriteLine($"  Goal Type: {goalType.Name}");
                                foreach (var prop in properties)
                                {
                                    try
                                    {
                                        var value = prop.GetValue(goal);
                                        Chat.WriteLine($"  {prop.Name}: {value}");
                                    }
                                    catch (Exception propEx)
                                    {
                                        Chat.WriteLine($"  {prop.Name}: <Error: {propEx.Message}>");
                                    }
                                }

                                // Try to get perk info using the research ID
                                try
                                {
                                    var progress = N3EngineClientAnarchy.GetPerkProgress((uint)goal.ResearchId);
                                    Chat.WriteLine($"  Progress: {progress:F2}");
                                }
                                catch (Exception progEx)
                                {
                                    Chat.WriteLine($"  Progress: <Error: {progEx.Message}>");
                                }

                                Chat.WriteLine("  ---");
                            }
                            catch (Exception ex)
                            {
                                Chat.WriteLine($"  Error getting properties: {ex.Message}");
                            }
                        }
                    }
                });

                // Command to check research ID patterns
                Chat.RegisterCommand("rmnames", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Chat.WriteLine("=== Research ID Analysis ===");

                    if (Research.Goals != null)
                    {
                        var availableGoals = Research.Goals.Where(g => g.Available).OrderBy(g => g.ResearchId).ToList();
                        Chat.WriteLine($"Available Research IDs: {string.Join(", ", availableGoals.Select(g => g.ResearchId))}");

                        // Group by potential research lines (assuming 10 levels per line)
                        var groupedByLine = availableGoals.GroupBy(g => g.ResearchId / 10).OrderBy(g => g.Key);

                        foreach (var group in groupedByLine)
                        {
                            var ids = group.Select(g => g.ResearchId).OrderBy(id => id).ToList();
                            Chat.WriteLine($"Research Line {group.Key}: IDs {string.Join(", ", ids)}");

                            // Check if this follows a pattern (consecutive IDs)
                            bool isConsecutive = true;
                            for (int i = 1; i < ids.Count; i++)
                            {
                                if (ids[i] != ids[i-1] + 1)
                                {
                                    isConsecutive = false;
                                    break;
                                }
                            }
                            Chat.WriteLine($"  Consecutive: {isConsecutive}");
                        }
                    }
                });

                // Test command for research line detection
                Chat.RegisterCommand("rmlines", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Chat.WriteLine("=== Available Research Lines ===");
                    var lines = GetAvailableResearchLines();
                    foreach (var line in lines)
                    {
                        Chat.WriteLine($"{line.name}: Base ID {line.baseId}, Available IDs: {string.Join(", ", line.availableIds)}");
                    }
                });

                // Safe test command that doesn't create UI
                Chat.RegisterCommand("rmsafe", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Chat.WriteLine("=== Safe Research Test ===");
                    try
                    {
                        var lines = GetAvailableResearchLines();
                        foreach (var line in lines)
                        {
                            foreach (int researchId in line.availableIds)
                            {
                                try
                                {
                                    float progress = N3EngineClientAnarchy.GetPerkProgress((uint)researchId);
                                    Chat.WriteLine($"{line.name} (ID {researchId}): {(progress * 100):F1}%");
                                }
                                catch (Exception ex)
                                {
                                    Chat.WriteLine($"{line.name} (ID {researchId}): Error - {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Chat.WriteLine($"Error in safe test: {ex.Message}");
                    }
                });

                // Simple test command that doesn't use SettingsController
                Chat.RegisterCommand("rmtest2", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    try
                    {
                        Chat.WriteLine("Testing simple window creation...");

                        // Try creating a very basic window without SettingsController
                        var testWindow = Window.CreateFromXml("Test Window",
                            PluginDirectory + "\\UI\\ResearchManagerSettingWindow.xml",
                            windowSize: new Rect(100, 100, 400, 300),
                            windowStyle: WindowStyle.Default,
                            windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                        if (testWindow != null)
                        {
                            testWindow.Show(true);
                            Chat.WriteLine("Test window created and shown!");

                            // Don't close it immediately - let user close it manually
                            // testWindow.Close();
                            // testWindow = null;
                            // Chat.WriteLine("Test window closed successfully!");
                        }
                        else
                        {
                            Chat.WriteLine("Failed to create test window!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Chat.WriteLine($"Error in test: {ex.Message}");
                    }
                });

                Chat.WriteLine("ResearchManager commands registered successfully!");
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error registering researchmanager commands: {ex.Message}");
            }

            if (Game.IsNewEngine)
            {
                Chat.WriteLine("Does not work on this engine!");
            }
            else
            {
                Chat.WriteLine("Research Manager Loaded!");
                Chat.WriteLine("/researchmanager for settings.");

                // Auto-open the window after a short delay
                Game.OnUpdate += OnUpdate;
                Game.OnUpdate += AutoOpenWindow;
            }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"CRITICAL ERROR in ResearchManager.Run(): {ex.Message}");
                Chat.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public override void Teardown()
        {
            SettingsController.CleanUp();
        }



        private void OpenResearchWindow()
        {
            try
            {
                // Clean up any existing window first
                if (SettingsController.settingsWindow != null)
                {
                    try
                    {
                        if (SettingsController.settingsWindow.IsVisible)
                        {
                            SettingsController.settingsWindow.Close();
                        }

                        // Force cleanup
                        SettingsController.settingsWindow = null;
                    }
                    catch (Exception cleanupEx)
                    {
                        Chat.WriteLine($"Error during window cleanup: {cleanupEx.Message}");
                        SettingsController.settingsWindow = null;
                    }
                }

                // Reset all references before opening new window
                ResetWindowReferences();

                // Wait a moment for cleanup to complete
                System.Threading.Thread.Sleep(100);

                // Create window with XML content
                string xmlPath = PluginDirectory + "\\UI\\ResearchManagerSettingWindow.xml";
                Chat.WriteLine($"Loading XML from: {xmlPath}");

                // Check if XML file exists
                if (!System.IO.File.Exists(xmlPath))
                {
                    Chat.WriteLine("ERROR: XML file does not exist!");
                    return;
                }

                SettingsController.settingsWindow = Window.CreateFromXml("Research Manager",
                    xmlPath,
                    windowSize: new Rect(50, 50, 720, 400),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                if (SettingsController.settingsWindow != null)
                {
                    SettingsController.settingsWindow.Show(true);
                    Chat.WriteLine("Research Manager window opened!");

                    // Debug window properties
                    Chat.WriteLine($"Window IsValid: {SettingsController.settingsWindow.IsValid}");
                    Chat.WriteLine($"Window IsVisible: {SettingsController.settingsWindow.IsVisible}");

                    // Try to find the MultiListView for research display
                    if (SettingsController.settingsWindow.FindView("researchLevelsList", out MultiListView listView1))
                    {
                        _researchLevelsList = listView1;
                    }
                    else
                    {
                        // Try alternative search methods
                        foreach (var view in SettingsController.settingsWindow.Views)
                        {
                            if (FindMultiListViewRecursive(view, "researchLevelsList"))
                            {
                                break;
                            }
                        }

                        // If still not found, try searching by type
                        if (_researchLevelsList == null)
                        {
                            foreach (var view in SettingsController.settingsWindow.Views)
                            {
                                SearchForMultiListViewByType(view);
                                if (_researchLevelsList != null)
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    Chat.WriteLine("Failed to create Research Manager window!");
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine($"Error opening Research Manager window: {e.Message}");
            }
        }

        private void AutoOpenWindow(object s, float deltaTime)
        {
            // Temporarily disabled to prevent crashes
            // if (!_windowAutoOpened && !Game.IsZoning && Time.NormalTime > 3.0) // Wait 3 seconds after startup
            // {
            //     _windowAutoOpened = true;
            //     OpenResearchWindow();
            //     Game.OnUpdate -= AutoOpenWindow; // Remove this handler after opening
            // }
        }

        private void OnUpdate(object s, float deltaTime)
        {
            if (Game.IsZoning)
                return;

            // Update research training logic
            if (enabled && Time.NormalTime >= lastUpdateTime + updateInterval)
            {
                lastUpdateTime = Time.NormalTime;

                var availableGoals = Research.Goals.Where(goal => goal.Available && (includeApotheosis || !apotheosis.Contains(goal.ResearchId)));

                if (mode == ModeSelection.LowestFirst)
                {
                    availableGoals = availableGoals.OrderBy(goal => GetPerkLevel(goal.ResearchId)).ThenByDescending(goal => N3EngineClientAnarchy.GetPerkProgress((uint)goal.ResearchId));
                }

                if (mode == ModeSelection.HighestFirst)
                {
                    availableGoals = availableGoals.OrderByDescending(goal => GetPerkLevel(goal.ResearchId)).ThenByDescending(goal => N3EngineClientAnarchy.GetPerkProgress((uint)goal.ResearchId));
                }

                if (availableGoals.Count() > 0)
                {
                    ResearchGoal goal = availableGoals.First();

                    if (DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal) != goal.ResearchId)
                    {
                        Research.Train(goal.ResearchId);
                    }
                }
            }

            // Check if settings changed and reinitialize if needed
            if (_lastIncludeApotheosis != includeApotheosis)
            {
                _lastIncludeApotheosis = includeApotheosis;
                _researchDisplayInitialized = false; // Force reinitialize
            }

            // Update research levels display using SettingsController window
            if (Time.NormalTime % 2.0 < 0.1) // Check every 2 seconds like stack manager
            {
                // Only update if SettingsController window is open
                if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid && SettingsController.settingsWindow.IsVisible)
                {
                    // Use the individual TextViews approach for the 2-column SimpleWindow layout
                    UpdateIndividualResearchDisplays();
                }
                else if (SettingsController.settingsWindow == null || !SettingsController.settingsWindow.IsValid || !SettingsController.settingsWindow.IsVisible)
                {
                    // Window is closed or invalid - reset our references
                    ResetWindowReferences();
                }
            }
        }

        private Window _directWindow = null;
        private static int _openAttempts = 0;
        private TextView _researchDisplayView = null;

        private void OpenResearchWindowSafe()
        {
            try
            {
                _openAttempts++;

                // Aggressive cleanup of any existing window
                if (SettingsController.settingsWindow != null)
                {
                    try
                    {
                        Chat.WriteLine("Cleaning up existing SettingsController window");
                        SettingsController.settingsWindow.Close();
                        SettingsController.settingsWindow = null;
                    }
                    catch (Exception ex)
                    {
                        Chat.WriteLine($"Error closing existing window: {ex.Message}");
                        SettingsController.settingsWindow = null;
                    }
                }

                // Reset all references
                ResetWindowReferences();

                // Force garbage collection
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();

                // Add delay for cleanup
                System.Threading.Thread.Sleep(300);

                // Create the simple window with 2-column layout
                string simpleXmlPath = PluginDirectory + "\\UI\\SimpleWindow.xml";

                // Debug: Check simple XML file
                if (!System.IO.File.Exists(simpleXmlPath))
                {
                    Chat.WriteLine($"ERROR: Simple XML file does not exist at: {simpleXmlPath}");
                    return;
                }

                SettingsController.settingsWindow = Window.CreateFromXml("Research Manager",
                    simpleXmlPath,
                    windowSize: new Rect(50, 50, 620, 450),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.NoFade);

                if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsValid)
                {
                    SettingsController.settingsWindow.Show(true);

                    // The SimpleWindow.xml uses individual TextViews for each research line
                    // These will be updated automatically by UpdateIndividualResearchDisplays()
                    // which is called every 2 seconds in OnUpdate()
                }
                else
                {
                    Chat.WriteLine("Failed to create simple window!");
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine($"Error in OpenResearchWindowSafe: {e.Message}");
            }
        }

        private void OpenResearchWindowDirect()
        {
            try
            {
                Chat.WriteLine("OpenResearchWindowDirect called");

                // Close existing window if open
                if (_directWindow != null)
                {
                    try
                    {
                        Chat.WriteLine("Closing existing window");
                        if (_directWindow.IsVisible)
                        {
                            _directWindow.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Chat.WriteLine($"Error closing existing window: {ex.Message}");
                    }
                    _directWindow = null;
                }

                // Reset references
                ResetWindowReferences();

                // Add delay to ensure cleanup
                System.Threading.Thread.Sleep(200);

                // Create window directly (like rmtest2 that works)
                Chat.WriteLine("Creating new window");
                _directWindow = Window.CreateFromXml("Research Manager",
                    PluginDirectory + "\\UI\\ResearchManagerSettingWindow.xml",
                    windowSize: new Rect(50, 50, 720, 400),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                if (_directWindow != null)
                {
                    _directWindow.Show(true);
                    Chat.WriteLine("Research Manager window opened!");

                    // Debug: Check if XML loaded correctly
                    Chat.WriteLine($"Window IsValid: {_directWindow.IsValid}");
                    Chat.WriteLine($"Window IsVisible: {_directWindow.IsVisible}");

                    // Debug: List all available views
                    Chat.WriteLine($"Window has {_directWindow.Views.Count} top-level views:");
                    foreach (var view in _directWindow.Views)
                    {
                        Chat.WriteLine($"  View: {view.GetType().Name}");
                    }

                    // Try a different approach - search for ANY MultiListView
                    Chat.WriteLine("Searching for ANY MultiListView in window...");
                    foreach (var view in _directWindow.Views)
                    {
                        if (SearchForAnyMultiListView(view))
                        {
                            Chat.WriteLine("Found a MultiListView!");
                            InitializeResearchDisplay();
                            break;
                        }
                    }

                    // Try to find the MultiListView for research display
                    if (_directWindow.FindView("researchLevelsList", out MultiListView listView1))
                    {
                        _researchLevelsList = listView1;
                        Chat.WriteLine("Found research list view!");

                        // Initialize the display immediately
                        InitializeResearchDisplay();
                    }
                    else
                    {
                        Chat.WriteLine("Could not find research list view - searching recursively...");

                        // Try recursive search through all views
                        foreach (var view in _directWindow.Views)
                        {
                            Chat.WriteLine($"Searching in view: {view.GetType().Name}");
                            if (FindMultiListViewRecursive(view, "researchLevelsList"))
                            {
                                Chat.WriteLine("Found research list view via recursive search!");
                                InitializeResearchDisplay();
                                break;
                            }
                        }

                        if (_researchLevelsList == null)
                        {
                            Chat.WriteLine("Still could not find research list view!");
                        }
                    }
                }
                else
                {
                    Chat.WriteLine("Failed to create Research Manager window!");
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine($"Error opening Research Manager window: {e.Message}");
            }
        }

        private void ResetWindowReferences()
        {
            try
            {
                // Reset all window-related references when window is closed
                _researchLevelsList = null;
                _researchDisplayInitialized = false;

                // Clear all collections
                if (_researchEntryViews != null)
                {
                    _researchEntryViews.Clear();
                }
                if (_lastResearchLevels != null)
                {
                    _lastResearchLevels.Clear();
                }
                if (_lastResearchProgress != null)
                {
                    _lastResearchProgress.Clear();
                }

                // Force garbage collection to clean up any lingering references
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        private int GetPerkLevel(int perkId)
        {
            if (apotheosis.Contains(perkId))
                return ((perkId - 2) % 10) + 1;
            else
                return (perkId % 10) + 1;
        }

        private void InitializeResearchData()
        {
            // Initialize comprehensive research names mapping for all professions
            _researchNames = new Dictionary<int, string>();

            // Apotheosis (available to all classes)
            for (int i = 0; i < 10; i++)
            {
                _researchNames[10002 + i] = "Apotheosis";
            }

            // Bureaucrat research lines (2160-2229)
            for (int i = 0; i < 10; i++) _researchNames[2167 + i] = "Human Resources";
            for (int i = 0; i < 10; i++) _researchNames[2177 + i] = "Team Building";
            for (int i = 0; i < 10; i++) _researchNames[2188 + i] = "Process Theory";
            for (int i = 0; i < 10; i++) _researchNames[2198 + i] = "Executive Decisions";
            for (int i = 0; i < 10; i++) _researchNames[2208 + i] = "Professional Development";
            for (int i = 0; i < 10; i++) _researchNames[2217 + i] = "Market Awareness";
            for (int i = 0; i < 10; i++) _researchNames[2228 + i] = "Hostile Negotiations";

            // Add other profession research lines as needed
            // Agent research lines (2000-2069)
            for (int i = 0; i < 10; i++) _researchNames[2000 + i] = "Concealment";
            for (int i = 0; i < 10; i++) _researchNames[2010 + i] = "Psychology";
            for (int i = 0; i < 10; i++) _researchNames[2020 + i] = "Tutoring";
            for (int i = 0; i < 10; i++) _researchNames[2030 + i] = "Weapon Smithing";
            for (int i = 0; i < 10; i++) _researchNames[2040 + i] = "Pharma Tech";
            for (int i = 0; i < 10; i++) _researchNames[2050 + i] = "Nano Programming";
            for (int i = 0; i < 10; i++) _researchNames[2060 + i] = "Quantum FT";

            // Get all research IDs from available goals
            _allResearchIds = new List<int>();
            try
            {
                if (Research.Goals != null)
                {
                    _allResearchIds = Research.Goals.Select(g => g.ResearchId).Distinct().OrderBy(id => id).ToList();
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error initializing research data: {ex.Message}");
            }
        }

        private void UpdateResearchLevelsDisplay()
        {
            try
            {
                // Find the research levels list if not already found
                if (_researchLevelsList == null)
                {
                    if (SettingsController.settingsWindow != null && SettingsController.settingsWindow.IsVisible)
                    {
                        if (SettingsController.settingsWindow.FindView("researchLevelsList", out MultiListView listView))
                        {
                            _researchLevelsList = listView;
                        }
                        else
                        {
                            // Try alternative search methods - search through the window's views
                            foreach (var view in SettingsController.settingsWindow.Views)
                            {
                                SearchForMultiListViewByType(view);
                                if (_researchLevelsList != null) // Stop searching once found
                                    break;
                            }
                        }
                    }

                    if (_researchLevelsList == null)
                        return;
                }

                // Initialize display if not done yet
                if (!_researchDisplayInitialized)
                {
                    InitializeResearchDisplay();
                    _researchDisplayInitialized = true;
                    return;
                }

                // Update existing entries with live data (like stack manager does)
                UpdateLiveResearchData();
            }
            catch (Exception ex)
            {
                // Silently handle errors to avoid spam (like stack manager)
            }
        }

        private void InitializeResearchDisplay()
        {
            try
            {
                // Clear existing entries only during initialization
                _researchLevelsList.DeleteAllChildren();
                _researchEntryViews.Clear();
                _lastResearchLevels.Clear();
                _lastResearchProgress.Clear();

                // Get available research lines dynamically
                var researchLines = GetAvailableResearchLines();



                if (researchLines.Count == 0)
                {
                    Chat.WriteLine("No research lines found - skipping display initialization");
                    return;
                }

                foreach (var researchLine in researchLines)
                {
                    try
                    {
                        // Create the display entry
                        var entry = View.CreateFromXml(PluginDirectory + "\\UI\\ResearchLevelEntry.xml");
                        if (entry != null)
                        {
                            // Set the research name
                            if (entry.FindChild("researchName", out TextView nameView))
                            {
                                nameView.Text = researchLine.name;
                            }

                            // Store the entry for live updates
                            _researchEntryViews[researchLine.name] = entry;

                            // Initialize cache values
                            _lastResearchLevels[researchLine.name] = -1; // Force initial update
                            _lastResearchProgress[researchLine.name] = -1f; // Force initial update

                            _researchLevelsList.AddChild(entry, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Silently handle errors during initialization
                    }
                }

                _researchDisplayInitialized = true;
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        private List<(string name, int baseId, List<int> availableIds)> GetAvailableResearchLines()
        {
            var researchLines = new List<(string name, int baseId, List<int> availableIds)>();

            try
            {
                if (Research.Goals == null)
                {
                    Chat.WriteLine("Research.Goals is null");
                    return researchLines;
                }

                var availableGoals = Research.Goals.Where(g => g.Available).ToList();
                if (!availableGoals.Any())
                {
                    Chat.WriteLine("No available research goals found");
                    return researchLines;
                }

                var availableIds = availableGoals.Select(g => g.ResearchId).ToList();

                // Group research IDs by research line (every 10 IDs = one line)
                var groupedByLine = availableIds.GroupBy(id => (id / 10) * 10).OrderBy(g => g.Key);

                foreach (var group in groupedByLine)
                {
                    try
                    {
                        int baseId = group.Key;
                        var lineIds = group.OrderBy(id => id).ToList();

                        // Get research name from the first ID in the line
                        string researchName = "Unknown Research";
                        if (lineIds.Any() && _researchNames.ContainsKey(lineIds.First()))
                        {
                            researchName = _researchNames[lineIds.First()];
                        }

                        // Skip Apotheosis unless specifically enabled
                        if (researchName == "Apotheosis" && !includeApotheosis)
                            continue;

                        researchLines.Add((researchName, baseId, lineIds));
                    }
                    catch (Exception groupEx)
                    {
                        Chat.WriteLine($"Error processing research group: {groupEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error getting research lines: {ex.Message}");
            }

            return researchLines;
        }

        private void UpdateLiveResearchData()
        {
            try
            {
                // Get available research lines dynamically
                var researchLines = GetAvailableResearchLines();

                foreach (var researchLine in researchLines)
                {
                    UpdateResearchEntry(researchLine);
                }
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        private void UpdateResearchEntry((string name, int baseId, List<int> availableIds) researchLine)
        {
            try
            {
                if (!_researchEntryViews.ContainsKey(researchLine.name))
                    return;

                var entry = _researchEntryViews[researchLine.name];
                if (entry == null)
                    return;

                // Calculate current level and progress
                int currentLevel = 0;
                float progress = 0f;
                bool isCurrentlyTraining = false;
                int currentTrainingId = DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal);

                // Check each available research ID in this line
                foreach (int researchId in researchLine.availableIds.OrderBy(id => id))
                {
                    try
                    {
                        // Check if this level is completed
                        float perkProgress = N3EngineClientAnarchy.GetPerkProgress((uint)researchId);
                        if (perkProgress >= 1.0f)
                        {
                            // This level is completed
                            currentLevel = (researchId % 10) + 1;
                        }
                        else if (perkProgress > 0f)
                        {
                            // This is the current level being worked on
                            currentLevel = (researchId % 10);
                            progress = perkProgress;
                            isCurrentlyTraining = (currentTrainingId == researchId);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip this research ID if there's an error
                        continue;
                    }
                }

                // Only update if values changed (like stack manager does)
                bool levelChanged = !_lastResearchLevels.ContainsKey(researchLine.name) ||
                                   _lastResearchLevels[researchLine.name] != currentLevel;
                bool progressChanged = !_lastResearchProgress.ContainsKey(researchLine.name) ||
                                      Math.Abs(_lastResearchProgress[researchLine.name] - progress) > 0.001f;

                if (levelChanged || progressChanged)
                {
                    // Update level display
                    if (entry.FindChild("researchLevel", out TextView levelView))
                    {
                        levelView.Text = $"Level {currentLevel}";
                    }

                    // Update progress percentage
                    if (entry.FindChild("researchProgress", out TextView progressView))
                    {
                        progressView.Text = $"{(progress * 100):F0}%";
                    }

                    // Update visual progress bar
                    UpdateProgressBar(entry, progress);

                    // Update status indicator
                    if (entry.FindChild("researchStatus", out TextView statusView))
                    {
                        if (isCurrentlyTraining)
                        {
                            statusView.Text = "●"; // Active training indicator
                        }
                        else if (progress > 0)
                        {
                            statusView.Text = "◐"; // Partial progress indicator
                        }
                        else
                        {
                            statusView.Text = "○"; // Inactive indicator
                        }
                    }

                    // Update cache
                    _lastResearchLevels[researchLine.name] = currentLevel;
                    _lastResearchProgress[researchLine.name] = progress;
                }
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        private void UpdateProgressBar(View entry, float progress)
        {
            try
            {
                if (entry.FindChild("progressBarFill", out TextView fillView))
                {
                    // Create a visual progress bar using block characters
                    int totalBlocks = 20;
                    int filledBlocks = (int)(progress * totalBlocks);

                    string progressBar = new string('█', filledBlocks) + new string('░', totalBlocks - filledBlocks);
                    fillView.Text = progressBar;
                }
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        private void SearchForMultiListViewByType(View view)
        {
            try
            {
                if (view is MultiListView mlv)
                {
                    _researchLevelsList = mlv;
                    Chat.WriteLine("ResearchManager: Found MultiListView via type search!");
                    return;
                }

                // Try to access children using reflection with multiple possible field names
                string[] possibleChildrenFields = { "_children", "children", "_childViews", "childViews" };

                foreach (string fieldName in possibleChildrenFields)
                {
                    var childrenField = view.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (childrenField != null)
                    {
                        var children = childrenField.GetValue(view) as System.Collections.IEnumerable;
                        if (children != null)
                        {
                            foreach (View child in children)
                            {
                                SearchForMultiListViewByType(child);
                                if (_researchLevelsList != null) // Stop searching once found
                                    return;
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently handle errors
            }
        }

        private bool FindMultiListViewRecursive(View view, string targetName)
        {
            try
            {
                Chat.WriteLine($"  Checking view: {view.GetType().Name}");

                if (view is MultiListView mlv)
                {
                    _researchLevelsList = mlv;
                    Chat.WriteLine("ResearchManager: Found MultiListView via recursive search!");
                    return true;
                }

                // Try to find child views using FindChild method
                if (view.FindChild(targetName, out MultiListView foundView))
                {
                    _researchLevelsList = foundView;
                    Chat.WriteLine($"Found {targetName} using FindChild!");
                    return true;
                }

                // Try to access children using Views property first
                try
                {
                    var viewsProperty = view.GetType().GetProperty("Views", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (viewsProperty != null)
                    {
                        var children = viewsProperty.GetValue(view) as System.Collections.IEnumerable;
                        if (children != null)
                        {
                            Chat.WriteLine($"  {view.GetType().Name} has Views property, searching...");
                            foreach (View child in children)
                            {
                                if (FindMultiListViewRecursive(child, targetName))
                                    return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Chat.WriteLine($"  Error accessing Views property: {ex.Message}");
                }

                // Try to access children using reflection with multiple possible field names
                string[] possibleChildrenFields = { "_children", "children", "_childViews", "childViews", "_views", "views" };

                foreach (string fieldName in possibleChildrenFields)
                {
                    var childrenField = view.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (childrenField != null)
                    {
                        var children = childrenField.GetValue(view) as System.Collections.IEnumerable;
                        if (children != null)
                        {
                            Chat.WriteLine($"  {view.GetType().Name} has {fieldName} field, searching...");
                            foreach (View child in children)
                            {
                                if (FindMultiListViewRecursive(child, targetName))
                                    return true;
                            }
                        }
                        else
                        {
                            Chat.WriteLine($"  {view.GetType().Name} {fieldName} field is null");
                        }
                        break; // Found the field, don't try others
                    }
                }

                Chat.WriteLine($"  {view.GetType().Name} has no accessible children fields");

                return false;
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error in recursive search: {ex.Message}");
                return false;
            }
        }

        private bool SearchForAnyMultiListView(View view)
        {
            try
            {
                Chat.WriteLine($"    Checking {view.GetType().Name} for MultiListView...");

                if (view is MultiListView mlv)
                {
                    _researchLevelsList = mlv;
                    Chat.WriteLine("    Found MultiListView!");
                    return true;
                }

                // Try Views property
                try
                {
                    var viewsProperty = view.GetType().GetProperty("Views", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (viewsProperty != null)
                    {
                        var children = viewsProperty.GetValue(view) as System.Collections.IEnumerable;
                        if (children != null)
                        {
                            foreach (View child in children)
                            {
                                if (SearchForAnyMultiListView(child))
                                    return true;
                            }
                        }
                    }
                }
                catch { }

                // Try reflection for children fields
                string[] possibleChildrenFields = { "_children", "children", "_childViews", "childViews", "_views", "views" };

                foreach (string fieldName in possibleChildrenFields)
                {
                    try
                    {
                        var childrenField = view.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        if (childrenField != null)
                        {
                            var children = childrenField.GetValue(view) as System.Collections.IEnumerable;
                            if (children != null)
                            {
                                foreach (View child in children)
                                {
                                    if (SearchForAnyMultiListView(child))
                                        return true;
                                }
                            }
                            break;
                        }
                    }
                    catch { }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool FindResearchDisplayView(Window window)
        {
            try
            {
                Chat.WriteLine("Searching for ANY TextView in window...");

                var allViews = new List<View>();
                foreach (var view in window.Views)
                {
                    allViews.AddRange(GetAllViewsRecursively(view));
                }

                foreach (var view in allViews)
                {
                    Chat.WriteLine($"  Checking: {view.GetType().Name}");
                    if (view is TextView tv)
                    {
                        _researchDisplayView = tv;
                        Chat.WriteLine($"  Found TextView: {view.GetType().Name}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error searching for TextView: {ex.Message}");
                return false;
            }
        }

        private bool FindAnyMultiListViewInWindow(Window window)
        {
            try
            {
                Chat.WriteLine("Deep searching entire window for MultiListView...");

                // Use reflection to get ALL views in the window
                var allViews = new List<View>();
                foreach (var view in window.Views)
                {
                    allViews.AddRange(GetAllViewsRecursively(view));
                }
                Chat.WriteLine($"Found {allViews.Count} total views in window");

                foreach (var view in allViews)
                {
                    Chat.WriteLine($"  Checking: {view.GetType().Name}");
                    if (view is MultiListView mlv)
                    {
                        _researchLevelsList = mlv;
                        Chat.WriteLine($"  Found MultiListView: {view.GetType().Name}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error in deep search: {ex.Message}");
                return false;
            }
        }

        private List<View> GetAllViewsRecursively(View parentView)
        {
            var allViews = new List<View>();

            try
            {
                allViews.Add(parentView);

                // Try multiple ways to get children
                var children = new List<View>();

                // Method 1: Views property (only for non-Window views)
                try
                {
                    var viewsProperty = parentView.GetType().GetProperty("Views");
                    if (viewsProperty != null)
                    {
                        var viewsCollection = viewsProperty.GetValue(parentView) as System.Collections.IEnumerable;
                        if (viewsCollection != null)
                        {
                            foreach (View child in viewsCollection)
                            {
                                children.Add(child);
                            }
                        }
                    }
                }
                catch { }

                // Method 2: Reflection on various field names
                string[] possibleFields = { "_children", "children", "_childViews", "childViews", "_views", "views" };
                foreach (string fieldName in possibleFields)
                {
                    try
                    {
                        var field = parentView.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        if (field != null)
                        {
                            var fieldValue = field.GetValue(parentView) as System.Collections.IEnumerable;
                            if (fieldValue != null)
                            {
                                foreach (View child in fieldValue)
                                {
                                    if (!children.Contains(child))
                                        children.Add(child);
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Recursively get children of children
                foreach (var child in children)
                {
                    allViews.AddRange(GetAllViewsRecursively(child));
                }
            }
            catch { }

            return allViews;
        }

        private void CreateProgrammaticUI()
        {
            try
            {
                Chat.WriteLine("Creating programmatic UI to bypass XML parsing issues...");

                // Close the broken XML window
                if (SettingsController.settingsWindow != null)
                {
                    SettingsController.settingsWindow.Close();
                    SettingsController.settingsWindow = null;
                }

                // Create a simple window using the same method as rmtest2 (which works)
                SettingsController.settingsWindow = Window.CreateFromXml("Research Manager",
                    PluginDirectory + "\\UI\\SimpleWindow.xml",
                    windowSize: new Rect(50, 50, 720, 400),
                    windowStyle: WindowStyle.Default,
                    windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);

                if (SettingsController.settingsWindow != null)
                {
                    SettingsController.settingsWindow.Show(true);
                    Chat.WriteLine("Created simple window successfully!");

                    // Find the research display TextView and populate it
                    if (SettingsController.settingsWindow.FindView("researchDisplay", out TextView displayView))
                    {
                        Chat.WriteLine("Found research display TextView - populating with data!");
                        PopulateResearchDisplay(displayView);
                    }
                    else
                    {
                        Chat.WriteLine("Could not find research display TextView");
                        // Fall back to chat display
                        CreateSimpleResearchDisplay();
                    }
                }
                else
                {
                    Chat.WriteLine("Failed to create simple window!");
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error creating programmatic UI: {ex.Message}");
            }
        }

        private void CreateSimpleResearchDisplay()
        {
            try
            {
                Chat.WriteLine("Creating simple research display...");

                // Get research data
                var researchLines = GetAvailableResearchLines();
                Chat.WriteLine($"Found {researchLines.Count} research lines to display");

                if (researchLines.Count > 0)
                {
                    // Create a simple text summary and display it in chat
                    Chat.WriteLine("=== RESEARCH PROGRESS ===");

                    foreach (var researchLine in researchLines)
                    {
                        // Calculate current level and progress
                        int currentLevel = 0;
                        float progress = 0f;
                        bool isCurrentlyTraining = false;
                        int currentTrainingId = DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal);

                        // Check each available research ID in this line
                        foreach (int researchId in researchLine.availableIds.OrderBy(id => id))
                        {
                            try
                            {
                                float perkProgress = N3EngineClientAnarchy.GetPerkProgress((uint)researchId);
                                if (perkProgress >= 1.0f)
                                {
                                    currentLevel = (researchId % 10) + 1;
                                }
                                else if (perkProgress > 0f)
                                {
                                    currentLevel = (researchId % 10);
                                    progress = perkProgress;
                                    isCurrentlyTraining = (currentTrainingId == researchId);
                                    break;
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }

                        string status = isCurrentlyTraining ? " (TRAINING)" : "";
                        string progressText = progress > 0 ? $" ({(progress * 100):F0}%)" : "";

                        Chat.WriteLine($"{researchLine.name}: Level {currentLevel}{progressText}{status}");
                    }

                    Chat.WriteLine("========================");
                    Chat.WriteLine("Research display created successfully!");
                }
                else
                {
                    Chat.WriteLine("No research lines found to display");
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error creating simple display: {ex.Message}");
            }
        }

        private void PopulateResearchDisplay(TextView displayView = null)
        {
            try
            {
                // Update individual TextViews instead of one big text block
                UpdateIndividualResearchDisplays();

                // If a displayView was passed (legacy support), show a simple message
                if (displayView != null)
                {
                    displayView.Text = "Research data displayed in individual fields above.\nUpdates every 2 seconds.";
                }

                Chat.WriteLine("Research display populated successfully!");
            }
            catch (Exception ex)
            {
                if (displayView != null)
                {
                    displayView.Text = $"Error loading research data: {ex.Message}";
                }
                Chat.WriteLine($"Error populating display: {ex.Message}");
            }
        }

        private void UpdateIndividualResearchDisplays()
        {
            try
            {
                int currentTrainingId = DynelManager.LocalPlayer.GetStat(Stat.PersonalResearchGoal);

                // Update Apotheosis Research
                if (includeApotheosis)
                {
                    // Apotheosis is a single research line, show it in the first slot
                    string viewName = "apotheosis1";
                    int baseId = 10002; // Apotheosis base ID
                    var (level, progress, isTraining) = GetResearchStatus(baseId, currentTrainingId);
                    string status = isTraining ? " (TRAINING)" : "";
                    string progressText = progress > 0 ? $" ({(progress * 100):F0}%)" : "";
                    string text = $"Apotheosis: Level {level}{progressText}{status}";
                    UpdateTextView(viewName, text);

                    // Hide the other apotheosis slots since there's only one Apotheosis research line
                    for (int i = 2; i <= 5; i++)
                    {
                        UpdateTextView($"apotheosis{i}", "");
                    }
                }
                else
                {
                    // Hide Apotheosis research when not enabled
                    for (int i = 1; i <= 5; i++)
                    {
                        string viewName = $"apotheosis{i}";
                        UpdateTextView(viewName, "");
                    }
                }

                // Update Profession-specific Research
                var researchLines = GetAvailableResearchLines();
                for (int i = 0; i < 7; i++) // Support up to 7 profession research lines
                {
                    string viewName = $"profResearch{i + 1}";
                    if (i < researchLines.Count)
                    {
                        var researchLine = researchLines[i];
                        var (level, progress, isTraining) = GetProfessionResearchStatus(researchLine, currentTrainingId);
                        string status = isTraining ? " (TRAINING)" : "";
                        string progressText = progress > 0 ? $" ({(progress * 100):F0}%)" : "";
                        string text = $"{researchLine.name}: Level {level}{progressText}{status}";
                        UpdateTextView(viewName, text);
                    }
                    else
                    {
                        // Hide unused research lines
                        UpdateTextView(viewName, "");
                    }
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error updating individual research displays: {ex.Message}");
            }
        }

        private void UpdateTextView(string viewName, string text)
        {
            try
            {
                if (SettingsController.settingsWindow?.FindView(viewName, out TextView textView) == true)
                {
                    textView.Text = text;
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"Error updating TextView {viewName}: {ex.Message}");
            }
        }

        private (int level, float progress, bool isTraining) GetResearchStatus(int baseId, int currentTrainingId)
        {
            int currentLevel = 0;
            float progress = 0f;
            bool isCurrentlyTraining = false;

            // Check levels 1-10
            for (int level = 1; level <= 10; level++)
            {
                int researchId = baseId + level;
                try
                {
                    float perkProgress = N3EngineClientAnarchy.GetPerkProgress((uint)researchId);
                    if (perkProgress >= 1.0f)
                    {
                        currentLevel = level;
                    }
                    else if (perkProgress > 0f)
                    {
                        currentLevel = level - 1;
                        progress = perkProgress;
                        isCurrentlyTraining = (currentTrainingId == researchId);
                        break;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return (currentLevel, progress, isCurrentlyTraining);
        }

        private (int level, float progress, bool isTraining) GetProfessionResearchStatus((string name, int baseId, List<int> availableIds) researchLine, int currentTrainingId)
        {
            int currentLevel = 0;
            float progress = 0f;
            bool isCurrentlyTraining = false;

            // Check each available research ID in this line
            foreach (int researchId in researchLine.availableIds.OrderBy(id => id))
            {
                try
                {
                    float perkProgress = N3EngineClientAnarchy.GetPerkProgress((uint)researchId);
                    if (perkProgress >= 1.0f)
                    {
                        currentLevel = (researchId % 10) + 1;
                    }
                    else if (perkProgress > 0f)
                    {
                        currentLevel = (researchId % 10);
                        progress = perkProgress;
                        isCurrentlyTraining = (currentTrainingId == researchId);
                        break;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return (currentLevel, progress, isCurrentlyTraining);
        }

        enum ModeSelection
        {
            LowestFirst,
            HighestFirst
        }
    }
}
