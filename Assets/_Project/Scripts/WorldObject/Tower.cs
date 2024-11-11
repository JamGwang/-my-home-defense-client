using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;

public class Tower : WorldBase<TowerDataSO>
{
    [SerializeField] private Transform beamPosition;
    private bool isAttackDelay = false;

    int level;
    public int power { get => data.power + data.powerPerLv * level; }
    public float extra { get => data.extra + data.extraPerLv * level; }

    public int towerId;
    public ePlayer player;

    bool isStop = false;

    public override void Init(BaseDataSO data)
    {
        this.data = (TowerDataSO)data;
    }

    private void Awake()
    {
        data = DataManager.instance.GetData<TowerDataSO>("TOW00001");
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isAttackDelay || player == ePlayer.another || isStop) return;
        if (collision.gameObject.TryGetComponent(out Monster monster))
        {
            if (!monster.isDead)
                OnAttackMonster(monster);
        }
    }

    public void OnAttackMonster(Monster monster)
    {
        if (monster == null) return;
        var beam = Instantiate(ResourceManager.instance.LoadAsset<BeamObject>("BeamObject"), beamPosition).SetTimer().SetTarget(monster);
        isAttackDelay = true;
        monster.SetDamage(power);
        if (player == ePlayer.me)
        {
            StartCoroutine(OnCooldown());
            GamePacket packet = new GamePacket();
            packet.TowerAttackRequest = new C2STowerAttackRequest() { MonsterId = monster.monsterId, TowerId = towerId };
            SocketManager.instance.Send(packet);
        }
    }

    public IEnumerator OnCooldown()
    {
        yield return new WaitForSeconds(data.cooldown / 60);
        isAttackDelay = false;
    }

    public void StopTower()
    {
        isStop = true;
    }
}