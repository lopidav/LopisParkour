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
public class SilverMedalProperty : ValueProperty<float>
{
    [SerializeField] private readonly float _SilverMedalTime;

    public override float Value => this._SilverMedalTime;

    public SilverMedalProperty() { }

    public SilverMedalProperty(float SilverMedalTime)
    {
        this._SilverMedalTime = SilverMedalTime;
    }

    public override bool Equals(ValueProperty<float> other) => this.Value == other.Value;

    public SilverMedalProperty Lerp(SilverMedalProperty end, float t) => new(Mathf.LerpAngle(this.Value, end.Value, t));
    public IProperty Lerp(IProperty end, float t) => this.Lerp((SilverMedalProperty) end, t);

    public static implicit operator SilverMedalProperty(float angle) => new(angle);
    public static implicit operator float(SilverMedalProperty prop) => prop.Value;

    public static SilverMedalProperty operator +(SilverMedalProperty a, SilverMedalProperty b) => a.Value + b.Value;
    public static SilverMedalProperty operator -(SilverMedalProperty a, SilverMedalProperty b) => a.Value - b.Value;
    public static Vector3 operator *(SilverMedalProperty a, Vector2 b) => a * b;
    public static Vector3 operator *(SilverMedalProperty a, Vector3 b) => a * b;
}

[PropertySerializer(typeof(SilverMedalProperty))]
public class SilverMedalPropertySerializer : IPropertyWriter<SilverMedalProperty>
{
    public virtual void WriteProperty(SilverMedalProperty property, GameObject target)
    {
		target.GetOrAddComponent<SilverMedalPropertyInstance>().SilverTime = property;
    }
}
[EditorPropertySerializer(typeof(SilverMedalProperty))]
public class EditorSilverMedalPropertySerializer : SilverMedalPropertySerializer, IPropertyReader<SilverMedalProperty>
{
	public override void WriteProperty(SilverMedalProperty property, GameObject target)
	{
		base.WriteProperty(property, target);
	}

	public virtual SilverMedalProperty ReadProperty(GameObject instance)
	{
		return instance.GetComponent<SilverMedalPropertyInstance>().SilverTime;
	}
}


public class SilverMedalPropertyInstance : MonoBehaviour
{
    public SilverMedalProperty SilverTime { get; set; } = new();
}

[InspectorElement(typeof(SilverMedalProperty))]
public class SilverMedalElement : FloatElement
{

    public SilverMedalElement() 
		: base("Silver Time (s)", 0f, float.MaxValue)
    {
    }

    protected override float GetValue()
    {
        return base.Context.InspectorTarget.ReadProperty<SilverMedalProperty>();
    }
    protected override void OnChange(float value, ChangeType changeType)
    {
		if (changeType == ChangeType.Change || changeType == ChangeType.ChangeEnd)
		{
			base.Context.InspectorTarget.WriteProperty((SilverMedalProperty)(value));
		}
		if (changeType == ChangeType.ChangeEnd)
		{
			base.Context.Editor.TakeSnaphot();
		}
    }
}

