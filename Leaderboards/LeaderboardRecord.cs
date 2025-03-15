
using MapsExt;
using MapsExt.MapObjects;
using MapsExt.Visualizers;
using Steamworks;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
namespace LopisParkourNS;
public class LeaderboardRecord
{
	public string id = "";
	public string name = "";
	public string mapId = "";
	public float time = -1f;
	public bool isLocal = false;
	public LeaderboardRecord()
	{
	}
	public LeaderboardRecord(string id, string name, string mapId, float time)
	{
		this.id = id;
		this.name = name;
		this.mapId = mapId;
		this.time = (float)Math.Round(time * 1000) / 1000f;
	}
	public LeaderboardRecord(string row)
	{
		var fields = row.Split(new char[]{'\t'}).Skip(1).ToList();
		if (fields.Count == 0) return;
		id = fields[0];
		name = fields[1];
		mapId = fields[2] ?? "";
		time = float.Parse(fields[3].Replace(".","").Replace(",","") ?? "-1");
		if (time != -1) time /= 1000;
	}
	public override string ToString()
	{
		return name + "\t" + TimeSpan.FromSeconds(time).ToString("mm\\:ss\\.fff");
	}
}