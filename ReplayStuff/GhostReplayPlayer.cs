
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
public class GhostReplayPlayer : MonoBehaviour
{
	public GameObject? groundCircle;
	public List<Tuple<float,float,float>> replayRecord = new List<Tuple<float,float,float>>();
	public int currentReplayStep = 0; 
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
	}

	private void Update()
	{
		if (GameModeManager.CurrentHandler.GameMode is not GM_Parkour) return;
		if (GM_Parkour.instance._selectedMap == null) {UnityEngine.Object.Destroy(this);return;}
		if (GM_Parkour.instance._selectedMap.id != mapId) {UnityEngine.Object.Destroy(this);return;}
		if (GM_Parkour.instance.isTransitioning) return;
		if (MapTransition.isTransitioning) return;
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

				UnityEngine.GameObject.Destroy(groundCircle.GetComponent<SpriteMask>());
				groundCircle.GetOrAddComponent<SpriteRenderer>().material = LopisParkour.defaultMaterial;

				groundCircle.GetOrAddComponent<SpriteRenderer>().color = new Color(1f, 0.8f, 0.4f, 0.25f);

				LopisParkour.instance.ExecuteAfterFrames(1, () => 
				{
					UnityEngine.GameObject.Destroy(groundCircle.GetComponent<SpriteMask>());
					var sprite = groundCircle.GetOrAddComponent<SpriteRenderer>();

					sprite.material = LopisParkour.defaultMaterial;
					sprite.color = new Color(1f, 0.8f, 0.4f, 0.25f);
				});
				UnityEngine.GameObject.Destroy(groundCircle.GetComponent<SpriteMask>());
				LopisParkour.instance.ExecuteAfterFrames(5, () =>
				{
					UnityEngine.GameObject.Destroy(groundCircle.GetComponent<SpriteMask>());
					var sprite = groundCircle.GetOrAddComponent<SpriteRenderer>();

					sprite.material = LopisParkour.defaultMaterial;
					sprite.color = new Color(1f, 0.8f, 0.4f, 0.25f);
				});

			});
		}
		// if (currentReplayStep == replayRecord.Count) {UnityEngine.Object.Destroy(this);return;}
		if (currentReplayStep == replayRecord.Count-2 && replayRecord[currentReplayStep+1].Item1 < timer) this.Reset();
		while (currentReplayStep + 2 < replayRecord.Count && replayRecord[currentReplayStep + 1].Item1 < timer) currentReplayStep++;

		float stage = timer == replayRecord[currentReplayStep].Item1 ? 0 : (timer - replayRecord[currentReplayStep].Item1)/(replayRecord[currentReplayStep+1].Item1 - replayRecord[currentReplayStep].Item1);
		groundCircle.transform.position = new Vector3(
			replayRecord[currentReplayStep].Item2 + (replayRecord[currentReplayStep+1].Item2 - replayRecord[currentReplayStep].Item2) * stage,
			replayRecord[currentReplayStep].Item3 + (replayRecord[currentReplayStep+1].Item3 - replayRecord[currentReplayStep].Item3) * stage,
			  groundCircle.transform.position.z);
		timer += Time.deltaTime;
		// SpawnPoint component = ((Component)this).gameObject.GetComponent<SpawnPoint>();
		// Vector3 val = MainCam.instance.cam.WorldToScreenPoint(((Component)this).transform.position) - MainCam.instance.cam.WorldToScreenPoint(((Component)MainCam.instance.cam).transform.position);
		// ((Graphic)_labelBg).rectTransform.anchoredPosition = (val);
	}
	public void Reset()
	{
		timer = 0f;
		currentReplayStep = 0; 
	}
}
