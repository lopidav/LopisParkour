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
public class BronzeMedalProperty : ValueProperty<float>
{
    [SerializeField] private readonly float _BronzeMedalTime;

    public override float Value => this._BronzeMedalTime;

    public BronzeMedalProperty() { }

    public BronzeMedalProperty(float BronzeMedalTime)
    {
        this._BronzeMedalTime = BronzeMedalTime;
    }

    public override bool Equals(ValueProperty<float> other) => this.Value == other.Value;

    // public BronzeMedalProperty Lerp(BronzeMedalProperty end, float t) => new(Mathf.LerpAngle(this.Value, end.Value, t));
    public IProperty Lerp(IProperty end, float t) => this.Lerp((BronzeMedalProperty) end, t);

    public static implicit operator BronzeMedalProperty(float angle) => new(angle);
    public static implicit operator float(BronzeMedalProperty prop) => prop.Value;

    public static BronzeMedalProperty operator +(BronzeMedalProperty a, BronzeMedalProperty b) => a.Value + b.Value;
    public static BronzeMedalProperty operator -(BronzeMedalProperty a, BronzeMedalProperty b) => a.Value - b.Value;
    public static Vector3 operator *(BronzeMedalProperty a, Vector2 b) => a * b;
    public static Vector3 operator *(BronzeMedalProperty a, Vector3 b) => a * b;
}

[PropertySerializer(typeof(BronzeMedalProperty))]
public class BronzeMedalPropertySerializer : IPropertyWriter<BronzeMedalProperty>
{
    public virtual void WriteProperty(BronzeMedalProperty property, GameObject target)
    {
		target.GetOrAddComponent<BronzeMedalPropertyInstance>().BronzeTime = property;
    }
}
[EditorPropertySerializer(typeof(BronzeMedalProperty))]
public class EditorBronzeMedalPropertySerializer : BronzeMedalPropertySerializer, IPropertyReader<BronzeMedalProperty>
{
	public override void WriteProperty(BronzeMedalProperty property, GameObject target)
	{
		base.WriteProperty(property, target);
	}

	public virtual BronzeMedalProperty ReadProperty(GameObject instance)
	{
		return instance.GetComponent<BronzeMedalPropertyInstance>().BronzeTime;
	}
}


public class BronzeMedalPropertyInstance : MonoBehaviour
{
    public BronzeMedalProperty BronzeTime { get; set; } = new();
}

[InspectorElement(typeof(BronzeMedalProperty))]
public class BronzeMedalElement : FloatElement
{

    public BronzeMedalElement() 
		: base("Bronze Time (s)", 0f, float.MaxValue)
    {
    }

    protected override float GetValue()
    {
        return base.Context.InspectorTarget.ReadProperty<BronzeMedalProperty>();
    }
    protected override void OnChange(float value, ChangeType changeType)
    {
		if (changeType == ChangeType.Change || changeType == ChangeType.ChangeEnd)
		{
			base.Context.InspectorTarget.WriteProperty((BronzeMedalProperty)(value));
		}
		if (changeType == ChangeType.ChangeEnd)
		{
			base.Context.Editor.TakeSnaphot();
		}
    }
}

