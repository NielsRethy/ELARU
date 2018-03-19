using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: AssemblyVersion("0.1.*")]
public class BuildVersion
{
    static string _version = null;

    public static string Version
    {
        get
        {
            // Return the static version value if it exists already
            if (_version != null)
                return _version;

            // Fetch the version
            _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // Return unknown if no version was found
            return _version.Length <= 0 ? "Unknown" : _version;
        }
    }
}

/// <summary>
/// A script for loading and saving crucial game state information.
/// </summary>
public class Script_SaveFileManager : MonoBehaviour
{
    #region Singleton instance (DontDestroyOnLoad)
    private static Script_SaveFileManager _instance;
    private static object _lock = new object(); // multi threading
    public static Script_SaveFileManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance != null)
                    return _instance;

                // Create instance if there is none
                Debug.Log("No SaveFileManager instance yet, creating one");
                _instance = new GameObject("SaveFileManager").AddComponent<Script_SaveFileManager>();
                DontDestroyOnLoad(_instance);

                return _instance;
            }
        }
    }
    #endregion


    #region SaveState class
    [Serializable]
    public class SaveState
    {
        // List stuff that needs to be saved here as public variables
        // Tutorial state
        public bool CompletedTutorial = false;
        // Weapon upgrade state
        public List<int> GunMods = new List<int>();
        public List<int> SwordMods = new List<int>();
        public List<bool> HasBeenFound = new List<bool>();
        // Quest manager
        public List<QuestLinkerSaveData> QuestLinks = new List<QuestLinkerSaveData>();
        // Achievements
        public List<AchievementSaveData> Achievements = new List<AchievementSaveData>();
        // Player height
        public float PlayerWaistHeight = 0.5f;
        public float PlayerHeight = 1f;
        // Player position
        public float PlayerSpawnPositionX = 0f;
        public float PlayerSpawnPositionY = 0f;
        public float PlayerSpawnPositionZ = 0f;
        // Player data
        public int PlayerLevel = 0;
        public int PlayerExp = 0;
        // Difficulty setting (TODO: Stretch goal)
        // Mini game manager
        public List<uint> PuzzleIDs = new List<uint>();
        // Enemy manager
        public List<Script_ManagerEnemy.Patrol> Patrols = new List<Script_ManagerEnemy.Patrol>();
        public Dictionary<int, int> ObjectPatrolLink = new Dictionary<int, int>();

        // Constructor
        public SaveState() { }

        // Method(s)
        /// <summary>
        /// Shorthand for setting the weapon data lists
        /// </summary>
        /// <param name="gunMods">List of gun modifications</param>
        /// <param name="swordMods">List of sword modifications</param>
        /// <param name="found">List of weapon found states</param>
        public void SetWeaponData(List<int> gunMods, List<int> swordMods, List<bool> found)
        {
            GunMods = gunMods;
            SwordMods = swordMods;
            HasBeenFound = found;
        }

        /// <summary>
        /// Shorthand for retrieving or setting a Vector3 of the player position
        /// </summary>
        public Vector3 PlayerSpawnPosition
        {
            get { return new Vector3(PlayerSpawnPositionX, PlayerSpawnPositionY, PlayerSpawnPositionZ); }
            set
            {
                PlayerSpawnPositionX = value.x;
                PlayerSpawnPositionY = value.y;
                PlayerSpawnPositionZ = value.z;
            }
        }

        /// <summary>
        /// Shorthand for setting the player and waist height values
        /// </summary>
        /// <param name="height">The player height</param>
        /// <param name="waist">The player waist height</param>
        public void SetHeight(float height, float waist)
        {
            PlayerHeight = height;
            PlayerWaistHeight = waist;
        }

        /// <summary>
        /// Shorthand for setting the player's level and experience values
        /// </summary>
        /// <param name="level">The player's level</param>
        /// <param name="exp">The player's accumulated experience</param>
        public void SetPlayerData(int level, int exp)
        {
            PlayerLevel = level;
            PlayerExp = exp;
        }

        /// <summary>
        /// Shorthand for setting the enemy manager patrol and patrol link data
        /// </summary>
        /// <param name="patrols">List of patrol info</param>
        /// <param name="patrolLinks">List of patrol links</param>
        public void SetEnemyData(List<Script_ManagerEnemy.Patrol> patrols, Dictionary<int, int> patrolLinks)
        {
            Patrols = patrols;
            ObjectPatrolLink = patrolLinks;
        }
    }
    #endregion

    // Variables
    private const int _maxBackups = 99;
    private const string _filePath = "/saveData";
    private const string _extension = ".dat";
    public static SaveState SaveData { get; private set; }
    [SerializeField]
    private Script_Tutorial _scriptTutorial = null;
    private bool _loaded = false;

    // Determines how high the docking stations for your weapons will be
    public static float DefaultHeight = 1.8f;

    #region Singletons
    private Script_PlayerInformation _scriptPlayerInformation = null;
    private Script_QuestManager _scriptQuestManager = null;
    private Script_MinigameManager _scriptMinigameManager = null;
    private Script_AchievementManager _scriptAchievementManager = null;
    private Script_WeaponManager _scriptWeaponManager = null;
    private Script_LocomotionBase _scriptLocomotion = null;

    private void GetSingletons()
    {
        if (_scriptPlayerInformation == null)
            _scriptPlayerInformation = Script_PlayerInformation.Instance;

        if (_scriptQuestManager == null)
            _scriptQuestManager = Script_QuestManager.Instance;

        if (_scriptMinigameManager == null)
            _scriptMinigameManager = Script_MinigameManager.Instance;

        if (_scriptAchievementManager == null)
            _scriptAchievementManager = Script_AchievementManager.Instance;

        if (_scriptWeaponManager == null)
            _scriptWeaponManager = Script_WeaponManager.Instance;

        if (_scriptLocomotion == null)
            _scriptLocomotion = Script_LocomotionBase.Instance;
    }
    #endregion

    private void Awake()
    {
        // Initial create
        if (SaveData == null)
            SaveData = new SaveState();

        Debug.Log("Current build version: " + BuildVersion.Version);

        GetSingletons();
    }

    #region Saving methods
    /// <summary>
    /// Method for saving all the current scene data
    /// </summary>
    /// <param name="quickSave">Store the save data inside of the singleton instead of saving to the hard drive</param>
    public void SaveSceneData(bool quickSave = false)
    {
        //Safety check singletons
        GetSingletons();

        // Tutorial state
        if (_scriptTutorial != null)
            SaveData.CompletedTutorial = _scriptTutorial.TutorialCompleted;
        // Weapon upgrade state
        SaveData.SetWeaponData(_scriptWeaponManager.GunMods, _scriptWeaponManager.SwordMods, _scriptWeaponManager.FoundWeapons);
        // Quest manager
        SaveData.QuestLinks = _scriptQuestManager.GetQuestLinkerSaveDatas();
        // Achievements
        SaveData.Achievements = _scriptAchievementManager.GetAchievementSaveDatas();
        // Player height
        SaveData.SetHeight(_scriptPlayerInformation.PlayerHeight, _scriptPlayerInformation.PlayerWaistHeight);
        // Player position
        SaveData.PlayerSpawnPosition = _scriptPlayerInformation.PlayerSpawnPosition;
        // Player data
        SaveData.SetPlayerData(_scriptPlayerInformation.PlayerLevel, _scriptPlayerInformation.PlayerExp);
        // Difficulty setting
        // Mini game manager
        SaveData.PuzzleIDs = _scriptMinigameManager.PuzzleIDs;
        // Enemy manager
        SaveData.SetEnemyData(Script_ManagerEnemy.Patrols, Script_ManagerEnemy.ObjectPatrolLink);

        if (!quickSave)
            Save();
    }

    /// <summary>
    /// Serialize the SaveData class using the binary formatter
    /// </summary>
    /// <param name="filePath">Custom filepath for additional save files</param>
    private void Save(string filePath = _filePath)
    {
        string fullPath = Application.persistentDataPath + filePath + _extension;
        BinaryFormatter bf = new BinaryFormatter();

        if (!File.Exists(fullPath))
        {
            FileStream createFile = File.Create(fullPath);
            createFile.Close();
        }
        FileStream file = File.Open(fullPath, FileMode.Open);

        bf.Serialize(file, SaveData);
        file.Close();
    }
    #endregion

    #region Loading methods
    /// <summary>
    /// Method for setting all the current scene data to the appropriate scripts
    /// </summary>
    /// <param name="loadQuickSave">Load the currently stored save data instead of loading the save file</param>
    /// <param name="applySaveData">Load the SaveData but don't apply anything to the singletons</param>
    public void LoadSceneData(bool loadQuickSave = false, bool applySaveData = true)
    {
        _loaded = false;

        // Attempt to load existing save data
        if (!loadQuickSave)
            _loaded = Load();

        // Log warning if no save file was found 
        if (!_loaded)
            Debug.LogWarning("No save file found");

        if (!applySaveData)
            return;

        GetSingletons();

        // Set all the relevant variables inside the current scene
        if (_loaded || loadQuickSave)
        {
            // Weapon upgrades
            _scriptWeaponManager.GunMods = SaveData.GunMods;
            _scriptWeaponManager.SwordMods = SaveData.SwordMods;
            _scriptWeaponManager.FoundWeapons = SaveData.HasBeenFound;

            // Weapon state
            _scriptWeaponManager.Load();

            // Quest manager
            _scriptQuestManager.LoadQuestLinksFromSaveDatas(SaveData.QuestLinks);

            // Achievements
            _scriptAchievementManager.LoadAchievementsFromSaveDatas(SaveData.Achievements);

            // Player height
            _scriptPlayerInformation.PlayerHeight = SaveData.PlayerHeight;
            _scriptPlayerInformation.PlayerWaistHeight = SaveData.PlayerWaistHeight;
            _scriptLocomotion.CameraRig.GetComponent<Script_WeaponDocking>().UpdateDockingHeight();
            _scriptLocomotion.PlayerCollider.GetComponent<Script_PlayerColliderFollow>().SetColliderHeight(SaveData.PlayerHeight);

            // Player position
            _scriptPlayerInformation.PlayerSpawnPosition = SaveData.PlayerSpawnPosition;

            // Player data
            _scriptPlayerInformation.LoadFromSave(SaveData.PlayerLevel, SaveData.PlayerExp);

            // Difficulty setting

            // Mini games manager
            _scriptMinigameManager.PuzzleIDs = SaveData.PuzzleIDs;
            _scriptMinigameManager.Load();

            // Enemy manager
            Script_ManagerEnemy.Patrols = SaveData.Patrols;
            Script_ManagerEnemy.ObjectPatrolLink = SaveData.ObjectPatrolLink;

            // Debug (moved to SaveFileManagerEditor)
            //Debug.Log(GetLogLoadedData());
        }
    }

    /// <summary>
    /// Load and deserialize the SaveData class using the binary formatter
    /// </summary>
    /// <param name="filePath">Custom filepath for additional save files</param>
    /// <returns></returns>
    private bool Load(string filePath = _filePath)
    {
        var fullPath = Application.persistentDataPath + filePath + _extension;

        if (!File.Exists(fullPath))
            return false;

        // If there is a save file, attempt to load it
        var file = File.Open(fullPath, FileMode.Open);

        // Try catch block in case the saved SaveState class does not match the current one
        try
        {
            var bf = new BinaryFormatter();
            SaveData = (SaveState)bf.Deserialize(file);
        }
        catch
        {
            var info = new FileInfo(fullPath);

            Debug.LogWarning("Failed to load any data from: " + fullPath +
                    "\nCurrent version: " + BuildVersion.Version +
                    "\nLast write date: " + info.LastWriteTime);

            // Back up and create a new save file
            // TODO: Stretch goal: parse save file and add/remove missing data
            try
            {
                var backupPath = Application.persistentDataPath + filePath + "_bckup";

                // Don't overwrite backups
                var version = 0;
                while (File.Exists(backupPath + version.ToString("00") + _extension))
                {
                    // Prevent the game from making infinite files just in case though
                    if (++version > _maxBackups)
                        break;
                }

                // Close file before cloning (prevents access exception)
                file.Close();
                File.Copy(fullPath, backupPath + version.ToString("00") + _extension);
                Debug.Log("Backed up old save data to: " + backupPath + version.ToString("00") + _extension);
            }
            catch
            {
                Debug.LogError("Failed to back up old save file!");
            }
            finally
            {
                // Remove the current data
                ClearSaveData();
            }
        }
        // Close file regardless of exception
        finally
        {
            file.Close();
        }

        Debug.Log("Loaded save state from: " + fullPath);
        return true;
    }
    #endregion

#if UNITY_STANDALONE
    private void OnApplicationQuit()
    {
        //Auto save on exit
        //SaveSceneData();
    }
#endif

    /// <summary>
    /// Clears all the data in SaveData, removes the save file and quick loads the new empty values
    /// </summary>
    /// <param name="filePath">Custom filepath for additional save files</param>
    public void ClearSaveData(string filePath = _filePath)
    {
        SaveData = new SaveState();

        string fullPath = Application.persistentDataPath + filePath + _extension;
        // Remove existing save file
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    /// <summary>
    /// Shorthand for logging all the loaded data via the appropriate scripts in the current scene
    /// </summary>
    public string GetLogLoadedData()
    {
        // Prevent null references
        if (_scriptPlayerInformation == null ||
            _scriptMinigameManager == null ||
            _scriptAchievementManager == null ||
            _scriptQuestManager == null)
            return "";

        // Fetch a list of all the puzzles
        string puzzlesStr = "";
        var puzzleIDs = _scriptMinigameManager.PuzzleIDs;
        if (puzzleIDs.Count > 0)
            foreach (var puzzleId in puzzleIDs)
                puzzlesStr += puzzleId + "\n";

        // Fetch a list of all the achievements
        string achievementsStr = "";
        var achievements = _scriptAchievementManager.Achievements;
        if (achievements.Count > 0)
            foreach (var achievement in achievements)
                achievementsStr += achievement + "\n";

        // Fetch a list of all the quests
        string questsStr = "";
        var quests = _scriptQuestManager.QuestLinks;
        if (quests.Count > 0)
            foreach (Script_QuestLinker quest in quests)
                questsStr += quest + "\n";

        return ("Level: " + _scriptPlayerInformation.PlayerLevel
                + "\nExp: " + _scriptPlayerInformation.PlayerExp
                + "\nHeight: " + _scriptPlayerInformation.PlayerHeight.ToString("0.0")
                + "\nPuzzles: " + puzzleIDs.Count + puzzlesStr
                + "\nAchievements: " + achievements.Sum(x => x.CompletedTiers) + achievementsStr
                + "\nQuests: " + quests.Count(x => x.IsLinkCompleted) + questsStr
                );
    }

}
