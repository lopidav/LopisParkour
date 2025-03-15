
using MapsExt;
using MapsExt.MapObjects;
using MapsExt.Visualizers;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
namespace LopisParkourNS;
[Serializable]
public class GhostReplayRecord
{
	public string version = "";
	
	public List<Tuple<float,float,float>> timedPositions = new List<Tuple<float,float,float>>();
	public List<string> mods = new List<string>();
	public List<float> timedBlocks = new List<float>();
	public List<List<Tuple<float,float,float>>> timedBulletPositions = new List<List<Tuple<float,float,float>>>();
	public string mapId = "";
}