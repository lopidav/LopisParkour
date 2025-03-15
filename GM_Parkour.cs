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
using UnityEngine.UI;
using TMPro;
using MapsExt.MapObjects;
using Steamworks;
using UnityEngine.TextCore;
using Sirenix.Serialization;
using SoundImplementation;
using Sonigon.Internal;
using MapsExt.Visualizers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LopisParkourNS;
public class GM_Parkour : MonoBehaviour
{
	public static GM_Parkour instance;
	public CustomMap _selectedMap;
	public bool isTransitioning = false;
	public static GhostReplayRecorder ghostRecorder = null;
	public static GhostReplayPlayer ghostPlayer = null;
	public bool isEnding = false;
	public CustomMap selectedMap {
		get { return _selectedMap;}
		set
		{
			if (_selectedMap == value) return;
			_selectedMap = value;
			if (GameManager.instance.isPlaying) reloadMap();
		}}
	public static float timePassed = 0;
	public static bool timerStop = false;
	public static GameObject pauseNextMapButton;
	public static GameObject pauseGhostEnabledButton;
	public static bool ghostEnabled = true;
	public static TextMeshProUGUI LeaderboardText;
	public static TextMeshProUGUI pauseLeaderboardText;
	public static TextMeshProUGUI pauseTimeText;
	public static TextMeshProUGUI pauseCurrentMapText;
	
	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		LopisParkour.Log("Start");
		base.transform.root.GetComponent<SetOfflineMode>().SetOffline();
		instance.isEnding = false;

		pauseGhostEnabledButton = MenuHandler.CreateButton("GhostToggle", LopisParkour.pauseMenu, pauseGhostToggle, forceUpper: false);
		pauseGhostEnabledButton.transform.SetAsFirstSibling(); 
		
		var ghostText = pauseGhostEnabledButton?.GetComponentInChildren<TextMeshProUGUI>();
		if (ghostText != null) ghostText.text = "Ghost: " + (ghostEnabled ? "On" : "Off");
		if (ghostPlayer != null) ghostPlayer.enabled = ghostEnabled;

		MenuHandler.CreateButton("Reload map [R]", LopisParkour.pauseMenu, reloadMap).transform.SetAsFirstSibling();
		pauseNextMapButton = MenuHandler.CreateButton("Next map", LopisParkour.pauseMenu, pauseNextMapAction);
		pauseNextMapButton.transform.SetAsFirstSibling(); 
		MenuHandler.CreateText("", LopisParkour.pauseMenu, out pauseTimeText).transform.SetAsFirstSibling();
		MenuHandler.CreateText("", LopisParkour.pauseMenu, out pauseCurrentMapText).transform.SetAsFirstSibling();

		
		LoadSelectedMap();
		PlayerAssigner.instance.SetPlayersCanJoin(canJoin: true);
		TimeHandler.instance.StartGame();
		PlayerManager playerManager = PlayerManager.instance;
		playerManager.PlayerJoinedAction = (Action<Player>)Delegate.Combine(playerManager.PlayerJoinedAction, new Action<Player>(PlayerWasAdded));
		PlayerManager.instance.AddPlayerDiedAction(PlayerDied);
		GameManager.instance.isPlaying = true;
		GameManager.instance.battleOngoing = true;
		// PlayerManager.instance.InvokeMethod("SetPlayersVisible", true);
		CameraHandler camera = MainCam.instance.cam.GetComponentInParent<CameraZoomHandler>().gameObject.GetComponent<CameraHandler>();
		camera._playersHaveSpawned = true;

		ghostRecorder = this.gameObject.GetOrAddComponent<GhostReplayRecorder>();
		
		GlobalLeaderboard.onLeaderboardFinishedDownload += WinMenuShowLeaderboard;
		GlobalLeaderboard.onLeaderboardStartedDownload += WinMenuShowLeaderboard;
		GlobalLeaderboard.onLeaderboardSubmittionStart += WinMenuShowLeaderboard;
		GlobalLeaderboard.onLeaderboardFail += FailedToLoadLeaderboard;

		// MapsExtended.instance CameraHandler.Mode = CameraHandler.CameraMode.FollowPlayer;
	}
	public static void FailedToLoadLeaderboard()
	{
		if (LeaderboardText != null) LeaderboardText.text = "Leaderboard\nfailed to load";
	}
	public static IEnumerator EndCoroutine()
	{
		if (instance.isEnding) yield return null;
		instance.isEnding = true;
		GlobalLeaderboard.onLeaderboardFinishedDownload -= WinMenuShowLeaderboard;
		GlobalLeaderboard.onLeaderboardStartedDownload -= WinMenuShowLeaderboard;
		GlobalLeaderboard.onLeaderboardSubmittionStart -= WinMenuShowLeaderboard;
		GlobalLeaderboard.onLeaderboardFail -= FailedToLoadLeaderboard;
		
		// ghostRecorder;
		CameraHandler camera = MainCam.instance.cam.GetComponentInParent<CameraZoomHandler>().gameObject.GetComponent<CameraHandler>();
		camera._playersHaveSpawned = false;
		GameManager.instance.isPlaying = false;
		GameManager.instance.battleOngoing = false;
		// PlayerManager playerManager = PlayerManager.instance;
		// playerManager.PlayerJoinedAction = (Action<Player>)Delegate.Combine(playerManager.PlayerJoinedAction, new Action<Player>(PlayerWasAdded));
		// PlayerManager.instance.PlayerDiedAction -= instance.PlayerDied;
		AsyncOperation op = SceneManager.UnloadSceneAsync($"MapsExtended:{instance.selectedMap._id}");
		MapManager.instance.currentMap = null;



		CameraHandler.Mode = CameraHandler.CameraMode.FollowPlayer;
		while (!op.isDone)
		{
			yield return null;
		}
		PlayerAssigner.instance.SetPlayersCanJoin(canJoin: false);

		GameObject.Find("Game/UI/UI_MainMenu").gameObject.SetActive(value: true);
		GameObject.Find("Game").GetComponent<SetOfflineMode>().SetOnline();
		LopisParkour.Log("End");
		
		// PlayerManager.instance.InvokeMethod("SetPlayersVisible", true);

		
	}
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R) && !DevConsole.isTyping)
		{
			TimeHandler.instance.gameOverTime = 1f;
			reloadMap();
			if(MainMenuHandler.instance.isOpen)
			{
				MainMenuHandler.instance.Close();
			}
		}

		if (PlayerManager.instance.players.Count != 0)
		{
			if (!timerStop && !EscapeMenuHandler.isEscMenu)
        		timePassed += Time.deltaTime;
		}
		else
		{
        	timePassed = 0;
		}

		pauseMenuUpdate();
	}
	public void OnDestroy()
	{
		
		GlobalLeaderboard.onLeaderboardFinishedDownload -= WinMenuShowLeaderboard;
		GlobalLeaderboard.onLeaderboardStartedDownload -= WinMenuShowLeaderboard;
		GlobalLeaderboard.onLeaderboardSubmittionStart -= WinMenuShowLeaderboard;
	}
	public void reloadMap()
	{
		if (isTransitioning) return;
		MapTransition.isTransitioning = true;
		foreach(var card in CardChoice.instance.spawnedCards)
		{
			UnityEngine.Object.Destroy(card);
		}
		CardChoice.instance.spawnedCards.Clear();
		
		LoadSelectedMap();
		PlayerManager.instance.RemovePlayers();
		// PlayerManager.instance.ResetCharacters();
		timerStop = false;
		TimeHandler.instance.gameOverTime = 1f;
		ghostRecorder?.Reset();
		ghostPlayer?.RepaintTheGhost();
		MapTransition.isTransitioning = true;
	}
	public void MapWon()
	{
		timerStop = true;
		
		GlobalLeaderboard.RequestUpdate();
		// if (GlobalLeaderboard.isUpdating) LeaderboardText.text = "Leaderboard\nloading";
		// GlobalLeaderboard.onLeaderboardDownload += WinMenuShowLeaderboard;
		if (!GM_Parkour.instance._selectedMap.id.StartsWith(BepInEx.Paths.GameRootPath) )
		{
			GlobalLeaderboard.AddRecord(LopisParkour.pathToId(selectedMap.id), timePassed);
			WinMenuShowLeaderboard();
			CheckPBGhost();
		}

		StartCoroutine(MapWonCorutine());
	}
	public void CheckPBGhost()
	{
		ParkourProgressEntry? prevEntry = LopisParkour.GetRecord(GM_Parkour.instance.selectedMap.id) ?? new ParkourProgressEntry();
		if (timePassed == 0 || (prevEntry.time >0 && prevEntry.time <= timePassed)) return;
		ghostRecorder.SaveReplay(LopisParkour.pathToId(GM_Parkour.instance._selectedMap.id).Replace('\\','_')+"PB");
	}
	public void pauseMenuUpdate()
	{
		if (pauseTimeText != null) pauseTimeText.text = timePassed == 0 ? "Jump to spawn in" : TimeSpan.FromSeconds(timePassed).ToString("mm\\:ss\\.fff");
	}
	public void pauseNextMapButtonUpdate()
	{
		var targetMap = getNextMap();
		var text = pauseNextMapButton?.GetComponentInChildren<TextMeshProUGUI>();
		if (text != null) text.text = targetMap == null ? "No next map" : $"Next map ({ mapToName(targetMap)})";
		if (pauseCurrentMapText != null) pauseCurrentMapText.text = "Current map: " + mapToName(_selectedMap);
	}
	public static string mapToName(CustomMap map)
	{
		return map._name.Remove(0,4).Replace("_", " ");
	}
	public static CustomMap? getNextMap()
	{
		var directory = LopisParkour.mapsHierarchy.FirstOrDefault(directory => GM_Parkour.instance._selectedMap.id.Contains(directory));
		LopisParkour.Log(directory);
		LopisParkour.Log(GM_Parkour.instance._selectedMap.id);
		var maps = LopisParkour.maps.AsEnumerable().Where(map => directory == null || map.Key.Contains(directory)).OrderBy( f => 
		{
			if(int.TryParse(Regex.Split(f.Value._name.Remove(0,4), "_")[0], out int n))
			{
				return n;
			}
			else {
				return (int)f.Value._name.Remove(0,4)[0];
			}
		}, Comparer<int>.Create((x, y) => x > y ?  1 : x < y ?  -1 : 0)
		).ToList();
		int ind = maps.FindIndex(p => p.Value == GM_Parkour.instance._selectedMap);
		if (ind != -1 && ind < maps.Count -1)
		{
			return maps[ind+1].Value;
		}
		return null;
	}
	public void pauseGhostToggle()
	{
		ghostEnabled = !ghostEnabled;
		var text = pauseGhostEnabledButton?.GetComponentInChildren<TextMeshProUGUI>();
		if (text != null) text.text = "Ghost: " + (ghostEnabled ? "On" : "Off");
		ghostPlayer.enabled = ghostEnabled;
	}
	public void pauseNextMapAction()
	{
		var targetMap = getNextMap();
		if (targetMap == null) return;
		
		TimeHandler.instance.gameOverTime = 1f;
		MainMenuHandler.instance.Close();
		GM_Parkour.instance.selectedMap = targetMap;
	}
	public IEnumerator MapWonCorutine()
	{
		isTransitioning = true;
		TimeHandler.instance.DoSlowDown();
		// UIHandler.instance.ShowJoinGameText(TimeSpan.FromSeconds(timePassed).ToString("hh\\:mm\\:ss\\.fff") + "improve?", PlayerManager.instance.GetColorFromTeam(PlayerManager.instance.players.FirstOrDefault().teamID).winText);
		
		yield return new WaitForSecondsRealtime(0.25f);
		isTransitioning = false;
		OpenParkourWinMenu();

		MapTransition.instance.Exit(MapManager.instance.currentMap.Map);
		// MapTransition.instance.StartCoroutine(MapTransition.instance.ClearObjectsAfterSeconds(0.05f));
		// SceneManager.sceneLoaded += OnLevelFinishedLoading; 
		PlayerManager.instance.RemovePlayers();
		PlayerManager.instance.ResetCharacters();
		
		
	}
	public static MedalType WhatMedalIsIt(float time)
	{
		MapObjectData finish = GM_Parkour.instance.selectedMap.MapObjects.FirstOrDefault(mod => mod.GetProperty<GoldMedalProperty>() != null);
		float bronzeTime = 0f;
		float silverTime = 0f;
		float goldTime = 0f;
		float shinyTime = 0f;
		if (finish != null)
		{
			bronzeTime = finish.GetProperty<BronzeMedalProperty>()?.Value ?? 0;
			silverTime = finish.GetProperty<SilverMedalProperty>()?.Value ?? 0;
			goldTime = finish.GetProperty<GoldMedalProperty>()?.Value ?? 0;
			shinyTime = finish.GetProperty<ShinyMedalProperty>()?.Value ?? 0;
		}
		return time >= bronzeTime ? MedalType.None
			: time >= silverTime ? MedalType.Bronze
			: time >= goldTime ? MedalType.Silver
			: time >= shinyTime ? MedalType.Golden
			: MedalType.Shiny;
		
	}
	public static float TimeFromMedal(MedalType medal)
	{
		MapObjectData finish = GM_Parkour.instance.selectedMap.MapObjects.FirstOrDefault(mod => mod.GetProperty<GoldMedalProperty>() != null);
		float bronzeTime = 0f;
		float silverTime = 0f;
		float goldTime = 0f;
		float shinyTime = 0f;
		if (finish != null)
		{
			bronzeTime = finish.GetProperty<BronzeMedalProperty>()?.Value ?? 0;
			silverTime = finish.GetProperty<SilverMedalProperty>()?.Value ?? 0;
			goldTime = finish.GetProperty<GoldMedalProperty>()?.Value ?? 0;
			shinyTime = finish.GetProperty<ShinyMedalProperty>()?.Value ?? 0;
		}
		switch (medal)
		{
			case MedalType.None:
			return Mathf.Infinity;
			case MedalType.Bronze:
			return bronzeTime;
			case MedalType.Silver:
			return silverTime;
			case MedalType.Golden:
			return goldTime;
			case MedalType.Shiny:
			return shinyTime;
			default:
			return 0;
		}
	}
	public static string RichTextMedalColor(MedalType medal, string text)
	{
		switch (medal)
		{
			case MedalType.None:
				return text;
			case MedalType.Bronze:
				return "<color=#950f>"+text+"</color>";
			case MedalType.Silver:
				return "<color=#99Cf>"+text+"</color>";
			case MedalType.Golden:
				return "<color=#DFDF00ff>"+text+"</color>";
			case MedalType.Shiny:
				return "<color=#FFFF00ff>"+text+"</color>";
			default:
				return text;

		}
	}
	public static void OpenParkourWinMenu()
	{
		var mainMenu = GameObject.Find("/Game/UI/UI_MainMenu/Canvas/ListSelector/Main/Group");
		mainMenu.transform.position = new Vector3(0,0,0); 
		
		if (LopisParkour.parkourWinMenu == null)
		{
			return;
		}
		
		MainMenuHandler.instance.Open();
		
		LopisParkour.parkourWinMenu.GetComponent<ListMenuPage>().Open();
		LopisParkour.parkourWinMenu.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 1f;
		var contents = LopisParkour.parkourWinMenu.transform.Find("Group/Grid/Scroll View/Viewport/Content");
		if (contents) foreach(Transform child in contents)
		{
			Destroy(child.gameObject);
		}

		ParkourProgressEntry? prevEntry = LopisParkour.GetRecord(GM_Parkour.instance.selectedMap.id) ?? new ParkourProgressEntry();
		// float prevTime = prevEntry == null ? -1 : prevEntry.time;
		// MedalType prevMedal = prevEntry == null ? MedalType.none : prevEntry.medal;
		
		MedalType newMedal = WhatMedalIsIt(timePassed);

		TextMeshProUGUI pbText;
		TextMeshProUGUI timeDiff;
		TextMeshProUGUI timeText;
		
		MenuHandler.CreateText(mapToName(GM_Parkour.instance.selectedMap), LopisParkour.parkourWinMenu, out pbText, 30);
		if ((prevEntry.time == -1 || prevEntry.time > timePassed) && timePassed != 0)
		{
			if (prevEntry.medal < newMedal)
					MenuHandler.CreateText(RichTextMedalColor(newMedal, newMedal.ToString() + " Medal!"), LopisParkour.parkourWinMenu, out pbText, 100);
			else
				MenuHandler.CreateText(RichTextMedalColor(newMedal, "Personal Best!"), LopisParkour.parkourWinMenu, out pbText, 100);
			SoundPlayerStatic.Instance.PlayMatchFound();
			LopisParkour.AddRecord(GM_Parkour.instance.selectedMap.id, timePassed, newMedal);
		}
		else
		{
			MenuHandler.CreateText(" ", LopisParkour.parkourWinMenu, out pbText, 100);
		}

		if (timePassed != 0)
		{
			MenuHandler.CreateText(RichTextMedalColor(newMedal, TimeSpan.FromSeconds(timePassed).ToString("mm\\:ss\\.fff")), LopisParkour.parkourWinMenu, out timeText, 80);
		}
		else
		{
			MenuHandler.CreateText(TimeSpan.FromSeconds(timePassed).ToString("mm\\:ss\\.fff") + "(sorry, timer broke, it's a bug)", LopisParkour.parkourWinMenu, out timeText, 80);
		}

		if (prevEntry.time == -1)
		{
			MenuHandler.CreateText(" ", LopisParkour.parkourWinMenu, out timeDiff, 40);
		}
		else
		{
			MenuHandler.CreateText(
				((prevEntry.time == -1 || prevEntry.time > timePassed) && timePassed != 0 ? "previously: " : "pb: ")
				+ TimeSpan.FromSeconds(prevEntry.time).ToString("mm\\:ss\\.fff")
				+" (" +(timePassed < prevEntry.time ? "-" : "+")
				+ TimeSpan.FromSeconds(timePassed - prevEntry.time).ToString("mm\\:ss\\.fff") + ")"
				, LopisParkour.parkourWinMenu, out timeDiff, 40, false);
		}

		float bestTime = prevEntry.time == -1 ? timePassed : Mathf.Min(timePassed, prevEntry.time);

        MedalType nextMedal = (MedalType)((int)(newMedal + 1) % Enum.GetValues(typeof(MedalType)).Length);
		float nextMedalTime = TimeFromMedal(nextMedal);

		if (nextMedal != MedalType.None && nextMedal != MedalType.Shiny) MenuHandler.CreateText(
				"next medal: "
				+ nextMedal.ToString() + " "
				+ TimeSpan.FromSeconds(nextMedalTime).ToString("mm\\:ss\\.fff")
				+" (" +(timePassed < nextMedalTime ? "-" : "+")
				+ TimeSpan.FromSeconds(timePassed - nextMedalTime).ToString("mm\\:ss\\.fff") + ")"
				, LopisParkour.parkourWinMenu, out _, 40, false);
		else  if(nextMedal == MedalType.None) 
		{
			MenuHandler.CreateText("No medals left to get", LopisParkour.parkourWinMenu, out _, 40, false);
		}
		else MenuHandler.CreateText(" ", LopisParkour.parkourWinMenu, out _, 40, false);

		MenuHandler.CreateButton("Improve [R]", LopisParkour.parkourWinMenu, ()=>{
			TimeHandler.instance.gameOverTime = 1f;
			MainMenuHandler.instance.Close();
			var gm = (GM_Parkour) GameModeManager.CurrentHandler.GameMode;
			gm.reloadMap();
		});

		CustomMap? targetMap = getNextMap();

		// GameObject? rateMenu = null;
		// rateMenu = MenuHandler.CreateMenu("RateTheMap", () => {
		// 	MenuHandler.CreateText("Please rate the map you just played:", rateMenu, out _ );
		// 	MenuHandler.CreateText(" ", rateMenu, out _ );
		// 	MenuHandler.CreateButton(("Bad and i hate it"), rateMenu, ()=>{
		// 		LopisParkour.RateMap(GM_Parkour.instance.selectedMap.id, 1);
		// 		TimeHandler.instance.gameOverTime = 1f;
		// 		MainMenuHandler.instance.Close();
		// 		GM_Parkour.instance.selectedMap = targetMap;
		// 	});
		// 	MenuHandler.CreateButton(("It's aight"), rateMenu, ()=>{
		// 		LopisParkour.RateMap(GM_Parkour.instance.selectedMap.id, 2);
		// 		TimeHandler.instance.gameOverTime = 1f;
		// 		MainMenuHandler.instance.Close();
		// 		GM_Parkour.instance.selectedMap = targetMap;
		// 	});
		// 	MenuHandler.CreateButton(("Good one"), rateMenu, ()=>{
		// 		LopisParkour.RateMap(GM_Parkour.instance.selectedMap.id, 3);
		// 		TimeHandler.instance.gameOverTime = 1f;
		// 		MainMenuHandler.instance.Close();
		// 		GM_Parkour.instance.selectedMap = targetMap;
		// 	});

		// }, LopisParkour.parkourWinMenu, out var rateMenuButton);


		// GameObject? saveReplayButton = null;
		// saveReplayButton = MenuHandler.CreateButton("Save replay", LopisParkour.parkourWinMenu, ()=>{
		// 	recorder.SaveReplay();
		// 	saveReplayButton.GetComponentInChildren<TextMeshProUGUI>().text = "saved";
		// });

		// rateMenuButton.GetComponentInChildren<TextMeshProUGUI>().text = targetMap == null ? "no more maps in this pack" : "next map (" + mapToName(targetMap) + ")";






		var nextMapButton = MenuHandler.CreateButton(targetMap == null ? "Get more maps" : "Next map: " + mapToName(targetMap), LopisParkour.parkourWinMenu, ()=>{
		// 		TimeHandler.instance.gameOverTime = 1f;
		// 		MainMenuHandler.instance.Close();
		// 		GM_Parkour.instance.selectedMap = targetMap;
			TimeHandler.instance.gameOverTime = 1f;
			MainMenuHandler.instance.Close();
			if (targetMap != null) GM_Parkour.instance.selectedMap = targetMap;
			else {
					Application.OpenURL("https://thunderstore.io/c/rounds/p/lopidav/Lopis_Parkour/dependants/?q=&ordering=top-rated&section=mods&included_categories=131");
					MainMenuHandler.instance.Open();
					LopisParkour.ExitParkourGamemode();
				}
		});
		// nextMapButton.SetActive(targetMap != null);


		// WinMenuShowLeaderboard();

		// GM_Parkour.instance.StartCoroutine(MapManager.instance.UnloadAfterSeconds(MapManager.instance.currentMap.Scene));
		// MapManager.instance.UnloadScene(MapManager.instance.currentMap.Scene);
		
		// MainMenuHandler.instance.Open();
		// loadMapThroughExtended(pair.Value);

	}
	public static void WinMenuShowLeaderboard()
	{
		if (LeaderboardText == null)
		{
			LeaderboardText = MenuHandler.CreateTextAt("", new Vector2());
			LeaderboardText.transform.SetParent(LopisParkour.parkourWinMenu.transform.Find("Group"));
			LeaderboardText.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
			LeaderboardText.rectTransform.localScale = new Vector3(1f,1f,1f);
			LeaderboardText.transform.position = new Vector3(-30, 15,0);
			LeaderboardText.fontSize = 20;
			LeaderboardText.color = new Color(255,255,255,200);
			LeaderboardText.alignment = TextAlignmentOptions.TopLeft;
		}
		
		if (pauseLeaderboardText == null)
		{
			pauseLeaderboardText = MenuHandler.CreateTextAt("", new Vector2());
			pauseLeaderboardText.transform.SetParent(LopisParkour.pauseMenu.transform);
			pauseLeaderboardText.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
			pauseLeaderboardText.rectTransform.localScale = new Vector3(1f,1f,1f);
			pauseLeaderboardText.transform.position = new Vector3(-30, 15,0);
			pauseLeaderboardText.fontSize = 20;
			pauseLeaderboardText.color = new Color(255,255,255,200);
			pauseLeaderboardText.alignment = TextAlignmentOptions.TopLeft;
		}
		if (LeaderboardText == null || pauseLeaderboardText == null) {LopisParkour.Log("failed to create leaderboard object");return;}
		LopisParkour.Log(GM_Parkour.instance._selectedMap.id);
		if (GM_Parkour.instance._selectedMap.id.StartsWith(BepInEx.Paths.GameRootPath))
		{
			LeaderboardText.text = "";
			return;
		}
		var latestRecord = GlobalLeaderboard.GetMyRecordForMap(LopisParkour.pathToId(GM_Parkour.instance._selectedMap.id));
		MapObjectData finish = GM_Parkour.instance.selectedMap.MapObjects.FirstOrDefault(mod => mod.GetProperty<GoldMedalProperty>() != null);
		float bronzeTime = 0f;
		float silverTime = 0f;
		float goldTime = 0f;
		float shinyTime = 0f;
		if (finish != null)
		{
			bronzeTime = finish.GetProperty<BronzeMedalProperty>()?.Value ?? 0;
			silverTime = finish.GetProperty<SilverMedalProperty>()?.Value ?? 0;
			goldTime = finish.GetProperty<GoldMedalProperty>()?.Value ?? 0;
			shinyTime = finish.GetProperty<ShinyMedalProperty>()?.Value ?? 0;
		}
		if (goldTime + shinyTime <= 0 || latestRecord == null || latestRecord.time > goldTime)
		{
			LeaderboardText.text = "Get gold to unlock Leaderboards for this map";
			pauseLeaderboardText.text = "Get gold to unlock Leaderboards for this map";
			return;
		}
		if (GlobalLeaderboard.isUpdating)
		{
			LeaderboardText.text = "Leaderboard (loading...)\n";
			pauseLeaderboardText.text = "Leaderboard (loading...)\n";
		}
		else
		{
			LeaderboardText.text = "Leaderboard\n";
			pauseLeaderboardText.text = "Leaderboard\n";
		}

		// LeaderboardText.rectTransform.localScale = new Vector3(1f,1f,1f);
		var leaderboard = GlobalLeaderboard.GetLeaderboardForMap(LopisParkour.pathToId(GM_Parkour.instance.selectedMap.id));
		// LopisParkour.Log("Leaderboard:");
		// LopisParkour.Log(String.Join("\n", leaderboard.Select(x=>x.ToString())));
		var myId = SteamUser.GetSteamID().ToString();
		var recordString = "";
		var i = 0;
		var myPos = leaderboard.FindIndex(x=>x.id == myId);
		var cutoff = 15;
		var halo = 2;
		var tempString = "";
		leaderboard.ForEach(x=>{
			i++;
			if (i <= cutoff || System.Math.Abs(myPos - i) <= halo)
			{
				if (i%2 != 0) recordString += "<mark=#FFFFFF05>";

				tempString = i + " " + Regex.Replace(x.name,"<.+?>","",RegexOptions.Multiline);
				if (tempString.Length > 30) tempString = tempString.Substring(0, 30) + "...";
				tempString += "\t" + TimeSpan.FromSeconds(x.time).ToString("mm\\:ss\\.fff");

				recordString += x.id != myId ? tempString : "<color=orange>" + tempString +"</color>";
				recordString += "\n";
				if (i%2 != 0) recordString += "</mark>";
			}
			else if (i == cutoff+1)
			{
				recordString += "...";
				recordString += "\n";
			}
			else if (i == myPos + halo + 1)
			{
				recordString += "...";
				recordString += "\n";
			}
		});
		LeaderboardText.text += recordString;
		pauseLeaderboardText.text += recordString;
		// GlobalLeaderboard.onLeaderboardDownload -= WinMenuShowLeaderboard;
	}
	public void LoadSelectedMap()
	{
		if (selectedMap == null) return;
		
		// MapManager.instance.isTestingMap = true;
		// MapManager.instance.currentMap = new MapWrapper(UnityEngine.Object.FindObjectOfType<Map>(), UnityEngine.Object.FindObjectOfType<Map>().gameObject.scene);
		// MapManager.instance.SetCurrentCustomMap(this.selectedMap);
		// ArtHandler.instance.NextArt();
		// LopisParkour.instance.loadMapThroughExtended(selectedMap);
		SceneManager.sceneLoaded += OnLevelFinishedLoading; 
		// MapManager.instance.view.RPC("RPCA_SetCallInNextMap", RpcTarget.All, true);
		MapManager.instance.RPCA_SetCallInNextMap(true);
		MapManager.instance.view.RPC("RPCA_LoadLevel", RpcTarget.All, $"MapsExtended:{selectedMap._id}");
		PlayerAssigner.instance.SetPlayersCanJoin(canJoin: true);
		timerStop = false;
		if (EscapeMenuHandler.isEscMenu)
		{
			LopisParkour.pauseMenu.GetComponentInParent<EscapeMenuHandler>().ToggleEsc();
		}
		pauseNextMapButtonUpdate();
		WinMenuShowLeaderboard();

		var PBGhostPath = Path.Combine(Paths.GameRootPath, "ParkourReplays", LopisParkour.pathToId(_selectedMap.id).Replace('\\','_')+"PB.prkr");
		if (File.Exists(PBGhostPath))
		{
			GhostReplayRecord record = SerializationUtility.DeserializeValue<GhostReplayRecord>(File.ReadAllBytes(PBGhostPath), DataFormat.Binary);
			
			ghostPlayer = this.gameObject.GetOrAddComponent<GhostReplayPlayer>();
			ghostPlayer.Reset();
			ghostPlayer.replayRecord = record;
			ghostPlayer.enabled = ghostEnabled;
		}
		// MapTransition.instance.Enter(MapManager.instance.currentMap.Map);
		// NetworkingManager.RPC(typeof(TRTMapManager), nameof(RPCA_SetCurrentLevel), TRTMapManager.CurrentLevel);
	}
	public void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		PlayerManager.instance.RemovePlayers();
		PlayerManager.instance.ResetCharacters();
		if (MapManager.instance.currentMap != null && MapManager.instance.currentMap.Scene != null)
		{
			try
			{
				SceneManager.UnloadSceneAsync(MapManager.instance.currentMap.Scene);
			}
			catch (Exception ex)
			{
				LopisParkour.Log(ex);
			}
		}
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}
	private void PlayerWasAdded(Player player)
	{
		LopisParkour.Log("PlayerWasAdded");
		if (MapTransition.isTransitioning)
		{
			PlayerManager.instance.RemovePlayers();
			PlayerManager.instance.ResetCharacters();
			return;
		}
		PlayerManager.instance.SetPlayersSimulated(simulated: true);
		player.data.GetComponent<PlayerCollision>().IgnoreWallForFrames(2);
		player.transform.position = MapManager.instance.currentMap.Map.GetRandomSpawnPos();
		// PlayerManager.instance.SetPlayersSimulated(simulated: true);
		PlayerManager.instance.SetPlayersPlaying(playing: true);
		PlayerAssigner.instance.SetPlayersCanJoin(canJoin: false);
		player.GetFaceOffline();
	}

	private void PlayerDied(Player player, int unused)
	{
		LopisParkour.Log("PlayerDied");
		if (MapTransition.isTransitioning) return;
		reloadMap();
		// StartCoroutine(DelayRevive(player));
	}

	private IEnumerator DelayRevive(Player player)
	{
		yield return new WaitForSecondsRealtime(0.5f);
		PlayerWasAdded(player);
		player.data.healthHandler.Revive();
	}
}