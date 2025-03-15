
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
	GhostReplayRecord replayRecord = new GhostReplayRecord();
	public int counter = 0;
	public int skipHowMany = 2;
	public float timer = 0f;
	public Player? ourPlayer = null;
	public List<GameObject> ourBullets = new List<GameObject>();
	private Tuple<float,float,float>? prevRecord = null;
	private Tuple<float,float,float>? nextRecord = null;

	protected virtual void OnEnable()
	{
		On.Block.RPCA_DoBlock += RegisterBlock;

		On.Gun.BulletInit += DetectBullet; //Messes bullets up
		// LopisParkour.OnBulletInit += DetectBullet;
	}

    public void DetectBullet(On.Gun.orig_BulletInit orig, Gun self, GameObject bullet, int usedNumberOfProjectiles, float damageM, float randomSeed, bool useAmmo)
    {
		ourBullets.Add(bullet);
		replayRecord.timedBulletPositions.Add(new List<Tuple<float, float, float>>());
		recordBullets();
		orig(self, bullet, usedNumberOfProjectiles, damageM, randomSeed, useAmmo);
    }

    public void RegisterBlock(On.Block.orig_RPCA_DoBlock orig, Block self, bool firstBlock, bool dontSetCD, BlockTrigger.BlockTriggerType triggerType, Vector3 useBlockPos, bool onlyBlockEffects)
    {
		if (GameModeManager.CurrentHandler.GameMode is not GM_Parkour) {orig(self, firstBlock, dontSetCD, triggerType, useBlockPos, onlyBlockEffects);return;}
		if (GM_Parkour.instance._selectedMap == null) {orig(self, firstBlock, dontSetCD, triggerType, useBlockPos, onlyBlockEffects);return;}
		if (GM_Parkour.instance.isTransitioning) {orig(self, firstBlock, dontSetCD, triggerType, useBlockPos, onlyBlockEffects);return;}
		if (MapTransition.isTransitioning) {orig(self, firstBlock, dontSetCD, triggerType, useBlockPos, onlyBlockEffects);return;}
		if (GM_Parkour.timerStop) {orig(self, firstBlock, dontSetCD, triggerType, useBlockPos, onlyBlockEffects);return;}
		if (PlayerManager.instance.players.Count == 0) {orig(self, firstBlock, dontSetCD, triggerType, useBlockPos, onlyBlockEffects);return;}

		if (ourPlayer == null && PlayerManager.instance.players.Any()) ourPlayer = PlayerManager.instance.players[0];
		if (ourPlayer == null) {orig(self, firstBlock, dontSetCD, triggerType, useBlockPos, onlyBlockEffects);return;}

		
		// ourPlayer.data.weaponHandler.gun.ShootPojectileAction = null; 

		if (ourPlayer.data.block != self) return;
		LopisParkour.Log("block detected");
		replayRecord.timedBlocks.Add(timer);
		orig(self, firstBlock, dontSetCD, triggerType, useBlockPos, onlyBlockEffects);
    }


    protected virtual void OnDisable()
	{
		On.Block.RPCA_DoBlock -= RegisterBlock;
		On.Gun.BulletInit -= DetectBullet;
		// if (LopisParkour.OnBulletInit != null) LopisParkour.OnBulletInit -= DetectBullet;
	}

	private void FixedUpdate()
	{
		if (GameModeManager.CurrentHandler.GameMode is not GM_Parkour) return;
		if (GM_Parkour.instance._selectedMap == null) return;
		if (GM_Parkour.instance.isTransitioning) return;
		if (MapTransition.isTransitioning) return;
		if (GM_Parkour.timerStop) return;
		if (PlayerManager.instance.players.Count == 0) return;
		if (ourPlayer == null && PlayerManager.instance.players.Any()) ourPlayer = PlayerManager.instance.players[0];
		if (ourPlayer == null) return;


		if (counter > 0) {counter--;timer += Time.fixedDeltaTime;return;}
		if (nextRecord?.Item2 == prevRecord?.Item2 && nextRecord?.Item3 == prevRecord?.Item3
			&& (nextRecord?.Item2 != ourPlayer?.transform.position.x || nextRecord?.Item3 != ourPlayer?.transform.position.y)
			&& nextRecord != null)
		{
			replayRecord.timedPositions.Add(nextRecord);
		}

		nextRecord = new Tuple<float,float,float>(timer, ourPlayer?.transform.position.x ?? 10000f, ourPlayer?.transform.position.y ?? 10000f);
		
		recordBullets();
		
		timer += Time.fixedDeltaTime;
		if (nextRecord?.Item2 == prevRecord?.Item2 && nextRecord?.Item3 == prevRecord?.Item3)
		{
			return;
		}
		if (nextRecord != null) replayRecord.timedPositions.Add(nextRecord);
		prevRecord = nextRecord;
		// SpawnPoint component = ((Component)this).gameObject.GetComponent<SpawnPoint>();
		// Vector3 val = MainCam.instance.cam.WorldToScreenPoint(((Component)this).transform.position) - MainCam.instance.cam.WorldToScreenPoint(((Component)MainCam.instance.cam).transform.position);
		// ((Graphic)_labelBg).rectTransform.anchoredPosition = (val);
		counter = skipHowMany;
	}
	public void recordBullets()
	{
		if (GameModeManager.CurrentHandler.GameMode is not GM_Parkour) return;
		if (GM_Parkour.instance._selectedMap == null) return;
		if (GM_Parkour.instance.isTransitioning) return;
		if (MapTransition.isTransitioning) return;
		if (GM_Parkour.timerStop) return;
		// LopisParkour.Log("recording bullets");
		for (var i = 0; i < ourBullets.Count; i++)
		{
			if (ourBullets[i] == null) continue;
			while (replayRecord.timedBulletPositions.Count < i + 1) replayRecord.timedBulletPositions.Add(new List<Tuple<float, float, float>>());
			replayRecord.timedBulletPositions[i].Add(new Tuple<float, float, float>(timer, ourBullets[i].transform.position.x, ourBullets[i].transform.position.y));
		}
	}
	public void SaveReplay(string? fileName)
	{
		// var record = new GhostReplayRecord();
		// record.timedPositions = replayRecord;
		replayRecord.mapId = GM_Parkour.instance._selectedMap.id;
		replayRecord.mods = BepInEx.Bootstrap.Chainloader.PluginInfos.Select(x=>x.Key).ToList();
		LopisParkour.Log(String.Join(", ",BepInEx.Bootstrap.Chainloader.PluginInfos.Select(x=>x.Key).ToList()));
		replayRecord.version = LopisParkour.Version;
		System.IO.Directory.CreateDirectory(Path.Combine(BepInEx.Paths.GameRootPath, "ParkourReplays"));
		File.WriteAllBytes(Path.Combine(BepInEx.Paths.GameRootPath, "ParkourReplays", (fileName ?? LopisParkour.pathToId(GM_Parkour.instance._selectedMap.id).Replace('\\','_') /* + "_" + String.Format("{0:ssMMHHddMMyyyy}", DateTime.Now)*/) + ".prkr"), SerializationUtility.SerializeValue<GhostReplayRecord>(replayRecord, DataFormat.Binary, null));
	}
	public void Reset()
	{
		replayRecord = new GhostReplayRecord();
		timer = 0f;
	}
}
