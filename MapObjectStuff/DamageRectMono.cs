using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnboundLib;
using HarmonyLib;
using UnboundLib.GameModes;
using Sonigon;

namespace LopisParkourNS;
public class DamageRectMono : MonoBehaviour
{

	public float shake = 2f;
    public float counter = 0f;
    public virtual Color DamageRectColor
    {
        get
        {
            return new Color(0.8f, 0.3f, 0.2f, 0.2f);
        }
    }
    public virtual void Start()
    {

        this.gameObject.layer = LayerMask.NameToLayer("BackgroundObject");
        var coll = gameObject.GetOrAddComponent<BoxCollider2D>();
        coll.isTrigger = true;
        gameObject.GetOrAddComponent<RectTransform>();

        UnityEngine.GameObject.Destroy(gameObject.GetComponent<SpriteMask>());
        gameObject.GetOrAddComponent<SpriteRenderer>().material = LopisParkour.defaultMaterial;

        gameObject.GetOrAddComponent<SpriteRenderer>().color = DamageRectColor;

        LopisParkour.instance.ExecuteAfterFrames(1, () => 
        {
            UnityEngine.GameObject.Destroy(gameObject.GetComponent<SpriteMask>());
            var sprite = gameObject.GetOrAddComponent<SpriteRenderer>();

            sprite.material = LopisParkour.defaultMaterial;
            sprite.color = DamageRectColor;
        });
        UnityEngine.GameObject.Destroy(gameObject.GetComponent<SpriteMask>());
        LopisParkour.instance.ExecuteAfterFrames(5, () =>
        {
            UnityEngine.GameObject.Destroy(gameObject.GetComponent<SpriteMask>());
            var sprite = gameObject.GetOrAddComponent<SpriteRenderer>();

            sprite.material = LopisParkour.defaultMaterial;
            sprite.color = DamageRectColor;
        });

    }

    private void FixedUpdate()
    {
        if (!gameObject.GetComponent<Renderer>() || PlayerManager.instance.players.Count == 0)
        {
            return;
        }
        counter += TimeHandler.fixedDeltaTime;
        if (counter > 1f) counter = 1f;

        // var tempHits = new List<RaycastHit2D>();

        // var tempCorners = new Vector3[4];
        // gameObject.GetOrAddComponent<RectTransform>().GetWorldCorners(tempCorners);
        // Vector2[] DamageRectCorners = tempCorners.Select(corner => (Vector2)corner).ToArray();

        // // Get all collisions going clockwise
        // for (int i = 0; i < DamageRectCorners.Length; i++)
        // {
        //     if (i == 0)
        //     {
        //         tempHits.AddRange(Physics2D.LinecastAll(DamageRectCorners[DamageRectCorners.Length - 1], DamageRectCorners[0]));
        //     }
        //     else
        //     {
        //         tempHits.AddRange(Physics2D.LinecastAll(DamageRectCorners[i - 1], DamageRectCorners[i]));
        //     }
        // }

        // // Get all collisions going counterclockwise
        // for (int i = 0; i < DamageRectCorners.Length; i++)
        // {
        //     if (i == 0)
        //     {
        //         tempHits.AddRange(Physics2D.LinecastAll(DamageRectCorners[0], DamageRectCorners[DamageRectCorners.Length - 1]));
        //     }
        //     else
        //     {
        //         tempHits.AddRange(Physics2D.LinecastAll(DamageRectCorners[i], DamageRectCorners[i - 1]));
        //     }
        // }

        // hits = tempHits.ToArray();

        var colliders = Physics2D.OverlapBoxAll(transform.position, gameObject.GetComponent<RectTransform>().lossyScale, Vector2.SignedAngle(Vector2.up, transform.up.normalized));


        foreach (var collider in colliders)
        {
            HandleCollision(collider);
        }
    }
    public virtual void HandleCollision(Collider2D collider)
    {
        if (collider.gameObject == this.gameObject)
        {
            return;
        }

        // If Player
        if (collider.GetComponent<Player>())
        {
            HandlePlayer(collider.GetComponent<Player>());

            return;
        }

        // If Bullet
        // if (collider.GetComponentInParent<ProjectileHit>())
        // {
        //     HandleBullet(collider.GetComponentInParent<ProjectileHit>());

        //     return;
        // }

        // If Box
        // if (collider.GetComponent<Rigidbody2D>())
        // {
        //     HandleBox(collider.GetComponent<Rigidbody2D>());

        //     return;
        // }
    }
    public virtual void HandlePlayer(Player player)
    {
        // player.data.TouchWall();
        var whyIsItNot2d = gameObject.transform.position;
        whyIsItNot2d.z = 0;
        Vector3 normalized = (player.transform.position - whyIsItNot2d).normalized;
		player.data.sinceGrounded = 0f;

        var HalfUpperAngle = Mathf.Rad2Deg * Mathf.Atan(gameObject.transform.localScale.x/gameObject.transform.localScale.y);
        var HalfSideAngle = 90 - HalfUpperAngle;
        var pushDir = Vector3.SignedAngle(gameObject.transform.up, normalized, new Vector3(0,0,1f)) switch
        {
            float i when i <= -(HalfUpperAngle + HalfSideAngle*2) => -gameObject.transform.up,
            float i when i > -(HalfUpperAngle + HalfSideAngle*2) && i <= -HalfUpperAngle => gameObject.transform.right,
            float i when i > -HalfUpperAngle && i <= HalfUpperAngle => gameObject.transform.up,
            float i when i > HalfUpperAngle && i <= HalfUpperAngle + HalfSideAngle*2 => -gameObject.transform.right,
            float i when i > HalfUpperAngle + HalfSideAngle*2   => -gameObject.transform.up,
            _ => gameObject.transform.up
        };
        
		if (counter > 0.1f && player.data.view.IsMine)
		{
			counter = 0f;
			if (player.data.block.IsBlocking())
			{
				// rpc.CallFunction("ShieldOutOfBounds");
				player.data.playerVel.velocity *= 0f;
				player.data.healthHandler.CallTakeForce(pushDir * 400f * player.data.playerVel.mass, ForceMode2D.Impulse, forceIgnoreMass: false, ignoreBlock: true);
				// player.data.transform.position = gameObject.transform.position;
			}
			else
			{
				// rpc.CallFunction("OutOfBounds");
				player.data.healthHandler.CallTakeForce(pushDir * 200f * player.data.playerVel.mass, ForceMode2D.Impulse, forceIgnoreMass: false, ignoreBlock: true);
				player.data.healthHandler.CallTakeDamage(51f * pushDir, player.data.transform.position);
			}
            SoundManager.Instance.Play(player.data.playerSounds.soundCharacterDamageScreenEdge, player.data.transform);
		}
            // float num = 1;
            // ForceMultiplier component = player.GetComponent<ForceMultiplier>();
            // if ((bool)component)
            // {
            //     num *= component.multiplier;
            // }
            // Vector3 forceDir = normalized;
            // forceDir.y *= 0.5f;
            // LopisParkour.Log(forceDir * num * TimeHandler.deltaTime * force);
            // LopisParkour.Log(forceDir );
            // LopisParkour.Log(num );
            // LopisParkour.Log(forceDir * num * 0.0005f * TimeHandler.deltaTime * force );
            // player.data.playerVel.AddForce(forceDir * num * TimeHandler.deltaTime * force, ForceMode2D.Force);
            // player.data.healthHandler.TakeForce(forceDir * num * TimeHandler.deltaTime * force);

			// player.data.healthHandler.CallTakeForce(base.transform.up * 200f * player.data.playerVel.mass, ForceMode2D.Impulse, forceIgnoreMass: false, ignoreBlock: true);
			// player.data.healthHandler.CallTakeDamage(51f * base.transform.up, player.data.transform.position);
        // if ((bool)sparkTransform)
        // {
        // 	sparkTransform.transform.position = player.transform.position;
        // 	if (normalized != Vector3.zero)
        // 	{
        // 		sparkTransform.rotation = Quaternion.LookRotation(normalized);
        // 	}
        // }
        GamefeelManager.GameFeel((normalized + UnityEngine.Random.onUnitSphere).normalized * shake * TimeHandler.deltaTime * 20f);
    }

}