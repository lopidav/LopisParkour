
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
	public List<Tuple<float,float,float>> timedPositions = new List<Tuple<float,float,float>>();
	public string mapId = "";
}