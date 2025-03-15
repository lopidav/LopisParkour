using MapsExt.MapObjects;
using MapsExt;
using UnityEngine;
using UnboundLib;
using System.Collections;
using MapsExt.Properties;
using UnboundLib.Utils;

namespace LopisParkourNS;
public class StarterCardMono : MonoBehaviour
{
	private string starterCard = "";
	private List<GameObject> pendingCardObjects = new List<GameObject>();
	private bool enabled = true;
	private List<Player> effectedPlayers = new List<Player>();
	Vector3 DesieredPreviewPosition = Vector3.zero;
	virtual public void Update()
	{
		if (!enabled) return;
		// for (var i = 0; i < pendingCardObjects.Count; i++) {
		// 	pendingCardObjects[i].transform.position = DesieredPreviewPosition + Vector3.left * (pendingCardObjects.Count-i-1) * 7 + Vector3.down * i;
		// }
		PlayerManager.instance.players.ForEach(player =>
			{
				if (!effectedPlayers.Contains(player)) PlayerWasAdded(player);
			});
	}
	
	public string GetStarterCard()
	{
		return starterCard;
	}

	public void SetStarterCard(string starterCard)
	{
		this.starterCard = starterCard;
	}

	private void PlayerWasAdded(Player player)
	{
		effectedPlayers.Add(player);
		pendingCardObjects.ForEach(obj => {
			DelayedPick(obj, player.playerID);
		});
		// DelayedPick(obj, player.playerID);
	}
	public async void DelayedPick(GameObject cardObject, int playerID)
    {
        await Task.Delay(22);
		cardObject.transform.root.GetComponentInChildren<ApplyCardStats>().Pick(playerID, pickerType: PickerType.Player);
		pendingCardObjects.Remove(cardObject);
		CardChoice.instance.spawnedCards.Remove(cardObject);
        UnityEngine.Object.Destroy(cardObject);
		
    }
    public virtual void Start()
    {
		if (!enabled) return;
		LopisParkour.instance.ExecuteAfterFrames(5, ()=>{
			this.addTheCard();
		});
		
	}
	public void addTheCard()
	{
		if (!enabled) return;
		CardInfo? ourCard = null;
		ourCard = CardManager.GetCardInfoWithName(this.starterCard);
		if (ourCard == null)
		{
			CardInfo[] cards = CardChoice.instance.cards;
			int num = -1;
			float num2 = 0f;
			for (int i = 0; i < cards.Length; i++)
			{
				string text = cards[i].GetComponent<CardInfo>().cardName.ToUpper();
				text = text.Replace(" ", "");
				string text2 = this.starterCard.ToUpper();
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

		if (ourCard == null) {LopisParkour.Log("starter card not found:"+ starterCard);return;}
		
		GameObject obj = CardChoice.instance.AddCard(ourCard);
		obj.GetComponentInChildren<CardVisuals>().firstValueToSet = true;
		obj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
		obj.transform.GetChild(0).localScale = new Vector3(9.5536f, 9.5536f, 9.5536f);
		obj.transform.position = (LopisParkour.cameraHandlerInstance?._targetSize ?? 20f)*(28.5f * Vector3.right + 11 * Vector3.up + 9.5f * Vector3.left * CardChoice.instance.spawnedCards.IndexOf(obj))/20;
		
		// var cardBar = GameObject.Find("/Game/UI/UI_Game/Canvas/CardViz/Pointer");
		// if (cardBar!= null) obj.transform.position = 9 * Vector3.right + 7 * Vector3.down + cardBar.transform.position + 9.5f * Vector3.left * CardChoice.instance.spawnedCards.Count();
		

		pendingCardObjects.Add(obj);
		// setCardPositions();
		// LopisParkour.instance.ExecuteAfterFrames(5,setCardPositions);
		// LopisParkour.instance.ExecuteAfterSeconds(3,setCardPositions);
		LopisParkour.Log("found starter card: "+ourCard.cardName);
		// obj.GetComponentInChildren<CardVisuals>().firstValueToSet = true;
		// effectedPlayers.Add(player);
		// DelayedPick(obj, player.playerID);


		// PlayerManager.instance.PlayerJoinedAction += PlayerWasAdded;//(Action<Player>)Delegate.Combine(playerManager.PlayerJoinedAction, new Action<Player>(PlayerWasAdded));
		// DevConsole.SpawnCard(this.starterCard);
		
	}
	public void setCardPositions()
	{
		pendingCardObjects.ForEach(obj => {
				if(obj == null) return;
				obj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
				// LopisParkour.cameraHandlerInstance?.UpdateTargets();
				obj.transform.position = (LopisParkour.cameraHandlerInstance?._targetSize ?? 20f)*(28.5f * Vector3.right + 11 * Vector3.up + 9.5f * Vector3.left * CardChoice.instance.spawnedCards.IndexOf(obj))/20;
		});
	}
	public virtual void onDisable()
	{
		pendingCardObjects.ForEach(cardObject => {
			CardChoice.instance.spawnedCards.Remove(cardObject);
			UnityEngine.Object.Destroy(cardObject);
			});
		pendingCardObjects.ForEach(cardObject => UnityEngine.Object.Destroy(cardObject));
		PlayerManager.instance.PlayerJoinedAction -= PlayerWasAdded;
	}
	public virtual void OnDestroy()
	{
		pendingCardObjects.ForEach(cardObject => {
			CardChoice.instance.spawnedCards.Remove(cardObject);
			UnityEngine.Object.Destroy(cardObject);
			});
		pendingCardObjects.ForEach(cardObject => UnityEngine.Object.Destroy(cardObject));
		PlayerManager.instance.PlayerJoinedAction -= PlayerWasAdded;
	}


}

public class StarterCardData : SpatialMapObjectData
{
	public CardNameProperty cardName = new CardNameProperty();
}
public class CardNameProperty : IProperty
{
	public string cardName = "";
}

[PropertySerializer(typeof(CardNameProperty))]
public class CardNamePropertySerializer : IPropertyWriter<CardNameProperty>
{
	public virtual void WriteProperty(CardNameProperty property, GameObject target)
	{
		target.GetOrAddComponent<StarterCardMono>().SetStarterCard(property.cardName);
	}
}

[MapObject(typeof(StarterCardData))]
public class StarterCard : IMapObject
{
    public virtual GameObject Prefab => MapObjectManager.LoadCustomAsset<GameObject>("Ground");

    public virtual void OnInstantiate(GameObject instance)
    {
        // UnityEngine.Object.Destroy(instance.GetComponent<Collider2D>());
        // UnityEngine.GameObject.Destroy(instance.GetComponent<SpriteMask>());
        // instance.GetOrAddComponent<StarterCardMono>().Start();

    }
}