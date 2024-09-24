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
public class GoldMedalProperty : ValueProperty<float>
{
    [SerializeField] private readonly float _goldMedalTime;

    public override float Value => this._goldMedalTime;

    public GoldMedalProperty() { }

    public GoldMedalProperty(float goldMedalTime)
    {
        this._goldMedalTime = goldMedalTime;
    }

    public override bool Equals(ValueProperty<float> other) => this.Value == other.Value;

    // public GoldMedalProperty Lerp(GoldMedalProperty end, float t) => new(Mathf.LerpAngle(this.Value, end.Value, t));
    public IProperty Lerp(IProperty end, float t) => this.Lerp((GoldMedalProperty) end, t);

    public static implicit operator GoldMedalProperty(float angle) => new(angle);
    public static implicit operator float(GoldMedalProperty prop) => prop.Value;

    public static GoldMedalProperty operator +(GoldMedalProperty a, GoldMedalProperty b) => a.Value + b.Value;
    public static GoldMedalProperty operator -(GoldMedalProperty a, GoldMedalProperty b) => a.Value - b.Value;
    public static Vector3 operator *(GoldMedalProperty a, Vector2 b) => a * b;
    public static Vector3 operator *(GoldMedalProperty a, Vector3 b) => a * b;
}

[PropertySerializer(typeof(GoldMedalProperty))]
public class GoldMedalPropertySerializer : IPropertyWriter<GoldMedalProperty>
{
    public virtual void WriteProperty(GoldMedalProperty property, GameObject target)
    {
		target.GetOrAddComponent<GoldMedalPropertyInstance>().GoldTime = property;
    }
}
[EditorPropertySerializer(typeof(GoldMedalProperty))]
public class EditorGoldMedalPropertySerializer : GoldMedalPropertySerializer, IPropertyReader<GoldMedalProperty>
{
	public override void WriteProperty(GoldMedalProperty property, GameObject target)
	{
		base.WriteProperty(property, target);
	}

	public virtual GoldMedalProperty ReadProperty(GameObject instance)
	{
		return instance.GetComponent<GoldMedalPropertyInstance>().GoldTime;
	}
}


public class GoldMedalPropertyInstance : MonoBehaviour
{
    public GoldMedalProperty GoldTime { get; set; } = new();
}

[InspectorElement(typeof(GoldMedalProperty))]
public class GoldMedalElement : FloatElement
{

    public GoldMedalElement() 
		: base("Gold Time (s)", 0f, float.MaxValue)
    {
    }

    protected override float GetValue()
    {
        return base.Context.InspectorTarget.ReadProperty<GoldMedalProperty>();
    }
    protected override void OnChange(float value, ChangeType changeType)
    {
		if (changeType == ChangeType.Change || changeType == ChangeType.ChangeEnd)
		{
			base.Context.InspectorTarget.WriteProperty((GoldMedalProperty)(value));
		}
		if (changeType == ChangeType.ChangeEnd)
		{
			base.Context.Editor.TakeSnaphot();
		}
    }
}

