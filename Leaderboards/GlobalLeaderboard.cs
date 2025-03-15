
using System.Text.RegularExpressions;
using MapsExt;
using MapsExt.MapObjects;
using MapsExt.Visualizers;
using Photon.Realtime;
using Sirenix.Serialization;
using Steamworks;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
namespace LopisParkourNS;
public class GlobalLeaderboard
{
	public static List<LeaderboardRecord> records = new List<LeaderboardRecord>();
	public static Dictionary<string,string> mapIdToEditURL = new Dictionary<string,string>();
	public static string saveFileName = "parkourLeaderboardData.sav"; 
	public static Action? onLeaderboardStartedDownload;
	public static Action? onLeaderboardSubmittionStart;
	public static Action? onLeaderboardFinishedDownload;
	public static Action? onLeaderboardFail;
	public static int processesUpdating = 0;
	public static bool isUpdating => processesUpdating > 0;
	public static bool filled = false;
	public static float lastRequestTime = -1f;
	public static float requestCooldown = 20;
	public static bool wasQueued = false;
	public static void saveUrls()
	{
		if (mapIdToEditURL == null) return;
		if (mapIdToEditURL.Count == 0) return;
		File.WriteAllBytes(Path.Combine(BepInEx.Paths.GameRootPath, saveFileName), SerializationUtility.SerializeValue<Dictionary<string,string>>(mapIdToEditURL, DataFormat.Binary, null));
	}
	public static void readUrls()
	{
		mapIdToEditURL = File.Exists(Path.Combine(BepInEx.Paths.GameRootPath, saveFileName)) ? SerializationUtility.DeserializeValue<Dictionary<string,string>>(File.ReadAllBytes(Path.Combine(BepInEx.Paths.GameRootPath, saveFileName)), (DataFormat.Binary), null) : new Dictionary<string,string>();
		
	}
	public static void QueueUpdate()
	{
		if (wasQueued) return;
		wasQueued = true;
		LopisParkour.instance.ExecuteAfterSeconds(requestCooldown - Time.time + lastRequestTime + 0.1f, () =>
		{
			wasQueued = false;
			RequestUpdate();
		});
		return;
	}
	public static async void RequestUpdate()
	{
		if (Time.time - lastRequestTime < requestCooldown && lastRequestTime != -1f)
		{
			QueueUpdate();
			return;
		}
		else
		{
			lastRequestTime = Time.time;
		}
		if (isUpdating) return;
		// var sheetUrl = "https://docs.google.com/spreadsheets/d/1v_Jlht_8CTWbLuXgZCkRH0I80KpKmQi0Owkd5J9J-40/pub?gid=1713390722&single=true&output=tsv";
		var sheetUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vTwPlhqxhy5MsAAMs38LOe1jMcayb4VpZkIYKZ1sdltek4PnbEWT7r5AsbuzLZYb0Wd4iXd1_gnEALf/pub?gid=1664204216&single=true&output=tsv";
		var downloadService = new GoogleSheetsDownloadService(sheetUrl);
		processesUpdating++;
		string response = "";
		if (onLeaderboardStartedDownload != null) onLeaderboardStartedDownload.Invoke();

		try
		{
			response = await downloadService.DownloadAsync();
		}
		catch (Exception ex)
		{
			if (onLeaderboardFail != null) onLeaderboardFail.Invoke();
			LopisParkour.Log("failed to download leaderboard" + ex.Message);
		}
		processesUpdating--;
		lastRequestTime = Time.time;
		if (processesUpdating <0) processesUpdating = 0;

		if (string.IsNullOrEmpty(response)) return;
		var rows = response.Split(new char[] { '\n'}, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToList();
		rows.ForEach(s => {
			var raw = new LeaderboardRecord(s);
			if (raw.time == -1f) return;
			var ind = records.FindIndex(lr => lr.id == raw.id && lr.mapId == raw.mapId);
			if (ind == -1)
			{
				records.Add(raw);
			}
			else if (records[ind].time > raw.time)
			{
				records[ind].time = raw.time;
			}
		});
		filled = true;
		if (onLeaderboardFinishedDownload != null) onLeaderboardFinishedDownload.Invoke();
        LopisParkour.Log("leaderboard downloaded");
        LopisParkour.Log(GlobalLeaderboard.records.Count + " records");
		var keys = mapIdToEditURL.Keys.Select(x=>x).ToList();
		var id = SteamUser.GetSteamID().ToString();
		foreach (var mapId in keys)
		{
			if (GetMyRecordForMap(mapId) == null) mapIdToEditURL.Remove(mapId);
		}
	}
	public static List<LeaderboardRecord> GetLeaderboardForMap(string mapId)
	{
        // LopisParkour.Log("leaderboard count");
        // LopisParkour.Log(records.Count);
		return records.FindAll(lr => lr.mapId == mapId).OrderBy(x=>x.time).ToList();
	}
	public static LeaderboardRecord? GetMyRecordForMap(string mapId)
	{
		var id = SteamUser.GetSteamID().ToString();
		return records.FirstOrDefault(lr => lr.mapId == mapId && lr.id == id);
	}
	public static void AddLocalRecord(LeaderboardRecord record)
	{
		if (record.time == -1f) return;
		var ind = records.FindIndex(lr => lr.id == record.id && lr.mapId == record.mapId);
		if (ind == -1)
		{
			record.isLocal = true;
			records.Add(record);
		}
		else if (records[ind].time > record.time)
		{
			records[ind].isLocal = true;
			records[ind].time = record.time;
		}
		if (onLeaderboardFinishedDownload != null) onLeaderboardFinishedDownload.Invoke();
	}
	public static async void AddRecord(string mapId, float time)
	{
			LopisParkour.Log("sending time");
		// var mapId = LopisParkour.pathToId(GM_Parkour.instance._selectedMap.id);
		var prevOnlineRecord = GlobalLeaderboard.GetMyRecordForMap(mapId);
		
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
		MedalType newMedal = time >= bronzeTime ? MedalType.None
			: time >= silverTime ? MedalType.Bronze
			: time >= goldTime ? MedalType.Silver
			: time >= shinyTime ? MedalType.Golden
			: MedalType.Shiny;

		if (
		(prevOnlineRecord != null && prevOnlineRecord.time >= 0 && prevOnlineRecord.time <= time )
		|| time <= 0
		|| goldTime + silverTime + bronzeTime == 0 || time > goldTime
		|| GM_Parkour.instance._selectedMap.id.StartsWith(BepInEx.Paths.GameRootPath) )
		{
			LopisParkour.Log("time doesn't count");
			return;
		}
		if (onLeaderboardSubmittionStart != null) onLeaderboardSubmittionStart.Invoke();
		// GlobalLeaderboard.AddLocalRecord(new LeaderboardRecord(SteamUser.GetSteamID().ToString(),SteamFriends.GetPersonaName(), mapId, time));

		var fields = new Dictionary<string, string>
		{
			{ "usp", "pp_url" },
			{ "entry.947442791", SteamUser.GetSteamID().ToString()},
			{ "entry.230836744", SteamFriends.GetPersonaName() },
			{ "entry.728914614", mapId},
			{ "entry.849905133", Math.Round(GM_Parkour.timePassed * 1000).ToString()},
			{ "entry.558170443", String.Join(",",BepInEx.Bootstrap.Chainloader.PluginInfos.Select(x=>x.Key).Where(x=>!LopisParkour.modWhitelist.Contains(x)))},
		};

		var formUrl = "https://docs.google.com/forms/u/3/d/e/1FAIpQLSctn-4nrblqszIcZisvSfXQd72erl1tpDHlgrVpS_4Q2qra6Q/formResponse";
		if (mapIdToEditURL.ContainsKey(mapId) && filled)
		{
			LopisParkour.Log("found eligable leaderboards record edit id " + mapIdToEditURL[mapId]);
			//this worked:
		//https://docs.google.com/forms/d/e/1FAIpQLSctn-4nrblqszIcZisvSfXQd72erl1tpDHlgrVpS_4Q2qra6Q/formResponse?edit2=2_ABaOnuf2mVRzOcd9McaUCQM36AO1wxIAdeI6jZBREDhgvhLZw8UgHvjWf-yJ05NE2Z3_p9U&entry.947442791=2222&entry.230836744=22222&entry.728914614=22222&entry.849905133=22222
			// formUrl = "https://docs.google.com/forms/d/e/1FAIpQLSctn-4nrblqszIcZisvSfXQd72erl1tpDHlgrVpS_4Q2qra6Q/formResponse";
			// fields["usp"] = "form_confirm&amp";
			fields.Remove("usp");
			fields["edit2"] = mapIdToEditURL[mapId];

		}
		var submissionService = new GoogleFormsSubmissionService(formUrl);
		submissionService.SetFieldValues(fields);
		var response = await submissionService.SubmitAsync();
		// LopisParkour.Log(response);
		if (!response.IsSuccessStatusCode && mapIdToEditURL.ContainsKey(mapId)) 
		{
			mapIdToEditURL.Remove(mapId);
			AddRecord(mapId, time);
			return;
		}
		// LopisParkour.Log(response);
		var stringResponce = await response.Content.ReadAsStringAsync();
		// LopisParkour.Log(stringResponce);
		var editUrl = Regex.Match(stringResponce, @"https:\/\/docs\.google\.com\/forms\/u\/3\/d\/e\/1FAIpQLSctn-4nrblqszIcZisvSfXQd72erl1tpDHlgrVpS_4Q2qra6Q\/viewform\?usp=form_confirm&amp;edit2=(.*?)" + '"');
		if(editUrl.Groups[1].Success)
		{
			mapIdToEditURL[mapId] = editUrl.Groups[1].Value;
			saveUrls();
			// LopisParkour.Log(editUrl.Groups[1].Value);
		}
		if (response.IsSuccessStatusCode) AddLocalRecord(new LeaderboardRecord(SteamUser.GetSteamID().ToString(), SteamFriends.GetPersonaName(), mapId, time));
		RequestUpdate();
	}
}