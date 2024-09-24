using MapsExt.MapObjects;
using MapsExt;
using UnityEngine;
using UnboundLib;
namespace LopisParkourNS;

public class DamageRectData : SpatialMapObjectData
{
}

[MapObject(typeof(DamageRectData))]
public class DamageRect : IMapObject
{
    public virtual GameObject Prefab => MapObjectManager.LoadCustomAsset<GameObject>("Ground");

    public virtual void OnInstantiate(GameObject instance)
    {
        var coll = instance.GetOrAddComponent<DamageRectMono>();

    }
}