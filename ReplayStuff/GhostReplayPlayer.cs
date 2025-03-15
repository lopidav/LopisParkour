
using MapsExt;
using MapsExt.MapObjects;
using MapsExt.Visualizers;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
namespace LopisParkourNS;
public class GhostReplayPlayer : MonoBehaviour
{
	public GameObject? groundCircle;
	public List<GameObject?> bulletCircles = new List<GameObject?>();
	public GhostReplayRecord replayRecord = new GhostReplayRecord();
	public int currentReplayStep = 0; 
	public int currentBlockReplayStep = 0; 
	public float timer = 0f;
	public int rememebredPlayerCount = 0;
	public string mapId = "";

	protected virtual void OnEnable()
	{
		
		// GameObject instance = UnityEngine.Object.Instantiate<GameObject>(MapObjectManager.LoadCustomAsset<GameObject>("Ground Circle"), Vector3.zero, Quaternion.identity, MapsExtended.instance.);
		// GameObject val5 = new GameObject("Image");
		// val5.transform.SetParent(this.gameObject.transform);
		// val5.transform.localScale = Vector3.one;
		// _labelBg = (Image)(object)val5.AddComponent<ProceduralImage>();
		// ((Graphic)_labelBg).rectTransform.sizeDelta = new Vector2(30f, 30f);
		// ((Graphic)_labelBg).color = new Color(1f, 1f, 0.1f, 0.5f);
		// UniformModifier uniformModifier = val5.AddComponent<UniformModifier>();
		// uniformModifier.Radius = 15f;
	}

	protected virtual void OnDisable()
	{
		UnityEngine.Object.Destroy(groundCircle);
		bulletCircles.ForEach(bullet => {if(bullet!=null)UnityEngine.Object.Destroy(bullet);});
	}
	protected virtual void OnDestroy()
	{
		UnityEngine.Object.Destroy(groundCircle);
		bulletCircles.ForEach(bullet => {if(bullet!=null)UnityEngine.Object.Destroy(bullet);});
	}
	private void Update()
	{
		if (GameModeManager.CurrentHandler.GameMode is not GM_Parkour) {UnityEngine.Object.Destroy(this);return;}
		if (GM_Parkour.instance._selectedMap == null) {UnityEngine.Object.Destroy(this);return;}
		if (GM_Parkour.instance._selectedMap.id != replayRecord.mapId) {UnityEngine.Object.Destroy(this);return;}
		if (GM_Parkour.instance.isTransitioning) {timer= 0;return;}
		if (LopisParkour.parkourWinMenu.activeInHierarchy) {UnityEngine.Object.Destroy(this);return;}
		if (!MapManager.instance.currentMap.Map.hasEntered || MapTransition.isTransitioning) return;
		if (PlayerManager.instance.players.Count != rememebredPlayerCount) {this.Reset();rememebredPlayerCount =PlayerManager.instance.players.Count;}

		if (groundCircle == null)
		{
			MapsExtended.MapObjectManager.Instantiate<GroundCircleData>(this.gameObject.transform.parent, (go) => {
				groundCircle = go;
				groundCircle.transform.localScale = new Vector3(1.35f, 1.35f,1f);
				groundCircle.gameObject.layer = LayerMask.NameToLayer("BackgroundObject");
				UnityEngine.GameObject.Destroy(groundCircle.GetOrAddComponent<PolygonCollider2D>());
				// var coll = groundCircle.GetOrAddComponent<PolygonCollider2D>();
				// coll.isTrigger = true;
				groundCircle.GetOrAddComponent<RectTransform>();
				RepaintTheGhost();
				LopisParkour.instance.ExecuteAfterFrames(1,RepaintTheGhost);
				LopisParkour.instance.ExecuteAfterFrames(5, RepaintTheGhost);

			});
		}
		if (groundCircle == null) return;
		if (currentReplayStep == replayRecord.timedPositions.Count-2 && replayRecord.timedPositions[currentReplayStep+1].Item1 < timer) this.Reset();
		while (currentReplayStep + 2 < replayRecord.timedPositions.Count && replayRecord.timedPositions[currentReplayStep + 1].Item1 < timer) currentReplayStep++;
		float stage = timer == replayRecord.timedPositions[currentReplayStep].Item1 ? 0 : timer > replayRecord.timedPositions[currentReplayStep+1].Item1 ? 1 : (timer - replayRecord.timedPositions[currentReplayStep].Item1)/(replayRecord.timedPositions[currentReplayStep+1].Item1 - replayRecord.timedPositions[currentReplayStep].Item1);
		groundCircle.transform.position = new Vector3(
			replayRecord.timedPositions[currentReplayStep].Item2 + (replayRecord.timedPositions[currentReplayStep+1].Item2 - replayRecord.timedPositions[currentReplayStep].Item2) * stage,
			replayRecord.timedPositions[currentReplayStep].Item3 + (replayRecord.timedPositions[currentReplayStep+1].Item3 - replayRecord.timedPositions[currentReplayStep].Item3) * stage,
			  groundCircle.transform.position.z);
		
		
		var vacantBulletCircle = 0;
		var currBulletStep = 0;
		bulletCircles.RemoveAll(x=>x==null);
		if (replayRecord.timedBulletPositions != null)
		{
			foreach (var timedBullet in replayRecord.timedBulletPositions)
			{
				if (timedBullet.Count == 0) continue;
				if (timedBullet[0].Item1 > timer) continue;
				if (timedBullet.Last().Item1 < timer) continue;
				currBulletStep = 0;
				while (currBulletStep + 2 < timedBullet.Count && timedBullet[currBulletStep + 1].Item1 < timer) currBulletStep++;
				stage = timer == timedBullet[currBulletStep].Item1 ? 0 : timer > timedBullet[currBulletStep+1].Item1 ? 1 : (timer - timedBullet[currBulletStep].Item1)/(timedBullet[currBulletStep+1].Item1 - timedBullet[currBulletStep].Item1);
				
				if(bulletCircles.Count <= vacantBulletCircle)
				{
					var savedPosition = new Vector3(
						timedBullet[currBulletStep].Item2 + (timedBullet[currBulletStep+1].Item2 - timedBullet[currBulletStep].Item2) * stage,
						timedBullet[currBulletStep].Item3 + (timedBullet[currBulletStep+1].Item3 - timedBullet[currBulletStep].Item3) * stage,
						groundCircle.transform.position.z);
					
					MapsExtended.MapObjectManager.Instantiate<GroundCircleData>(this.gameObject.transform.parent, (go) => {
						bulletCircles.Add(go);
						go.transform.localScale = new Vector3(0.2f, 0.2f,1f);
						go.transform.position = savedPosition;
						go.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
						UnityEngine.GameObject.Destroy(go.GetOrAddComponent<PolygonCollider2D>());
						go.GetOrAddComponent<RectTransform>();
						RepaintBullet(go);
						LopisParkour.instance.ExecuteAfterFrames(1, ()=>RepaintBullet(go));
						LopisParkour.instance.ExecuteAfterFrames(5, ()=>RepaintBullet(go));
					});
				}
				else
				{
					if (bulletCircles[vacantBulletCircle] == null ) continue;
					if (bulletCircles[vacantBulletCircle].activeSelf == false) {bulletCircles[vacantBulletCircle].SetActive(true);RepaintBullet(bulletCircles[vacantBulletCircle]);}
					bulletCircles[vacantBulletCircle].transform.position = new Vector3(
						timedBullet[currBulletStep].Item2 + (timedBullet[currBulletStep+1].Item2 - timedBullet[currBulletStep].Item2) * stage,
						timedBullet[currBulletStep].Item3 + (timedBullet[currBulletStep+1].Item3 - timedBullet[currBulletStep].Item3) * stage,
						groundCircle.transform.position.z);
				}
				vacantBulletCircle++;
			}
			for (int i = vacantBulletCircle; i < bulletCircles.Count; i++)
			{
				if (bulletCircles[i] != null && bulletCircles[i].activeSelf) bulletCircles[i].SetActive(false);
			}
		}

		if (GM_Parkour.timePassed > 0 && replayRecord.timedPositions.Last().Item1 > GM_Parkour.timePassed) timer = GM_Parkour.timePassed;
		
		else timer += Time.deltaTime;
		
		if (replayRecord.timedBlocks.Count > currentBlockReplayStep && replayRecord.timedBlocks[currentBlockReplayStep] <= timer)
		{
			currentBlockReplayStep++;
			PaintForBlock();
			LopisParkour.instance.ExecuteAfterSeconds(0.15f, RepaintTheGhost);
		}

	}
	public void Reset()
	{
		timer = 0f;
		currentReplayStep = 0; 
		currentBlockReplayStep = 0;
		UnityEngine.Object.Destroy(groundCircle);
		bulletCircles.ForEach(bullet => {if(bullet!=null)UnityEngine.Object.Destroy(bullet);});
		bulletCircles.Clear();
	}
	public static void RepaintBullet(GameObject bullet)
	{
		if (bullet == null) return;
		UnityEngine.GameObject.Destroy(bullet.GetComponent<SpriteMask>());
		var sprite = bullet.GetOrAddComponent<SpriteRenderer>();

		sprite.material = LopisParkour.defaultMaterial;
		
		sprite.color = new Color(0.5f, 0.7f, 0.5f, 0.7f);
	}
	public void RepaintTheGhost()
	{
		if (groundCircle == null) return;
		UnityEngine.GameObject.Destroy(groundCircle.GetComponent<SpriteMask>());
		var sprite = groundCircle.GetOrAddComponent<SpriteRenderer>();

		sprite.material = LopisParkour.defaultMaterial;
		
		sprite.color = new Color(0.2f, 0.7f, 0.2f, 0.2f);
	}
	public void PaintForBlock()
	{
		if (groundCircle == null) return;
		var sprite = groundCircle.GetOrAddComponent<SpriteRenderer>();
		sprite.color = new Color(1f, 1f, 1f, 0.25f);
	}
}
