

// MapsExtended.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MapsExt.Visualizers.SpawnVisualizer
using MapsExt;
using MapsExt.MapObjects;
using MapsExt.Visualizers;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
namespace LopisParkourNS;
public class ParkourSpawnVisualizer : MonoBehaviour
{
	public GameObject? groundCircle;
	public SpriteRenderer? groundCircleSprite;
	public Color activeColor = new Color(1f, 1f, 0.4f, 0.2f);
	public Color disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.2f);

	// private Image _labelBg;
	public void SetEnabled(bool enabled)
	{
		((Behaviour)this).enabled = enabled;
	}

	protected virtual void OnEnable()
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

			groundCircle.GetOrAddComponent<SpriteRenderer>().color = disabledColor;

			LopisParkour.instance.ExecuteAfterFrames(1, () => 
			{
				if (groundCircle == null) return;
				UnityEngine.GameObject.Destroy(groundCircle.GetComponent<SpriteMask>());
				if (groundCircleSprite == null) groundCircleSprite = groundCircle.GetOrAddComponent<SpriteRenderer>();

				groundCircleSprite.material = LopisParkour.defaultMaterial;
				groundCircleSprite.color = disabledColor;
			});
			UnityEngine.GameObject.Destroy(groundCircle.GetComponent<SpriteMask>());
			LopisParkour.instance.ExecuteAfterFrames(5, () =>
			{
				if (groundCircle == null) return;
				UnityEngine.GameObject.Destroy(groundCircle.GetComponent<SpriteMask>());
				if (groundCircleSprite == null) groundCircleSprite = groundCircle.GetOrAddComponent<SpriteRenderer>();

				groundCircleSprite.material = LopisParkour.defaultMaterial;
				groundCircleSprite.color = disabledColor;
			});

		});
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

	private void LateUpdate()
	{
		if (groundCircle != null)
		{
			if (PlayerManager.instance.players.Count > 0) UnityEngine.Object.Destroy(this);
			else
			{
				if (groundCircleSprite == null) groundCircleSprite = groundCircle.GetOrAddComponent<SpriteRenderer>();
				groundCircle.transform.position = this.transform.position;
				if (groundCircleSprite.color == disabledColor && MapManager.instance.currentMap.Map.hasEntered && !MapTransition.isTransitioning)
				{
					groundCircleSprite.color = activeColor;
				}
			}
		}
		// SpawnPoint component = ((Component)this).gameObject.GetComponent<SpawnPoint>();
		// Vector3 val = MainCam.instance.cam.WorldToScreenPoint(((Component)this).transform.position) - MainCam.instance.cam.WorldToScreenPoint(((Component)MainCam.instance.cam).transform.position);
		// ((Graphic)_labelBg).rectTransform.anchoredPosition = (val);
	}
}
