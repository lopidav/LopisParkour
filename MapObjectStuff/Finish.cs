using MapsExt.MapObjects;
using MapsExt;
using UnityEngine;
using UnboundLib;
namespace LopisParkourNS;

public class FinishData : SpatialMapObjectData
{
    [SerializeField] private GoldMedalProperty _gold= 0;
    public GoldMedalProperty GoldMedal { get => this._gold; set => this._gold = value; }
    [SerializeField] private SilverMedalProperty _silver= 0;
    public SilverMedalProperty SilverMedal { get => this._silver; set => this._silver = value; }
    [SerializeField] private BronzeMedalProperty _bronze= 0;
    public BronzeMedalProperty BronzeMedal { get => this._bronze; set => this._bronze = value; }
    [SerializeField] private ShinyMedalProperty _shiny= 0;
    public ShinyMedalProperty ShinyMedal { get => this._shiny; set => this._shiny = value; }
    // [SerializeField] private string _nextMap = "";
    // [SerializeField] private bool _goToNext = false;
}

[MapObject(typeof(FinishData))]
public class Finish : IMapObject
{
    public virtual GameObject Prefab => MapObjectManager.LoadCustomAsset<GameObject>("Ground Circle");

    public virtual void OnInstantiate(GameObject instance)
    {
        instance.GetOrAddComponent<FinishMono>();

        // SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
        // instance.tag = "NoMask";

        // SpriteMask mask = instance.GetComponent<SpriteMask>();
        // UnityEngine.GameObject.Destroy(mask);

        // spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // GetColor[] getColors = instance.GetComponentsInChildren<GetColor>();

        // LopisParkour.instance.ExecuteAfterFrames(1, () =>
        // {
        //     SpriteMask mask = instance.GetComponent<SpriteMask>();
        //     UnityEngine.GameObject.Destroy(mask);
        // });

        // LopisParkour.instance.ExecuteAfterFrames(5, () =>
        // {
        //     SpriteMask mask = instance.GetComponent<SpriteMask>();
        //     UnityEngine.GameObject.Destroy(mask);
        // });

        // for (int i = getColors.Length - 1; i >= 0; i--)
        // {
        //     UnityEngine.GameObject.DestroyImmediate(getColors[i]);
        // }
    }
}