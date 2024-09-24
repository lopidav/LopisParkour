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

namespace LopisParkourNS;
public class GM_Parkour : MonoBehaviour
{
	public static GM_Parkour instance;
	public CustomMap _selectedMap;
	public bool isTransitioning = false;
	public static GhostReplayRecorder recorder = null;
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
		
		MenuHandler.CreateButton("Reload map (hotkey: R)", LopisParkour.pauseMenu, reloadMap).transform.SetAsFirstSibling();
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

		recorder = this.gameObject.GetOrAddComponent<GhostReplayRecorder>();

		// MapsExtended.instance CameraHandler.Mode = CameraHandler.CameraMode.FollowPlayer;
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
	public void reloadMap()
	{
		if (isTransitioning) return;
		LoadSelectedMap();
		PlayerManager.instance.RemovePlayers();
		PlayerManager.instance.ResetCharacters();
		timerStop = false;
		TimeHandler.instance.gameOverTime = 1f;
		MapTransition.isTransitioning = true;
		recorder.Reset();
	}
	public void MapWon()
	{
		timerStop = true;
		StartCoroutine(MapWonCorutine());
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
		
		
		// UIHandler.instance.popUpHandler.isPicking = true;
		// UIHandler.instance.popUpHandler.fToCall = (answer) =>
		// {
		// 	if (answer == PopUpHandler.YesNo.Yes)
		// 	{
		// 		TimeHandler.instance.DoSpeedUp();
		// 		reloadMap();
		// 		UIHandler.instance.HideJoinGameText();
		// 	}
		// 	else
		// 	{
		// 		TimeHandler.instance.DoSpeedUp();

		// 		UIHandler.instance.HideJoinGameText();
		// 		MainMenuHandler.instance.Open();
		// 		StartCoroutine(MapManager.instance.UnloadAfterSeconds(MapManager.instance.currentMap.Scene));
		// 		MapTransition.instance.Exit(MapManager.instance.currentMap.Map);
		// 		PlayerManager.instance.RemovePlayers();
		// 		// LopisParkour.parkourMenu.gameObject.SetActive(value: true);
		// 	}
		// };
		// UIHandler.instance.popUpHandler.yesPart.particleSettings.color = PlayerManager.instance.GetColorFromTeam(PlayerManager.instance.players.FirstOrDefault().teamID).winText;
		// UIHandler.instance.popUpHandler.noPart.particleSettings.color = PlayerManager.instance.GetColorFromTeam(PlayerManager.instance.players.FirstOrDefault().teamID).winText;
		// UIHandler.instance.popUpHandler.yesPart.loop = true;
		// UIHandler.instance.popUpHandler.yesPart.Play();
		// UIHandler.instance.popUpHandler.yesAnim.PlayIn();
		// UIHandler.instance.popUpHandler.noPart.loop = true;
		// UIHandler.instance.popUpHandler.noPart.Play();
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
		MedalType newMedal = timePassed >= bronzeTime ? MedalType.None
			: timePassed >= silverTime ? MedalType.Bronze
			: timePassed >= goldTime ? MedalType.Silver
			: timePassed >= shinyTime ? MedalType.Golden
			: MedalType.Shiny;

		TextMeshProUGUI pbText;
		TextMeshProUGUI timeDiff;
		TextMeshProUGUI timeText;
		
		MenuHandler.CreateText(mapToName(GM_Parkour.instance.selectedMap), LopisParkour.parkourWinMenu, out pbText, 30);
		if ((prevEntry.time == -1 || prevEntry.time > timePassed) && timePassed != 0)
		{
			if (prevEntry.medal < newMedal)
					MenuHandler.CreateText(newMedal.ToString() + " Medal!", LopisParkour.parkourWinMenu, out pbText, 100);
			else
				MenuHandler.CreateText("New Personal Best!", LopisParkour.parkourWinMenu, out pbText, 100);
			LopisParkour.AddRecord(GM_Parkour.instance.selectedMap.id, timePassed, newMedal);
		}
		else
		{
			MenuHandler.CreateText(" ", LopisParkour.parkourWinMenu, out pbText, 100);
		}

		if (timePassed != 0)
		{
			MenuHandler.CreateText(TimeSpan.FromSeconds(timePassed).ToString("mm\\:ss\\.fff"), LopisParkour.parkourWinMenu, out timeText, 80);
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
		if (timePassed > shinyTime)
		{
			MedalType nextMedal = MedalType.None;
			float nextMedalTime = -1f;

			if (bestTime > bronzeTime && bronzeTime != 0)
			{
				nextMedal = MedalType.Bronze;
				nextMedalTime = bronzeTime;
			}
			else if (bestTime > silverTime && silverTime != 0)
			{
				nextMedal = MedalType.Silver;
				nextMedalTime = silverTime;
			}
			else if (bestTime > goldTime && goldTime != 0)
			{
				nextMedal = MedalType.Golden;
				nextMedalTime = goldTime;
			}
			else if (bestTime > shinyTime && shinyTime != 0)
			{
				nextMedal = MedalType.Shiny;
				nextMedalTime = shinyTime;
			}
			
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
		}

		MenuHandler.CreateButton("Improve", LopisParkour.parkourWinMenu, ()=>{
			TimeHandler.instance.gameOverTime = 1f;
			MainMenuHandler.instance.Close();
			var gm = (GM_Parkour) GameModeManager.CurrentHandler.GameMode;
			gm.reloadMap();
		});

		CustomMap targetMap = getNextMap();

		GameObject? rateMenu = null;
		rateMenu = MenuHandler.CreateMenu("RateTheMap", () => {
			MenuHandler.CreateText("Please rate the map you just played:", rateMenu, out _ );
			MenuHandler.CreateText(" ", rateMenu, out _ );
			MenuHandler.CreateButton(("Bad and i hate it"), rateMenu, ()=>{
				LopisParkour.RateMap(GM_Parkour.instance.selectedMap.id, 1);
				TimeHandler.instance.gameOverTime = 1f;
				MainMenuHandler.instance.Close();
				GM_Parkour.instance.selectedMap = targetMap;
			});
			MenuHandler.CreateButton(("It's aight"), rateMenu, ()=>{
				LopisParkour.RateMap(GM_Parkour.instance.selectedMap.id, 2);
				TimeHandler.instance.gameOverTime = 1f;
				MainMenuHandler.instance.Close();
				GM_Parkour.instance.selectedMap = targetMap;
			});
			MenuHandler.CreateButton(("Good one"), rateMenu, ()=>{
				LopisParkour.RateMap(GM_Parkour.instance.selectedMap.id, 3);
				TimeHandler.instance.gameOverTime = 1f;
				MainMenuHandler.instance.Close();
				GM_Parkour.instance.selectedMap = targetMap;
			});

		}, LopisParkour.parkourWinMenu, out var rateMenuButton);


		GameObject? saveReplayButton = null;
		saveReplayButton = MenuHandler.CreateButton("Save replay", LopisParkour.parkourWinMenu, ()=>{
			recorder.SaveReplay();
			saveReplayButton.GetComponentInChildren<TextMeshProUGUI>().text = "saved";
		});

		rateMenuButton.GetComponentInChildren<TextMeshProUGUI>().text = targetMap == null ? "no more maps in this pack" : "next map (" + mapToName(targetMap) + ")";
		// MenuHandler.CreateButton("Next map: " + mapToName(targetMap), LopisParkour.parkourWinMenu, ()=>{
		// 	TimeHandler.instance.gameOverTime = 1f;
		// 	MainMenuHandler.instance.Close();
		// 	GM_Parkour.instance.selectedMap = targetMap;
		// });


		// GM_Parkour.instance.StartCoroutine(MapManager.instance.UnloadAfterSeconds(MapManager.instance.currentMap.Scene));
		// MapManager.instance.UnloadScene(MapManager.instance.currentMap.Scene);
		
		// MainMenuHandler.instance.Open();
		// loadMapThroughExtended(pair.Value);

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
		MapManager.instance.view.RPC("RPCA_SetCallInNextMap", RpcTarget.All, true);
		MapManager.instance.view.RPC("RPCA_LoadLevel", RpcTarget.All, $"MapsExtended:{selectedMap._id}");
		PlayerAssigner.instance.SetPlayersCanJoin(canJoin: true);
		timerStop = false;
		if (EscapeMenuHandler.isEscMenu)
		{
			LopisParkour.pauseMenu.GetComponentInParent<EscapeMenuHandler>().ToggleEsc();
		}
		pauseNextMapButtonUpdate();
		// MapTransition.instance.Enter(MapManager.instance.currentMap.Map);
		// NetworkingManager.RPC(typeof(TRTMapManager), nameof(RPCA_SetCurrentLevel), TRTMapManager.CurrentLevel);
	}
	public void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		if (MapManager.instance.currentMap != null)
		{
			SceneManager.UnloadSceneAsync(MapManager.instance.currentMap.Scene);
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