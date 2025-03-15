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
using UnityEngine.TextCore;
using UnityEngine.Events;
using InControl;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace LopisParkourNS;
[BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)] // necessary for most modding stuff here
[BepInPlugin(ModId, ModName, Version)]
[BepInProcess("Rounds.exe")]
public class LopisParkour : BaseUnityPlugin
{
	public const string ModId = "lopi.rounds.plugins.lopisparkour";
	public const string ModName = "Lopis Parkour";
	public const string Version = "1.0.3";
	public static string CompatibilityModName => ModName.Replace(" ", "");
	public Harmony harmony;
	public static GameObject parkourMenu;
	public static bool openParkourMenuOnCreation = false;
	public static GameObject pauseMenu;
	public static GameObject parkourWinMenu = null;
	public static List<GameObject> parkourMenuDirectories = new List<GameObject>();
	internal static readonly Material defaultMaterial = new Material(Shader.Find("Sprites/Default"));
	public static Dictionary<string, CustomMap> maps = new Dictionary<string, CustomMap>();
	public static List<string> mapsHierarchy = new List<string>();
	public static string saveFileName = "parkour.sav";
	public static string saveFilePath => Path.Combine(Paths.GameRootPath, saveFileName);
	public static string urlYouTubeChannel ="https://www.youtube.com/@RoundsParkour";
	public static string urlBugSubmissionForm ="https://docs.google.com/forms/d/e/1FAIpQLSexyvrUEe0yg-lnIRBPXw0YY89d43rB_VfoLCJtXWQ3ibXyFg/viewform?usp=dialog";
	public static string[] modWhitelist = new string[] {"com.willis.rounds.unbound","io.olavim.rounds.mapsextended","com.woukie.rounds.mapimageobjects","io.olavim.rounds.mapsextended.editor","com.woukie.rounds.mapimageobjectseditor","lopi.rounds.plugins.lopisparkour","pykess.rounds.plugins.performanceimprovements"};
	// public static Action<Gun, GameObject> OnBulletInit;

	public static LopisParkour? instance;
	public static CameraHandler? cameraHandlerInstance;
	public static Sprite? parkourTitleCard;
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

		try
		{
			var titleId = 0;
			for (; UnityEngine.Random.value < 0.5f && titleId < 9; titleId++);
			if (titleId == 9 && UnityEngine.Random.value < (2f/3f))
			{
				titleId++;
				if (UnityEngine.Random.value < 0.01f) titleId++;
			}
			parkourTitleCard = LoadTitleSpriteFromPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(instance.Info.Location), "title"+titleId+".png"));
		}
		catch(Exception ex)
		{
			Log("failed to load the title card");
			Log(ex);
		}
		Log("Loaded!");
		
	}

	public void Start()
	{
		UnboundLib.GameModes.GameModeManager.AddHandler<GM_Parkour>("Parkour", new ParkourHandler());
		AddTheButton();
		// On.MainMenuHandler.Awake += delegate(On.MainMenuHandler.orig_Awake orig, MainMenuHandler self)
		// {
		// 	AddTheButton();
		// };
		SceneManager.sceneLoaded +=  delegate(Scene scene, LoadSceneMode mode)
		{
			if (scene.name == "Main") AddTheButton();
		};
		TMP_FontAsset fontAss = TMP_Settings.defaultFontAsset;
		fontAss.tabSize = 6;
		GlobalLeaderboard.readUrls();
		GlobalLeaderboard.RequestUpdate();
		CheckForExtraMods();
	}
	public static void CheckForExtraMods()
	{
		var activeExtraMods = BepInEx.Bootstrap.Chainloader.PluginInfos.Where(x=>!modWhitelist.Contains(x.Key)).Select(x=>x.Value.Metadata.Name);
		if (!activeExtraMods.Any()) return;
		Unbound.BuildModal("Extra mods detected",
			"Please create a new profile to play Lopi's Parkour. Otherwise, your leaderboard records will be disqualified.\nExtra mods detected:"
			+ String.Join(", ",activeExtraMods.Take(20))
			+ (activeExtraMods.Count()>20 ? "..." : ".")
			+ "\nTo reqest a mod to be whitelisted use the bug report form");
	} 
    public static Sprite LoadTitleSpriteFromPath(string path)
    {
        Texture2D texture2D = new Texture2D(0, 0, TextureFormat.RGBA32, mipChain: false);
        texture2D.LoadImage(File.ReadAllBytes(path));
        return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
    }
	public static void ParkourMenu()
	{
		// if (LopisParkour.parkourMenu != null) DestroyImmediate(LopisParkour.parkourMenu.gameObject);

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
				if (f.StartsWith(Paths.GameRootPath)) name = "Your custom maps";
				else if (File.Exists(Path.Combine(f, "manifest.json")))
				{
					string manifest = File.ReadAllText(Path.Combine(f, "manifest.json"));
					var names = Regex.Matches(manifest, "\"name\": \"(.*?)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);
					if (names.Count > 0) name = names[0].Groups[1].Value;
				}
				if (name == "") name = Path.GetFileName(Path.GetDirectoryName(f).Remove(-1));
				name = name.Replace("_", " ");
				var realName = name;

				if (!f.StartsWith(Paths.GameRootPath))
				{
					List<int> MedalHits = Enumerable.Repeat(0, 5).ToList();
					ParkourProgressEntry  entry;
					LopisParkour.maps.AsEnumerable().Where(kvp => kvp.Key.Contains(f)).Do(pair => {
							entry = progress.FirstOrDefault(ppe => ppe.map == pathToId(pair.Value.id));
							if (entry == null) MedalHits[0]++;
							else MedalHits[(int)entry.medal]++;
							
						});
					if (MedalHits.Any(x=>x>0))
					{
						name += " (";
						name += String.Join(" ", MedalHits.Select((medalCount, i) => medalCount <= 0 ? "" : medalCount.ToString() + new ParkourProgressEntry(){medal=(MedalType)i}.getMedalSymbol()).Where(x=>x!=""));
						name += ")";
					}
				}

				GameObject directoryMenuButton;
				parkourMenuDirectories.Add(MenuHandler.CreateMenu(
						realName,
						()=>{
							var contents = GameObject.Find(@"Game/UI/UI_MainMenu/Canvas/ListSelector/"+realName+"/Group/Grid/Scroll View/Viewport/Content").transform;
							if (contents)
							foreach(Transform child in contents)
							{
								Destroy(child.gameObject);
							}

							LopisParkour.maps.AsEnumerable().Where(kvp => kvp.Key.Contains(f))
								.OrderBy( f => 
								{
									if(float.TryParse(Regex.Split(f.Value._name.Remove(0,4), "_")[0].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out float n))
									{
										return n;
									}
									else {
										return (float)f.Value._name.Remove(0,4)[0];
									}
								}, Comparer<float>.Create((x, y) => x > y ?  1 : x < y ?  -1 : 0)
								)
								.Do( pair =>
								{
									var buttonName = pair.Value._name.Remove(0,4).Replace("_", " ");
									var entry = progress.FirstOrDefault(ppe => ppe.map == pathToId(pair.Value.id));
									if (entry != null)
									{
										buttonName = entry.getMedalSymbol() + " " + buttonName + " <color=#fff1>" + TimeSpan.FromSeconds(entry.time).ToString("mm\\:ss\\.fff");
									}
									else buttonName = new ParkourProgressEntry().getMedalSymbol() + " " + buttonName;
									MenuHandler.CreateButton(
										buttonName,
										GameObject.Find(@"Game/UI/UI_MainMenu/Canvas/ListSelector/"+realName+"/Group/Grid/Scroll View/Viewport/Content"), () => {
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
					var text = directoryMenuButton.GetComponentInChildren<TextMeshProUGUI>();
					if (text != null) text.text = name;
			});
		}
		else maps.AsEnumerable().OrderBy( f => 
			{
				if(float.TryParse(Regex.Split(f.Value._name.Remove(0,4), "_")[0].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out float n))
				{
					return n;
				}
				else {
					return (float)f.Value._name.Remove(0,4)[0];
				}
			}, Comparer<float>.Create((x, y) => x > y ?  1 : x < y ?  -1 : 0)
			)
			.Do( pair => {
				var name = pair.Value._name.Remove(0,4).Replace("_", " ");
				var entry = progress.FirstOrDefault(ppe => ppe.map == pathToId(pair.Value.id));
				if (entry != null)
				{
					name = entry.getMedalSymbol() + " " + name + " <color=#fff1>" + TimeSpan.FromSeconds(entry.time).ToString("mm\\:ss\\.fff");
				}
				else name = new ParkourProgressEntry().getMedalSymbol() + " " + name;
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
		MenuHandler.CreateButton( "Get More Maps",
			parkourMenu, () => {
				Application.OpenURL("https://thunderstore.io/c/rounds/p/lopidav/Lopis_Parkour/dependants/?q=&ordering=top-rated&section=mods&included_categories=131");
				// MainMenuHandler.instance.Open();
				ExitParkourGamemode();
		});
		MenuHandler.CreateButton( "Map Creation Guide",
			parkourMenu, () => {
				Application.OpenURL("https://docs.google.com/document/d/1kYegslKXzL2sf-BxvEWKecUuQ_RAQn5joaaG4d6Ur8E/edit?usp=sharing");
				// MainMenuHandler.instance.Open();
				// ExitParkourGamemode();
		});

		if (!System.IO.Directory.Exists(Path.Combine(Paths.GameRootPath, "ParkourReplays"))) System.IO.Directory.CreateDirectory(Path.Combine(BepInEx.Paths.GameRootPath, "ParkourReplays"));
		// string[] prkr_files = Directory.GetFiles(Path.Combine(Paths.GameRootPath, "ParkourReplays"), "*.prkr", SearchOption.TopDirectoryOnly);
		// prkr_files = prkr_files.Where(s => s!="").ToArray();
		// if (prkr_files.Length > 0) {
		// 	var replayMenu = MenuHandler.CreateMenu("Your replays", ()=>{},parkourMenu, out var replayMenuButton);
		// 	prkr_files.Do(x=> {
		// 		MenuHandler.CreateButton(x, replayMenu, ()=>{
		// 				GhostReplayRecord record = SerializationUtility.DeserializeValue<GhostReplayRecord>(File.ReadAllBytes(x), DataFormat.Binary);
		// 				TimeHandler.instance.gameOverTime = 1f;
		// 				MainMenuHandler.instance.Close();
		// 				UnboundLib.GameModes.GameModeManager.SetGameMode("Parkour");
		// 				var gm = (GM_Parkour) GameModeManager.CurrentHandler.GameMode;
		// 				gm.selectedMap = MapLoader.LoadPath(record.mapId);
		// 				gm.selectedMap.id = record.mapId;
		// 				var player = gm.gameObject.AddComponent<GhostReplayPlayer>();
		// 				player.replayRecord = record.timedPositions;
		// 				player.mapId = record.mapId;
		// 				UnboundLib.GameModes.GameModeManager.CurrentHandler.StartGame();

		// 		});
		// 	});
		// }
		if (parkourWinMenu != null)
		{
			DestroyImmediate(parkourWinMenu.gameObject);
		}
		var theGoBackThing = new UnityAction(()=>{
			// ParkourMenu();
			// ExitParkourGamemode();
			});
		parkourWinMenu = MenuHandler.CreateMenu("UI_PARKOURWIN", ()=>{TimeHandler.instance.gameOverTime = 1f;}, parkourMenu, out GameObject winMenuButton, 60);
		parkourWinMenu.transform.Find("Group/Back").gameObject.GetComponent<Button>().onClick.AddListener(theGoBackThing);
		parkourWinMenu.GetComponentInChildren<GoBack>(true).goBackEvent.AddListener(theGoBackThing);

		
		var theGoBackToMainMenuThing = new UnityAction(()=>{
			// MainMenuHandler.instance.Open();
			ExitParkourGamemode();
			});
		parkourMenu.transform.Find("Group/Back").gameObject.GetComponent<Button>().onClick.AddListener(theGoBackToMainMenuThing);
		parkourMenu.GetComponentInChildren<GoBack>(true).goBackEvent.AddListener(theGoBackToMainMenuThing);

		winMenuButton.SetActive(false);
	}
	public static void ExitParkourGamemode()
	{
		// MainMenuHandler.instance.Open();
		// MainMenuHandler.instance.Close();
		if (UnboundLib.GameModes.GameModeManager.CurrentHandler.GameMode is not GM_Parkour parkour) return;
		parkour.selectedMap = null;
		GameModeManager.TriggerHook("GameEnd");
		SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
		// ParkourMenu();

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
	
	public static void replaceTheTitleCard()
	{
		if (parkourTitleCard == null) return;
		var titleCard = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group/Rounds_Logo2_White").gameObject;
		if (titleCard == null) return;
		var spriteRenderer = titleCard.GetComponent<SpriteRenderer>();
		var spriteMask = titleCard.GetComponent<SpriteMask>();
		if (spriteMask == null) return;
		spriteMask.sprite = parkourTitleCard;
		if (spriteRenderer == null) return;
		spriteRenderer.sprite = parkourTitleCard;

		List<LayoutElement> theThings = titleCard.GetComponents<LayoutElement>().ToList();
		theThings.ForEach(x=>UnityEngine.Object.DestroyImmediate(x));
		titleCard.transform.localScale = titleCard.transform.localScale * 5.5f;
		
		titleCard.transform.localPosition = titleCard.transform.localPosition + new Vector3(0,80f,0);
	}
	// [HarmonyPatch(typeof(GameModeManager), nameof(GameModeManager.SetupUI))]
	// [HarmonyPostfix]
	public static void AddTheButton()
	{
		parkourMenu = MenuHandler.CreateMenu("PARKOUR", ParkourMenu, GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group").gameObject, 70, false);
		
		MenuHandler.CreateText("Tournament", GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group").gameObject, out _, color: new Color(1,1,1,0.1f));
		replaceTheTitleCard();


		//instance.ExecuteAfterSeconds(5f,replaceTheTitleCard);

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
		text.text = "SINGLEPLAYER";
		// text.text = "<size=30>lopi's</size>\n<size=60>PARKOUR</size>\n<size=10>	</size>";
		// text.paragraphSpacing = -30;
	}
	
	public static void PushTheUnboundText() //putched as postfix to AddTextWhenReady manually in the awake
	{
			var unboundText = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group/Unbound Text Object");
			if (unboundText != null) unboundText.transform.localPosition = new Vector3(0, 285, 0);
		 //0 285
	}
	[HarmonyPatch(typeof(UnboundLib.Unbound), nameof(UnboundLib.Unbound.AddTextWhenReady))]
	[HarmonyPrefix]
	public static bool UnboundLib_Unbound_AddTextWhenReady_Prefix(ref IEnumerator __result) // Removes the unbound text
	{
		__result = UnboundLib_Unbound_AddTextWhenReady_Replacement();
		return false;
	}
	public static IEnumerator UnboundLib_Unbound_AddTextWhenReady_Replacement()
	{
		yield break;
	}


	[HarmonyPatch(typeof(Spawn), nameof(Spawn.OnInstantiate))]
	[HarmonyPostfix]
	public static void Spawn_Prefab_Getter_Postfix(GameObject instance)
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

	[HarmonyPatch(typeof(MapTransition), nameof(MapTransition.Move))]
	[HarmonyPrefix]
	public static bool MapTransition_Move_Prefix(GameObject target, Vector3 distance, Map targetMap, ref IEnumerator __result)
	{
		__result = MapTransition_Move_Replacement(target,distance,targetMap);
		return false;
	}
	public static IEnumerator MapTransition_Move_Replacement(GameObject? target, UnityEngine.Vector3 distance, Map targetMap)
	{
		if (target == null) yield break;
		MapTransition.isTransitioning = true;
		float nonRandomDelay = 0.13f;
		yield return new WaitForSecondsRealtime(nonRandomDelay);

		if (target == null) {MapTransition.isTransitioning = false;yield break;}
		Vector3 targetStartPos = target.transform.position;
		float t = MapTransition.instance.curve.keys[MapTransition.instance.curve.keys.Length - 1].time;
		float c = 0f;
		while (c < t)
		{
			if (target == null) {MapTransition.isTransitioning = false;yield break;}
			c += Time.unscaledDeltaTime;
			target.transform.position = targetStartPos + distance * MapTransition.instance.curve.Evaluate(c);
			yield return null;
		}

		if (target == null) {MapTransition.isTransitioning = false;yield break;}
		target.transform.position = targetStartPos + distance;
		MapTransition.instance.Toggle(target, enabled: true);
		yield return new WaitForSecondsRealtime(nonRandomDelay);
		MapTransition.isTransitioning = false;
		if ((bool)targetMap)
		{
			targetMap.hasEntered = true;
		}
	}
	
	[HarmonyPatch(typeof(PlayerAssigner), nameof(PlayerAssigner.CreatePlayer))]
	[HarmonyPrefix]
	public static bool PlayerAssigner_CreatePlayer_Prefix(ref IEnumerator __result)
	{
		if (GameModeManager.CurrentHandler?.GameMode is GM_Parkour parkour && (!MapManager.instance.currentMap.Map.hasEntered || MapTransition.isTransitioning)) {
			
			// parkour.selectedMap?.MapObjects?.Where(mod => mod is Spawn);
			__result = new EmptyEnumerator();
			return false;
		}
		return true;
	}
	[HarmonyPatch(typeof(PlayerAssigner), nameof(PlayerAssigner.LateUpdate))]
	[HarmonyPostfix]
	public static void PlayerAssigner_LateUpdate_Postfix(PlayerAssigner __instance)
	{
		if (!PlayerAssigner.instance.playersCanJoin || PlayerAssigner.instance.players.Count >= PlayerAssigner.instance.maxPlayers || DevConsole.isTyping)
		{
			return;
		}
		if (GameModeManager.CurrentHandler?.GameMode is GM_Parkour && MapManager.instance.currentMap.Map.hasEntered && !MapTransition.isTransitioning
			&& (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.LeftShift))
			)
		{
			bool flag = true;
			for (int i = 0; i < PlayerAssigner.instance.players.Count; i++)
			{
				if (PlayerAssigner.instance.players[i].playerActions.Device == null)
				{
					flag = false;
				}
			}
			if (flag)
			{
				PlayerAssigner.instance.StartCoroutine(PlayerAssigner.instance.CreatePlayer(null));
			}
		}
		foreach (var inputDevice in InputManager.ActiveDevices)
		{
			if (__instance.ThereIsNoPlayerUsingDevice(inputDevice) && inputDevice.AnyButtonWasPressed)
				PlayerAssigner.instance.StartCoroutine(PlayerAssigner.instance.CreatePlayer(inputDevice));
		}
	}
	class EmptyEnumerator : IEnumerator
	{
        public object? Current => null;

        public void Reset()
        {
            throw new NotImplementedException();
        }

        bool IEnumerator.MoveNext()
        {
            return false;
        }
    }
	
	// [HarmonyPatch(typeof(Gun), nameof(Gun.BulletInit))]
	// [HarmonyPostfix]
	// public static void Gun_BulletInit_Postfix(Gun __instance, GameObject bullet)
	// {
	// 	Log("shooting detected");
	// 	if (OnBulletInit != null) OnBulletInit(__instance, bullet);
	// }
	[HarmonyPatch(typeof(MainMenuLinks), nameof(MainMenuLinks.Links), MethodType.Getter)]
	[HarmonyPrefix]
	public static void MainMenuLinks_Links_Getter_Prefix(ref bool __state)
	{
		__state = MainMenuLinks.links == null;
	}
	[HarmonyPatch(typeof(MainMenuLinks), nameof(MainMenuLinks.Links), MethodType.Getter)]
	[HarmonyPostfix]
	public static void MainMenuLinks_Links_Getter_Postfix(ref bool __state, ref GameObject __result)
	{
		if (!__state) return;
		var newLink = UnityEngine.Object.Instantiate<GameObject>(MainMenuLinks.links.transform.GetChild(0).gameObject, MainMenuLinks.links.transform);
		if (newLink == null) return;
		MainMenuLinks.links.transform.GetChild(1)?.gameObject.SetActive(false);
		newLink.transform.SetAsFirstSibling();
		newLink.GetOrAddComponent<Link>()._Links = urlYouTubeChannel;
		newLink.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "YOUTUBE CHANNEL";
		
		var ytSprite = LoadTitleSpriteFromPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(instance.Info.Location), "YouTube.png"));
		var image = newLink.GetComponentInChildren<Image>();
		image.overrideSprite = ytSprite;
		image.color = new Color(0.95f,0.95f,0.95f,1);

		newLink = UnityEngine.Object.Instantiate<GameObject>(MainMenuLinks.links.transform.GetChild(0).gameObject, MainMenuLinks.links.transform);
		if (newLink == null) return;
		// MainMenuLinks.links.transform.GetChild(1)?.gameObject.SetActive(false);
		newLink.transform.SetAsFirstSibling();
		newLink.GetOrAddComponent<Link>()._Links = urlBugSubmissionForm;
		newLink.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "REPORT A BUG";

		ytSprite = LoadTitleSpriteFromPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(instance.Info.Location), "Bug.png"));
		image = newLink.GetComponentInChildren<Image>();
		image.overrideSprite = ytSprite;
		image.color = new Color(0.95f,0.95f,0.95f,1);
		
		MainMenuLinks.links.transform.position += new Vector3(0f, 1f, 0f);
	}
	[HarmonyPatch(typeof(CameraHandler), nameof(CameraHandler.Awake))]
	[HarmonyPostfix]
	public static void CameraHandler_Awake_Postfix(CameraHandler __instance)
	{
		LopisParkour.cameraHandlerInstance = __instance;
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
				return "<color=#aaa2>•</color>";
				//return " ";
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