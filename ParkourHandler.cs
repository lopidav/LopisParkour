
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

namespace LopisParkourNS;
public class ParkourHandler : GameModeHandler<GM_Parkour>
{
	public override string Name => "Parkour";

	public override bool AllowTeams => false;

	public override UISettings UISettings => new UISettings("A Parkour mode");

	public override GameSettings Settings { get; set; }

	public ParkourHandler()
		: base("Parkour")
	{
		Settings = new GameSettings();
		
	}

	public override void PlayerJoined(Player player)
	{
		LopisParkour.Log("PlayerJoined");
		// base.GameMode.InvokeMethod("PlayerWasAdded", player);
		
	}

	public override void PlayerDied(Player killedPlayer, int playersAlive)
	{
		LopisParkour.Log("PlayerDied");
		// base.GameMode.InvokeMethod("PlayerDied", killedPlayer, playersAlive);
	}

	public override TeamScore GetTeamScore(int teamID)
	{
		return new TeamScore(0, 0);
	}

	public override void SetTeamScore(int teamID, TeamScore score)
	{
	}

	public override void SetActive(bool active)
	{
		base.GameMode.gameObject.SetActive(active);
	}

	public override void StartGame()
	{
		LopisParkour.Log("StartGame");
		base.GameMode.gameObject.SetActive(value: true);
		// CameraHandler.Mode = CameraHandler.CameraMode.FollowPlayer;
	}

	public override void ResetGame()
	{
		LopisParkour.Log("ResetGame");
		base.GameMode.gameObject.SetActive(value: false);
		// PlayerManager.instance.InvokeMethod("ResetCharacters");
	}
}
