using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.VisualScripting;
using UnityEngine.Events;

public enum ePlayer
{
    me,
    another
}

public class Monster : WorldBase<MonsterDataSO>
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CircleCollider2D circleCollider;
    [SerializeField] private Rigidbody2D rigidBody;

    HpGauge hpGauge;

    public ePlayer player;

    public int level;
    public int atk { get => data.atk + data.atkPerLv * (level - 1); }
    public int maxHp { get => data.maxHp + data.hpPerLv * (level - 1); }
    public int def { get => data.def + data.defPerLv * (level - 1); }

    public int nowHp;

    public int monsterId;

    private bool isStop = false;

    public bool isDead = false;

    public override void Init(BaseDataSO data)
    {
        this.data = (MonsterDataSO)data;
        //spriteRenderer.sprite = ResourceManager.instance.LoadAsset<Sprite>(data.rcode);
        var roads = player == ePlayer.me ? GameManager.instance.roads1 : GameManager.instance.roads2;
        rigidBody.velocity = roads[0].right * this.data.spd;
        hpGauge = Instantiate(ResourceManager.instance.LoadAsset<HpGauge>("HpGauge"), UIManager.Get<UIGame>().hpParent);
        hpGauge.SetTraget(transform, player);
        hpGauge.Init(maxHp);
    }

    public void SetMonster(ePlayer player)
    {
        this.player = player;
    }

    public void SetLevel(BaseDataSO data, int level)
    {
        this.level = level;
        Init(data);
        nowHp = maxHp;
        gameObject.name = data.rcode;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.tag == "EndPoint")
        {
            //GameManager.instance.OnDamagedHome(atk);
            if (GameManager.instance.isGameStart &&
                player == ePlayer.me)
            {
                GamePacket packet = new GamePacket();
                packet.MonsterAttackBaseRequest = new C2SMonsterAttackBaseRequest() { Damage = atk };
                SocketManager.instance.Send(packet);
            }
            GameManager.instance.RemoveMonster(this);
            Destroy(hpGauge.gameObject);
            Destroy(gameObject);
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (isStop) return;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Road"))
            rigidBody.velocity = collision.transform.right * this.data.spd;
    }

    public void SetDamage(int dmg)
    {
        AudioManager.instance.PlayOneShot("attacked");
        var result = dmg - data.def;
        nowHp -= result;
        hpGauge.SetProgress(nowHp);
        if (nowHp <= 0 && player == ePlayer.me)
        {
            isDead = true;
            GamePacket packet = new GamePacket();
            packet.MonsterDeathNotification = new C2SMonsterDeathNotification() { MonsterId = monsterId };
            SocketManager.instance.Send(packet);
            GameManager.instance.RemoveMonster(monsterId);
        }
    }

    public IEnumerator OnDeath(UnityAction<Monster> callback)
    {
        StopMonster();
        circleCollider.enabled = false;
        //GameManager.instance.gold += 5;
        var color = spriteRenderer.color;
        for (int i = 0; i < 60; i++)
        {
            color.a -= Mathf.Lerp(0, 1, 60f - i / 60f);
            yield return null;
        }
        callback.Invoke(this);
        if(hpGauge != null && hpGauge.gameObject != null)
            Destroy(hpGauge.gameObject);
        Destroy(gameObject);
    }

    public void StopMonster()
    {
        if (rigidBody != null)
        {
            rigidBody.velocity = Vector3.zero;
        }
    }
}