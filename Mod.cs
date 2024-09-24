using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.GameModes;
using UnboundLib.Utils.UI;
using UnboundLib.Extensions;
using MapsExt;
using MapsExt.Compatibility;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UnboundLib.Utils;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Sirenix.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MapsExt.MapObjects;
using MapsExt.Visualizers;

namespace LopisParkourNS;
[BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)] // necessary for most modding stuff here
[BepInPlugin(ModId, ModName, Version)]
[BepInProcess("Rounds.exe")]
public class LopisParkour : BaseUnityPlugin
{
	public const string ModId = "lopi.rounds.plugins.lopisparkour";
	public const string ModName = "lopi's Parkour";
	public const string Version = "0.1.0";
	public static string CompatibilityModName => ModName.Replace(" ", "");
	public Harmony harmony;
	public static GameObject parkourMenu;
	public static GameObject pauseMenu;
	public static GameObject parkourWinMenu = null;
	public static List<GameObject> parkourMenuDirectories = new List<GameObject>();
	internal static readonly Material defaultMaterial = new Material(Shader.Find("Sprites/Default"));
	public static Dictionary<string, CustomMap> maps = new Dictionary<string, CustomMap>();
	public static List<string> mapsHierarchy = new List<string>();
	public static string saveFileName = "parkour.sav";
	public static string saveFilePath => Path.Combine(Paths.GameRootPath, saveFileName);
	
	public static LopisParkour? instance;
	public static void Log(object s)
	{
		if (instance != null) UnityEngine.Debug.Log($"[{ModName} {TimeSpan.FromSeconds(Time.time).ToString("hh\\:mm\\:ss\\.fff")}] {s}");
	}
	public static void LogError(object s)
	{
		if (instance != null) UnityEngine.Debug.LogError($"[{ModName} {TimeSpan.FromSeconds(Time.time).ToString("hh\\:mm\\:ss\\.fff")}] {s}");
	}
	public void Awake()
	{
		instance = this;
		try
		{
			harmony = new Harmony(ModId);
			// harmony.PatchAll();
			harmony.PatchAll(typeof(LopisParkour));
			harmony.Patch((MethodBase)AccessTools.Method(typeof(Unbound).GetNestedType("<AddTextWhenReady>d__39", BindingFlags.NonPublic), "MoveNext", (Type[])null, (Type[])null), (HarmonyMethod)null, new HarmonyMethod(AccessTools.Method(typeof(LopisParkour), "PushTheUnboundText", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		} catch(Exception ex)
		{
			LogError(ex);
		}
		Log("Loaded!");
		
	}

	public void Start()
	{
		UnboundLib.GameModes.GameModeManager.AddHandler<GM_Parkour>("Parkour", new ParkourHandler());
		AddTheButton();
		SceneManager.sceneLoaded +=  delegate(Scene scene, LoadSceneMode mode)
		{
			if (scene.name == "Main") AddTheButton();
		};
	}
	public static void ParkourMenu()
	{
		var contents = parkourMenu.transform.Find("Group/Grid/Scroll View/Viewport/Content");
		if (contents)
		foreach(Transform child in contents)
		{
			Destroy(child.gameObject);
		}
		MapsExtended.instance.UpdateMapFiles();
	
		parkourMenuDirectories.ForEach(x => Destroy(x));
		parkourMenuDirectories.Clear();
		GameObject.Instantiate(GameObject.Find("Game/UI/UI_MainMenu/Canvas/ListSelector/Online/Group/FaceSelector"), contents);
		MenuHandler.CreateText(" ", parkourMenu, out TextMeshProUGUI _, 30); 
		string[] prk_files = Directory.GetFiles(Path.Combine(Paths.GameRootPath, "maps"), "prk_*.map", SearchOption.TopDirectoryOnly);
		prk_files = prk_files.AddRangeToArray(Directory.GetFiles(Paths.PluginPath, "prk_*.map", SearchOption.AllDirectories));
		prk_files = prk_files.Where(s => s!="").ToArray();
		prk_files.Do(f => {
				if (!maps.ContainsKey(f)) maps.Add(f, MapLoader.LoadPath(f));
				else maps[f] = MapLoader.LoadPath(f);
				maps[f].id = f;
			});
		List<ParkourProgressEntry> progress = readProgress();
		mapsHierarchy = new List<string>();
		// mapsHierarchy.Add("CustomMaps");
		maps.AsEnumerable().Do(f => {
			var pluginFolder = "";
			if (f.Key.StartsWith(Paths.PluginPath))
			{
				pluginFolder = Path.Combine(Paths.PluginPath, f.Key.Remove(0, Paths.PluginPath.Length+1).Take(f.Key.Remove(0, Paths.PluginPath.Length+1).IndexOf(Path.DirectorySeparatorChar)).Join(delimiter: ""));
			}
			else if (f.Key.StartsWith(Paths.GameRootPath))
			{
				pluginFolder = Path.Combine(Paths.GameRootPath, f.Key.Remove(0, Paths.GameRootPath.Length+1).Take(f.Key.Remove(0, Paths.GameRootPath.Length+1).IndexOf(Path.DirectorySeparatorChar)).Join(delimiter: ""));
			}
			if (!mapsHierarchy.Contains(pluginFolder)) mapsHierarchy.Add(pluginFolder);
		});

		if (mapsHierarchy.Count > 1)
		{
			mapsHierarchy.ForEach(f => {
				var name = "";
				if (f.StartsWith(Paths.GameRootPath)) name = "Custom maps";
				else if (File.Exists(Path.Combine(f, "manifest.json")))
				{
					string manifest = File.ReadAllText(Path.Combine(f, "manifest.json"));
					var names = Regex.Matches(manifest, "\"name\": \"(.*?)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);
					if (names.Count > 0) name = names[0].Groups[1].Value;
				}
				if (name == "") name = Path.GetFileName(Path.GetDirectoryName(f).Remove(-1));
				var realName = name;
				name = name.Replace("_", " ");
				GameObject directoryMenuButton;
				parkourMenuDirectories.Add(MenuHandler.CreateMenu(
						name,
						()=>{
							LopisParkour.maps.AsEnumerable().Where(kvp => kvp.Key.Contains(f)).OrderBy( f => 
								{
									if(int.TryParse(Regex.Split(f.Value._name.Remove(0,4), "_")[0], out int n))
									{
										return n;
									}
									else {
										return (int)f.Value._name.Remove(0,4)[0];
									}
								}, Comparer<int>.Create((x, y) => x > y ?  1 : x < y ?  -1 : 0)
								)
								.Do( pair => {
									var buttonName = pair.Value._name.Remove(0,4).Replace("_", " ");
									var entry = progress.FirstOrDefault(ppe => ppe.map == pathToId(pair.Value.id));
									if (entry != null)
									{
										buttonName = entry.getMedalSymbol() + " " + buttonName + " <color=#555f>" + TimeSpan.FromSeconds(entry.time).ToString("mm\\:ss\\.fff");
									}
									else buttonName = "  " + buttonName;
									MenuHandler.CreateButton(
										buttonName,
										GameObject.Find(@"Game/UI/UI_MainMenu/Canvas/ListSelector/"+name+"/Group/Grid/Scroll View/Viewport/Content"), () => {
											TimeHandler.instance.gameOverTime = 1f;
											MainMenuHandler.instance.Close();
											UnboundLib.GameModes.GameModeManager.SetGameMode("Parkour");
											var gm = (GM_Parkour) GameModeManager.CurrentHandler.GameMode;
											
											gm.selectedMap = pair.Value;
											UnboundLib.GameModes.GameModeManager.CurrentHandler.StartGame();
										}, 60, false, alignmentOptions: TextAlignmentOptions.Left);
								});
						},
						parkourMenu, out directoryMenuButton));
			});
		}
		else maps.AsEnumerable().OrderBy( f => 
			{
				if(int.TryParse(Regex.Split(f.Value._name.Remove(0,4), "_")[0], out int n))
				{
					return n;
				}
				else {
					return (int)f.Value._name.Remove(0,4)[0];
				}
			}, Comparer<int>.Create((x, y) => x > y ?  1 : x < y ?  -1 : 0)
			)
			.Do( pair => {
				var name = pair.Value._name.Remove(0,4).Replace("_", " ");
				var entry = progress.FirstOrDefault(ppe => ppe.map == pathToId(pair.Value.id));
				if (entry != null)
				{
					name = entry.getMedalSymbol() + " " + name + " <color=#555f>" + TimeSpan.FromSeconds(entry.time).ToString("mm\\:ss\\.fff");
				}
				else name = "  " + name;
				MenuHandler.CreateButton(
					name,
					parkourMenu, () => {
						TimeHandler.instance.gameOverTime = 1f;
						MainMenuHandler.instance.Close();
						UnboundLib.GameModes.GameModeManager.SetGameMode("Parkour");
						var gm = (GM_Parkour) GameModeManager.CurrentHandler.GameMode;
						
						gm.selectedMap = pair.Value;
						UnboundLib.GameModes.GameModeManager.CurrentHandler.StartGame();
					}, 60, false, alignmentOptions: TextAlignmentOptions.Left);
		});
		MenuHandler.CreateText(" ", parkourMenu, out _);
		MenuHandler.CreateButton(
			"Get More Maps",
			parkourMenu, () => {
				var url = "https://discord.gg/XEpTTDhrDr";
				try // code from https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp
				{
					Process.Start(url);
				}
				catch
				{
					// hack because of this: https://github.com/dotnet/corefx/issues/10361
					if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						url = url.Replace("&", "^&");
						Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
					}
					else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					{
						Process.Start("xdg-open", url);
					}
					else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
					{
						Process.Start("open", url);
					}
					else
					{
						throw;
					}
				}
		});
		MenuHandler.CreateButton(
			"Map Creation Guide",
			parkourMenu, () => {
				var url = "https://docs.google.com/document/d/1kYegslKXzL2sf-BxvEWKecUuQ_RAQn5joaaG4d6Ur8E/edit?usp=sharing";
				try // code from https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp
				{
					Process.Start(url);
				}
				catch
				{
					// hack because of this: https://github.com/dotnet/corefx/issues/10361
					if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						url = url.Replace("&", "^&");
						Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
					}
					else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					{
						Process.Start("xdg-open", url);
					}
					else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
					{
						Process.Start("open", url);
					}
					else
					{
						throw;
					}
				}
		});

		if (!System.IO.Directory.Exists(Path.Combine(Paths.GameRootPath, "ParkourReplays"))) System.IO.Directory.CreateDirectory(Path.Combine(BepInEx.Paths.GameRootPath, "ParkourReplays"));
		string[] prkr_files = Directory.GetFiles(Path.Combine(Paths.GameRootPath, "ParkourReplays"), "*.prkr", SearchOption.TopDirectoryOnly);
		prkr_files = prkr_files.Where(s => s!="").ToArray();
		if (prkr_files.Length > 0) {
			var replayMenu = MenuHandler.CreateMenu("Your replays", ()=>{},parkourMenu, out var replayMenuButton);
			prkr_files.Do(x=> {
				MenuHandler.CreateButton(x, replayMenu, ()=>{
						GhostReplayRecord record = SerializationUtility.DeserializeValue<GhostReplayRecord>(File.ReadAllBytes(x), DataFormat.Binary);
						TimeHandler.instance.gameOverTime = 1f;
						MainMenuHandler.instance.Close();
						UnboundLib.GameModes.GameModeManager.SetGameMode("Parkour");
						var gm = (GM_Parkour) GameModeManager.CurrentHandler.GameMode;
						gm.selectedMap = MapLoader.LoadPath(record.mapId);
						gm.selectedMap.id = record.mapId;
						var player = gm.gameObject.AddComponent<GhostReplayPlayer>();
						player.replayRecord = record.timedPositions;
						player.mapId = record.mapId;
						UnboundLib.GameModes.GameModeManager.CurrentHandler.StartGame();

				});
			});
		}

		parkourWinMenu = MenuHandler.CreateMenu("UI_PARKOURWIN", ()=>{TimeHandler.instance.gameOverTime = 1f;}, parkourMenu, out GameObject winMenuButton, 60);
		winMenuButton.SetActive(false);
	}
	public static string pathToId(string path)
	{
		if (path.StartsWith(Paths.PluginPath))
		{
			path = Path.Combine(path.Remove(0, Paths.PluginPath.Length+1).Take(path.Remove(0, Paths.PluginPath.Length+1).IndexOf(Path.DirectorySeparatorChar)).Join(delimiter: ""), Path.GetFileName(path));
		}
		else if (path.StartsWith(Paths.GameRootPath))
		{
			path = Path.Combine(path.Remove(0, Paths.GameRootPath.Length+1).Take(path.Remove(0, Paths.GameRootPath.Length+1).IndexOf(Path.DirectorySeparatorChar)).Join(delimiter: ""), Path.GetFileName(path));
		}
		return path;
	}
	public static void RateMap(string path, int rate)
	{
		path = pathToId(path);
		Log("rating a map");
		List<ParkourProgressEntry> progress = readProgress();
		var entry = progress.Find(s => s.map == path);
		if (entry == null) return;
		entry.rated = rate;
		saveProgress(progress);
	}
	public static bool AddRecord(string path, float time, MedalType medal)
	{
		path = pathToId(path);
		Log("adding a record!");
		Log(path);
		Log(time);
		Log(medal);
		string savePath = Path.Combine(Paths.GameRootPath, saveFileName);
		bool beaten = false;
		List<ParkourProgressEntry> progress = new List<ParkourProgressEntry>();
		if (File.Exists(savePath))
		{
			progress = readProgress();
		}
		var entry = progress.FirstOrDefault(s => s.map == path);
		Log("entry:");
		Log(entry == null);
		if (entry == null)
		{
			entry = new ParkourProgressEntry(path, time, medal);
			progress.Add(entry);
			beaten = true;

		}
		else if (entry.time > time) {
			entry.time = time;
			entry.medal = medal;
			beaten = true;
		}
		else
		{
			return false;
		}
		Log(progress.Count);
		saveProgress(progress);
		return beaten;
	}
	public static ParkourProgressEntry? GetRecord(string path)
	{
		path = pathToId(path);
		if (!File.Exists(saveFilePath))
		{
			return null;
		}
		List<ParkourProgressEntry> progress = readProgress();
		var entry = progress.Find(s => s.map == path);
		if (entry == null) return null;
		return entry;
	}
	public static List<ParkourProgressEntry> readProgress()
	{
		return File.Exists(saveFilePath) ? SerializationUtility.DeserializeValue<List<LopisParkourNS.ParkourProgressEntry>>(File.ReadAllBytes(saveFilePath), (DataFormat)1, null) : new List<ParkourProgressEntry>();
	}
	public static void saveProgress(List<ParkourProgressEntry> progress)
	{
		if (progress == null) return;
		if (progress.Count == 0) return;
		File.WriteAllBytes(saveFilePath, SerializationUtility.SerializeValue<List<ParkourProgressEntry>>(progress, (DataFormat)1, null));
	}
	// [HarmonyPatch(typeof(GameModeManager), nameof(GameModeManager.SetupUI))]
	// [HarmonyPostfix]
	public static void AddTheButton()
	{
		parkourMenu = MenuHandler.CreateMenu("PARKOUR", ParkourMenu, GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group").gameObject, 70, false);
		//"     lopi's PARKOUR		"
		// MenuHandler.CreateText(" ", GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group").gameObject, out TextMeshProUGUI Space ,5);
		// Space.fontSizeMin = 5;
		// Space.enableAutoSizing = true;
		// Space.autoSizeTextContainer = true;
		//<size=30>lopi's</size>\n<size=50>PARKOUR</size>\n<size=30>	</size>
		
        GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group/LOCAL")?.gameObject.SetActive(false);
		GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group/Online")?.gameObject.SetActive(false);


		pauseMenu = GameObject.Find("/Game/UI/UI_Game/Canvas/EscapeMenu/Main/Group").gameObject;

		TextMeshProUGUI text = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group/PARKOUR/Text").GetComponent<TextMeshProUGUI>();
		text.text = "<size=30>lopi's</size>\n<size=60>PARKOUR</size>\n<size=10>	</size>";
		text.paragraphSpacing = -30;
		
	}
	
	public static void PushTheUnboundText() //putched as postfix to AddTextWhenReady manually in the awake
	{
			var unboundText = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group/Unbound Text Object");
			if (unboundText != null) unboundText.transform.localPosition = new Vector3(0, 285, 0);
		 //0 285
	}


	[HarmonyPatch(typeof(Spawn), nameof(Spawn.OnInstantiate))]
	[HarmonyPostfix]
	public static void Spawn_Prefab_getter_postfix(GameObject instance)
	{
		if (GameModeManager.CurrentHandler?.GameMode is GM_Parkour) {

			instance.GetOrAddComponent<ParkourSpawnVisualizer>(searchChildren: false);
			// instance.GetOrAddComponent<SpawnVisualizer>(searchChildren: false);
			// GameObject val7 = new GameObject("Position Indicator");
			// val7.transform.SetParent(instance.transform);
			// Image _positionIndicator = val7.AddComponent<Image>();
			// ((Graphic)_positionIndicator).rectTransform.sizeDelta = Vector2.one * 10f;
		}
	}
	

	private void OnDestroy()
	{
		harmony.UnpatchSelf();
	}
	
}
public class ParkourProgressEntry
{
	[SerializeField]public string map;
	[SerializeField]public float time;
	[SerializeField]public MedalType medal;
	[SerializeField]public int rated = -1;


	public ParkourProgressEntry(string map, float time, MedalType medal)
	{
		this.map = map;
		this.time = time;
		this.medal = medal;
	}
	public ParkourProgressEntry()
	{
		map = "";
		time = -1;
		medal = MedalType.None;
	}
	public string getMedalSymbol()
	{
		switch (medal)
		{
			case MedalType.None:
				return " ";
			case MedalType.Bronze:
				return "<color=#950f>•</color>";
			case MedalType.Silver:
				return "<color=#99Cf>•</color>";
			case MedalType.Golden:
				return "<color=#DFDF00ff>•</color>";
			case MedalType.Shiny:
				return "<color=#FFFF00ff>•</color>";
			default:
				return " ";

		}
	}
};
public enum MedalType
{
	None,
	Bronze,
	Silver,
	Golden,
	Shiny
}