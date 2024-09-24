using MapsExt.MapObjects;
using MapsExt;
using UnityEngine;
using UnboundLib;
namespace LopisParkourNS;

public class BackgroundGroundData : SpatialMapObjectData
{

    // [SerializeField] private ColorProperty _color = new ColorProperty();

    // public ColorProperty Color { get => this._color; set => this._color = value; }
}

[MapObject(typeof(BackgroundGroundData))]
public class BackgroundGround : IMapObject
{
    public virtual GameObject Prefab => MapObjectManager.LoadCustomAsset<GameObject>("Ground Circle");

    public virtual void OnInstantiate(GameObject instance)
    {
        instance.layer = LayerMask.NameToLayer("BackgroundObject");
    }
}