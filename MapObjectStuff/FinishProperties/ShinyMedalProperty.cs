using MapsExt.MapObjects;
using MapsExt;
using UnityEngine;
using UnboundLib;
using MapsExt.Editor.MapObjects;
using MapsExt.Properties;
using MapsExt.Editor.UI;
using UnityEngine.UI;
using MapsExt.Editor.Utils;
using MapsExt.Editor.Properties;

namespace LopisParkourNS;
public class ShinyMedalProperty : ValueProperty<float>
{
    [SerializeField] private readonly float _ShinyMedalTime;

    public override float Value => this._ShinyMedalTime;

    public ShinyMedalProperty() { }

    public ShinyMedalProperty(float ShinyMedalTime)
    {
        this._ShinyMedalTime = ShinyMedalTime;
    }

    public override bool Equals(ValueProperty<float> other) => this.Value == other.Value;

    public ShinyMedalProperty Lerp(ShinyMedalProperty end, float t) => new(Mathf.LerpAngle(this.Value, end.Value, t));
    public IProperty Lerp(IProperty end, float t) => this.Lerp((ShinyMedalProperty) end, t);

    public static implicit operator ShinyMedalProperty(float angle) => new(angle);
    public static implicit operator float(ShinyMedalProperty prop) => prop.Value;

    public static ShinyMedalProperty operator +(ShinyMedalProperty a, ShinyMedalProperty b) => a.Value + b.Value;
    public static ShinyMedalProperty operator -(ShinyMedalProperty a, ShinyMedalProperty b) => a.Value - b.Value;
    public static Vector3 operator *(ShinyMedalProperty a, Vector2 b) => a * b;
    public static Vector3 operator *(ShinyMedalProperty a, Vector3 b) => a * b;
}

[PropertySerializer(typeof(ShinyMedalProperty))]
public class ShinyMedalPropertySerializer : IPropertyWriter<ShinyMedalProperty>
{
    public virtual void WriteProperty(ShinyMedalProperty property, GameObject target)
    {
		target.GetOrAddComponent<ShinyMedalPropertyInstance>().ShinyTime = property;
    }
}
[EditorPropertySerializer(typeof(ShinyMedalProperty))]
public class EditorShinyMedalPropertySerializer : ShinyMedalPropertySerializer, IPropertyReader<ShinyMedalProperty>
{
	public override void WriteProperty(ShinyMedalProperty property, GameObject target)
	{
		base.WriteProperty(property, target);
	}

	public virtual ShinyMedalProperty ReadProperty(GameObject instance)
	{
		return instance.GetComponent<ShinyMedalPropertyInstance>().ShinyTime;
	}
}


public class ShinyMedalPropertyInstance : MonoBehaviour
{
    public ShinyMedalProperty ShinyTime { get; set; } = new();
}

[InspectorElement(typeof(ShinyMedalProperty))]
public class ShinyMedalElement : FloatElement
{

    public ShinyMedalElement() 
		: base("Shiny Time (s)", 0f, float.MaxValue)
    {
    }

    protected override float GetValue()
    {
        return base.Context.InspectorTarget.ReadProperty<ShinyMedalProperty>();
    }
    protected override void OnChange(float value, ChangeType changeType)
    {
		if (changeType == ChangeType.Change || changeType == ChangeType.ChangeEnd)
		{
			base.Context.InspectorTarget.WriteProperty((ShinyMedalProperty)(value));
		}
		if (changeType == ChangeType.ChangeEnd)
		{
			base.Context.Editor.TakeSnaphot();
		}
    }
}

