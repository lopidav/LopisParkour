using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnboundLib;
using HarmonyLib;
using UnboundLib.GameModes;

namespace LopisParkourNS;
public class FinishMono : MonoBehaviour
{

    public RaycastHit2D[] hits;
    public float timePassed = 0;
    public bool completed = false;
    public virtual Color finishColor
    {
        get
        {
            return new Color(0.215f, 0.215f, 0.8f, 0.25f);
        }
    }
    public virtual Color completedFinishColor
    {
        get
        {
            return new Color(0.4f, 0.4f, 0.4f, 0.25f);
        }
    }
    public virtual void Start()
    {

        this.gameObject.layer = LayerMask.NameToLayer("BackgroundObject");
        var coll = gameObject.GetOrAddComponent<PolygonCollider2D>();
        coll.isTrigger = true;
        gameObject.GetOrAddComponent<RectTransform>();

        UnityEngine.GameObject.Destroy(gameObject.GetComponent<SpriteMask>());
        gameObject.GetOrAddComponent<SpriteRenderer>().material = LopisParkour.defaultMaterial;

        gameObject.GetOrAddComponent<SpriteRenderer>().color = finishColor;

        LopisParkour.instance.ExecuteAfterFrames(1, () => 
        {
            UnityEngine.GameObject.Destroy(gameObject.GetComponent<SpriteMask>());
            var sprite = gameObject.GetOrAddComponent<SpriteRenderer>();

            sprite.material = LopisParkour.defaultMaterial;
            sprite.color = finishColor;
        });
        UnityEngine.GameObject.Destroy(gameObject.GetComponent<SpriteMask>());
        LopisParkour.instance.ExecuteAfterFrames(5, () =>
        {
            UnityEngine.GameObject.Destroy(gameObject.GetComponent<SpriteMask>());
            var sprite = gameObject.GetOrAddComponent<SpriteRenderer>();

            sprite.material = LopisParkour.defaultMaterial;
            sprite.color = finishColor;
        });

    }

    private void FixedUpdate()
    {
        if (!gameObject.GetComponent<Renderer>() || this.completed || PlayerManager.instance.players.Count == 0)
        {
            return;
        }

        timePassed += Time.fixedDeltaTime;
        // Unbound.BuildInfoPopup(TimeSpan.FromSeconds(this.timePassed).ToString("hh\\:mm\\:ss\\.fff"));

        // var tempHits = new List<RaycastHit2D>();

        // var tempCorners = new Vector3[4];
        // gameObject.GetOrAddComponent<RectTransform>().GetWorldCorners(tempCorners);
        // Vector2[] finishCorners = tempCorners.Select(corner => (Vector2)corner).ToArray();

        // // Get all collisions going clockwise
        // for (int i = 0; i < finishCorners.Length; i++)
        // {
        //     if (i == 0)
        //     {
        //         tempHits.AddRange(Physics2D.LinecastAll(finishCorners[finishCorners.Length - 1], finishCorners[0]));
        //     }
        //     else
        //     {
        //         tempHits.AddRange(Physics2D.LinecastAll(finishCorners[i - 1], finishCorners[i]));
        //     }
        // }

        // // Get all collisions going counterclockwise
        // for (int i = 0; i < finishCorners.Length; i++)
        // {
        //     if (i == 0)
        //     {
        //         tempHits.AddRange(Physics2D.LinecastAll(finishCorners[0], finishCorners[finishCorners.Length - 1]));
        //     }
        //     else
        //     {
        //         tempHits.AddRange(Physics2D.LinecastAll(finishCorners[i], finishCorners[i - 1]));
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
        // var data = player.data;
        if (GameModeManager.CurrentHandler.GameMode is GM_Parkour gm_parkpur)
        {
            gm_parkpur.MapWon();
        }
        else
        {
            Unbound.BuildInfoPopup(this.timePassed.ToString());//TimeSpan.FromSeconds(this.timePassed).ToString("hh\\:mm\\:ss\\.fff"));
        }

        gameObject.GetOrAddComponent<SpriteRenderer>().color = completedFinishColor;
        completed = true;
    }

}