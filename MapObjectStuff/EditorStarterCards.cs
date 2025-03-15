using MapsExt.MapObjects;
using MapsExt;
using UnityEngine;
using UnboundLib;
using MapsExt.Editor.MapObjects;
using MapsExt.Editor.Properties;
using MapsExt.Properties;
using MapsExt.Editor.UI;
using UnityEngine.UI;
using UnboundLib.Utils;

namespace LopisParkourNS;


[EditorMapObject(typeof(StarterCardData), "Starter Card", Category = "Parkur")]
public class EditorStarterCard : StarterCard
{
	public override void OnInstantiate(GameObject instance)
	{
		instance.GetOrAddComponent<StarterCardMono>().enabled = false;;
		//  base.OnInstantiate(instance);
	}
}


[EditorPropertySerializer(typeof(CardNameProperty))]
public class EditorCardNamePropertySerializer : CardNamePropertySerializer, IPropertyReader<CardNameProperty>
{
	public virtual CardNameProperty ReadProperty(GameObject instance)
	{
		return new CardNameProperty
		{
			cardName = instance.GetComponent<StarterCardMono>().GetStarterCard()
		};
	}
}

[InspectorElement(typeof(CardNameProperty))]
public class CardNameElement : IInspectorElement
{
	private InspectorContext context;

	private InputField input;

	public string Value
	{
		get
		{
			return context.InspectorTarget.ReadProperty<CardNameProperty>().cardName;
		}
		set
		{
			OnChanged(value);
		}
	}

	public GameObject Instantiate(InspectorContext context)
	{
		this.context = context;
		GameObject gameObject = UnityEngine.Object.Instantiate(Assets.InspectorVector2InputPrefab);
		Transform child = gameObject.transform.GetChild(1);
		Transform child2 = child.GetChild(0);
		Transform child3 = gameObject.transform.GetChild(0);
        UnityEngine.Object.Destroy(gameObject.GetComponent<Vector2Input>());
		UnityEngine.Object.Destroy(gameObject.GetComponent<InspectorVector2Input>());
		UnityEngine.Object.Destroy(child.GetChild(1).gameObject);
		UnityEngine.Object.Destroy(child2.GetChild(0).gameObject);
		child2.GetChild(1).GetChild(0).GetComponent<Text>()
			.text = "Name";
		child3.gameObject.GetComponent<Text>().text = "Name";
		input = gameObject.GetComponentInChildren<InputField>();
		input.onValueChanged.AddListener(OnChanged);
		input.onEndEdit.AddListener(onEndEdit);
		return gameObject;
	}

	public void OnUpdate()
	{
		input.text = Value;
	}

	private void OnChanged(string str)
	{
		CardNameProperty cardNameProperty = new CardNameProperty();

		cardNameProperty.cardName = str;
		context.InspectorTarget.WriteProperty(cardNameProperty);
		context.Editor.TakeSnaphot();
	}
	private void onEndEdit(string str)
	{
		CardNameProperty cardNameProperty = new CardNameProperty();
		CardInfo? ourCard = null;
		ourCard = CardManager.GetCardInfoWithName(str);
		if (ourCard == null)
		{
			CardInfo[] cards = CardManager.allCards;
			int num = -1;
			float num2 = 0f;
			for (int i = 0; i < cards.Length; i++)
			{
				string text = cards[i].GetComponent<CardInfo>().cardName.ToUpper();
				text = text.Replace(" ", "");
				string text2 = str.ToUpper();
				text2 = text2.Replace(" ", "");
				float num3 = 0f;
				for (int j = 0; j < text2.Length; j++)
				{
					if (text.Length > j && text2[j] == text[j])
					{
						num3 += 1f / (float)text2.Length;
					}
				}
				num3 -= (float)Mathf.Abs(text2.Length - text.Length) * 0.001f;
				if (num3 > 0.1f && num3 > num2)
				{
					num2 = num3;
					num = i;
				}
			}
			if (num != -1) ourCard = cards[num];
		}
		if (ourCard != null)
		{
			cardNameProperty.cardName = ((UnityEngine.Object)((Component)ourCard).gameObject).name;
			context.InspectorTarget.WriteProperty(cardNameProperty);
			context.Editor.TakeSnaphot();
		}
	}
}

