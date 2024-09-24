
using MapsExt;
using MapsExt.MapObjects;
using MapsExt.Visualizers;
using Sirenix.Serialization;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
namespace LopisParkourNS;
public class GhostReplayRecorder : MonoBehaviour
{
	public List<Tuple<float,float,float>> replayRecord = new List<Tuple<float,float,float>>();
	public int counter = 0;
	public int skipHowMany = 2;
	public float timer = 0f;

	protected virtual void OnEnable()
	{
	}

	protected virtual void OnDisable()
	{
	}

	private void FixedUpdate()
	{
		if (GameModeManager.CurrentHandler.GameMode is not GM_Parkour) return;
		if (GM_Parkour.instance._selectedMap == null) return;
		if (GM_Parkour.instance.isTransitioning) return;
		if (MapTransition.isTransitioning) return;
		if (GM_Parkour.timerStop) return;
		if (PlayerManager.instance.players.Count == 0) return;
		Player? ourPlayer = null;
		if (PlayerManager.instance.players.Any()) ourPlayer = PlayerManager.instance.players[0];
		if (counter > 0) {counter--;timer += Time.fixedDeltaTime;return;}
		replayRecord.Add(new Tuple<float,float,float>(timer, ourPlayer?.transform.position.x ?? 1000f, ourPlayer?.transform.position.y ?? 1000f));
		timer += Time.fixedDeltaTime;
		// SpawnPoint component = ((Component)this).gameObject.GetComponent<SpawnPoint>();
		// Vector3 val = MainCam.instance.cam.WorldToScreenPoint(((Component)this).transform.position) - MainCam.instance.cam.WorldToScreenPoint(((Component)MainCam.instance.cam).transform.position);
		// ((Graphic)_labelBg).rectTransform.anchoredPosition = (val);
		counter = skipHowMany;
	}
	public void SaveReplay()
	{
		var record = new GhostReplayRecord();
		record.timedPositions = replayRecord;
		record.mapId = GM_Parkour.instance._selectedMap.id;
		System.IO.Directory.CreateDirectory(Path.Combine(BepInEx.Paths.GameRootPath, "ParkourReplays"));
		File.WriteAllBytes(Path.Combine(BepInEx.Paths.GameRootPath, "ParkourReplays", LopisParkour.pathToId(GM_Parkour.instance._selectedMap.id).Replace('\\','_') + "_" + String.Format("{0:ssMMHHddMMyyyy}"+".prkr", DateTime.Now)), SerializationUtility.SerializeValue<GhostReplayRecord>(record, DataFormat.Binary, null));
	}
	public void Reset()
	{
		replayRecord = new List<Tuple<float,float,float>>();
		timer = 0f;
	}
}
