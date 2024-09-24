using MapsExt.MapObjects;
using MapsExt;
using UnityEngine;
using UnboundLib;
namespace LopisParkourNS;

public class RectFinishData : SpatialMapObjectData
{
    [SerializeField] private GoldMedalProperty _gold= 0;
    public GoldMedalProperty GoldMedal { get => this._gold; set => this._gold = value; }
    [SerializeField] private SilverMedalProperty _silver= 0;
    public SilverMedalProperty SilverMedal { get => this._silver; set => this._silver = value; }
    [SerializeField] private BronzeMedalProperty _bronze= 0;
    public BronzeMedalProperty BronzeMedal { get => this._bronze; set => this._bronze = value; }
    [SerializeField] private ShinyMedalProperty _shiny= 0;
    public ShinyMedalProperty ShinyMedal { get => this._shiny; set => this._shiny = value; }
}

[MapObject(typeof(RectFinishData))]
public class RectFinish : IMapObject
{
    public virtual GameObject Prefab => MapObjectManager.LoadCustomAsset<GameObject>("Ground");

    public virtual void OnInstantiate(GameObject instance)
    {
        instance.GetOrAddComponent<FinishMono>();

    }
}