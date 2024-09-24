using MapsExt.MapObjects;
using MapsExt;
using UnityEngine;
using UnboundLib;
using MapsExt.Editor.MapObjects;

namespace LopisParkourNS;

[EditorMapObject(typeof(DamageRectData), "Damage box (Saw but rect)", Category = "Parkur")]
public sealed class EditorDamageRect : DamageRect
{

}