using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Threading;

public class GameManager : MonoSingleton<GameManager>
{
    public bool isLoadDontDestroy;

    public bool isGameStart;

    private int _homeHp1 = 100;
    public int homeHp1
    {
        get => _homeHp1;
        set
        {
            _homeHp1 = value;
            UIManager.Get<UIGame>().SetHpGauge1(value);
        }
    }
    private int _homeHp2 = 100;
    public int homeHp2
    {
        get => _homeHp2;
        set
        {
            _homeHp2 = value;
            UIManager.Get<UIGame>().SetHpGauge2(value);
        }
    }

    private int _gold = 10;
    public int gold
    {
        get => _gold;
        set
        {
            _gold = value;
            UIManager.Get<UIGame>().SetGoldText(_gold);
        }
    }

    private int _score = 0;
    public int score
    {
        get => _score;
        set
        {
            _score = value;
            if(topScore < _score)
            {
                topScore = _score;
            }
            UIManager.Get<UIGame>().SetScoreText(_score);
        }
    }

    private int _topScore = 0;
    public int topScore
    {
        get => _topScore;
        set
        {
            _topScore = value;
            UIManager.Get<UIGame>().SetTopScoreText(_topScore);
        }
    }

    private int _level = 0;
    public int level
    {
        get => _level;
        set
        {
            _level = value;
            UIManager.Get<UIGame>().SetLevelText(_level);
        }
    }

    private float time = 0;

    private List<Monster> monsters = new List<Monster>();

    [HideInInspector] public List<int> usesTowerPositions = new List<int>();

    [HideInInspector] public List<Transform> roads1 = new List<Transform>();
    [HideInInspector] public List<Transform> roads2 = new List<Transform>();

    public GameState playerData;
    public GameState opponentData;
    public InitialGameState initialGameState;

    public GameObjects playerObjects { get => CanvasGame.instance.player1; }
    public GameObjects opponentObjects { get => CanvasGame.instance.player2; }

    public List<Tower> towers = new List<Tower>();

    public bool isLogin = false;

    public void OnGameStart()
    {
        isGameStart = true;
        Time.timeScale = 1;
        _homeHp1 = initialGameState.BaseHp;
        _homeHp2 = initialGameState.BaseHp;
        UIManager.Get<UIGame>().InitHpGauge(homeHp1);
        gold = initialGameState.InitialGold;
        topScore = 0;
        score = 0;
        level = 1;
        time = 0;
        roads1.Clear();
        roads2.Clear();
        towers.Clear();
        monsters.Clear();
        StartCoroutine(MultiGameLoop());
    }

    public int GetRandomTowerPositionIndex(int max = 0)
    {
        var rand = -1;
        int count = 0;
        do
        {
            rand = Util.Random(4, 50);
            count++;
            if (count > 15) break;
        } while (usesTowerPositions.Contains(rand));
        //usesTowerPositions.Add(rand);
        return rand;
    }

    public void AddRoad(Position position, Position nextPos, Transform parent, ePlayer player, int count = 0)
    {
        var roads = player == ePlayer.me ? roads1 : roads2;
        var roadPrefab = ResourceManager.instance.LoadAsset<SpriteRenderer>("Road");

        var newRoad = Instantiate(roadPrefab, parent);
        roads.Add(newRoad.transform);
        newRoad.transform.localPosition = new Vector3(position.X, position.Y);

        if (count > 0)
        {
            var normal = (nextPos.ToVector3() - position.ToVector3()).normalized;
            var isUp = nextPos.Y > position.Y;
            var angle = Mathf.Abs(Mathf.Atan2(normal.y, normal.x) * 180 / Mathf.PI) * (isUp ? 1 : -1);
            var eulerAngle = new Vector3(0, 0, angle);
            newRoad.transform.localEulerAngles = eulerAngle;
            for (int i = 0; i < count; i++)
            {
                var newRoad2 = Instantiate(roadPrefab, parent);
                roads.Add(newRoad2.transform);
                newRoad2.transform.localPosition = position.ToVector3() + normal * 30 * (i + 1);
                newRoad2.transform.localEulerAngles = eulerAngle;
                newRoad2.GetComponent<CircleCollider2D>().enabled = false;
            }
        }
    }

    public void AddRoads()
    {
        var roadPrefab = ResourceManager.instance.LoadAsset<SpriteRenderer>("Road");
        var width = roadPrefab.sprite.bounds.size.x;
        var isUp = false;
        float angle = 0f;
        for (int i = 0; i < 4; i++)
        {
            angle = Util.Random(i == 0 ? -45f : 15, i == 0 ? 45f : 45);
            if(i == 3)
            {
                var normal = (GameObjects.instance.endPosition.position - roads1.Last().position).normalized;
                angle = Mathf.Abs(Mathf.Atan2(normal.y, normal.x) * 180 / Mathf.PI);
            }
            isUp = i == 0 ? (angle > 0 ? true : false) : !isUp;
            for(int j = 0; j < (i < 3 ? 6 : 10); j++)
            {
                var newRoad = Instantiate(roadPrefab);
                newRoad.transform.localEulerAngles = new Vector3(0, 0, i == 0 ? angle : angle * (isUp ? 1 : -1));
                newRoad.transform.position = i == 0 && j == 0 ? GameObjects.instance.startPosition.position :
                    (i != 0 && j == 0 ? roads1.Last().position :
                    roads1.Last().position + roads1.Last().right * width);
                roads1.Add(newRoad.transform);
                if(j == 5) newRoad.GetComponent<CircleCollider2D>().enabled = false;
            }
        }
    }

    private Tower AddTower(Vector3 position)
    {
        return Instantiate(ResourceManager.instance.LoadAsset<Tower>("Tower"), position, new Quaternion());
    }

    private Tower AddTower(float x, float y, ePlayer player)
    {
        var gameObjects = player == ePlayer.me ? playerObjects : opponentObjects;
        var tower = Instantiate(ResourceManager.instance.LoadAsset<Tower>("Tower"), gameObjects.roadParent);
        tower.transform.localPosition = new Vector3(x, y);
        return tower;
    }

    public Tower AddRandomTower()
    {
        var position = CalcRandomTowerPosition();
        var tower = AddTower(position.x, position.y, ePlayer.me);
        tower.Init(DataManager.instance.GetData<TowerDataSO>("TOW00001"));
        towers.Add(tower);
        return tower;
    }

    public Vector3 CalcRandomTowerPosition()
    {
        var rand = GetRandomTowerPositionIndex();
        var position = roads1[rand].transform.localPosition + CalcRandomTowerPositionY();
        //usesTowerPositions.Add(rand);
        return position;
    }

    public Vector3 CalcRandomTowerPositionY()
    {
        var position = new Vector3(0, Util.Random(35f, 41f));
        var position2 = new Vector3(0, Util.Random(-35f, -41f));
        var random = Util.Random(0, 2);
        return random >= 1 ? position : position2;
    }

    public void AddTower(TowerData data, ePlayer player)
    {
        var tower = AddTower(data.X, data.Y, player);
        tower.Init(DataManager.instance.GetData<TowerDataSO>("TOW00001"));
        tower.towerId = data.TowerId;
        tower.player = player;
        towers.Add(tower);
    }

    public void SetTower(int towerId)
    {
        towers.Last().towerId = towerId;
    }

    public void OnGameEnd()
    {
        isGameStart = false;
        monsters.ForEach(obj => obj.StopMonster());
        towers.ForEach(obj => obj.StopTower());
        monsters.Clear();
        towers.Clear();
        StopAllCoroutines();
        StartCoroutine(OnSceneChange());
    }

    IEnumerator OnSceneChange()
    {
        yield return new WaitForSeconds(5);
        AudioManager.instance.StopBgm();
        SceneManager.LoadSceneAsync("Main");
    }

    public void OnDamagedHome(int dmg)
    {
        homeHp1 -= dmg;
        UIManager.Get<UIGame>().SetHpGauge1(homeHp1);
    }

    IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(1);
        AddRoads();
        for (int i = 0; i < 3; i++)
        {
            var rand = GetRandomTowerPositionIndex();
            var tower = AddTower(roads1[rand].transform.position + new Vector3(0, 2 * Util.Random(0, 2) == 0 ? 1 : -1, 0));
            tower.Init(DataManager.instance.GetData<TowerDataSO>("TOW00001"));
            usesTowerPositions.Add(rand);
        }
        while (isGameStart)
        {
            var mon = Instantiate(ResourceManager.instance.LoadAsset<Monster>("Monster"), GameObjects.instance.startPosition.position, new Quaternion());
            monsters.Add(mon);
            mon.SetLevel(DataManager.instance.GetDatas<MonsterDataSO>().RandomValue(), level);
            yield return new WaitForSeconds(2);
        }
    }

    IEnumerator MultiGameLoop()
    {
        topScore = playerData.HighScore;
        for(int i = 0; i < 2; i++)
        {
            var gameState = i == 0 ? playerData : opponentData;
            var gameObjects = i == 0 ? playerObjects : opponentObjects;

            for (int j = 0; j < gameState.MonsterPath.Count; j++)
            {
                var count = 0;
                if (gameState.MonsterPath.Count > j + 1)
                {
                    var dist = Vector3.Distance(gameState.MonsterPath[j].ToVector3(), gameState.MonsterPath[j + 1].ToVector3());
                    count = (int)(dist / 30f);
                }
                AddRoad(gameState.MonsterPath[j], count > 0 ? gameState.MonsterPath[j+1] : null, gameObjects.roadParent, (ePlayer)i, count);
            }
            var roads = i == 0 ? roads1 : roads2;
            gameObjects.endPosition.position = roads.Last().position;
            gameObjects.house.localPosition = gameState.BasePosition.ToVector3();
            UIManager.Get<UIGame>().SetHpGaugeTarget((ePlayer)i, gameObjects.house);

            for (int j = 0; j < gameState.Towers.Count; j++)
            {
                AddTower(gameState.Towers[j], (ePlayer)i);
                /*var tower = AddTower(new Vector3(gameState.Towers[j].X, gameState.Towers[j].Y));
                tower.Init(DataManager.instance.GetData<TowerDataSO>("TOW00001"));
                tower.towerId = gameState.Towers[j].TowerId;*/
            }
        }
        yield return new WaitForSeconds(1);
        while (isGameStart)
        {
            /*var mon = Instantiate(ResourceManager.instance.LoadAsset<Monster>("Monster"), GameObjects.instance.startPosition.position, new Quaternion());
            monsters.Add(mon);
            mon.Init(DataManager.instance.GetDatas<MonsterDataSO>().RandomValue());
            mon.SetLevel(level);*/
            GamePacket packet = new GamePacket();
            packet.SpawnMonsterRequest = new C2SSpawnMonsterRequest();
            SocketManager.instance.Send(packet);
            yield return new WaitForSeconds(initialGameState.MonsterSpawnInterval);
        }
    }

    public void AddMonster(MonsterData data, ePlayer player)
    {
        var roads = player == ePlayer.me ? roads1 : roads2;
        var mon = Instantiate(ResourceManager.instance.LoadAsset<Monster>("Monster"), roads[0].position, new Quaternion());
        monsters.Add(mon);
        mon.monsterId = data.MonsterId;
        mon.player = player;
        mon.SetLevel(DataManager.instance.GetData<MonsterDataSO>("MON0000" + data.MonsterNumber), data.Level);
    }

    public void RemoveMonster(int monsterId)
    {
        monsters.RemoveAll(obj => obj == null);
        var targetMonster = monsters.Find(obj => obj.monsterId == monsterId);
        if (targetMonster != null)
        {
            var idx = monsters.IndexOf(targetMonster);
            StartCoroutine(targetMonster.OnDeath(monster =>
            {
                if (monster == null)
                {
                    monsters.RemoveAt(idx);
                }
                else
                {
                    monsters.Remove(monster);
                }
            }));
        }
    }

    public void RemoveMonster(Monster monster)
    {
        monsters.Remove(monster);
    }

    public void OnAttackMonster(int towerId, int monsterId)
    {
        var targetTower = towers.Find(obj => obj.towerId ==  towerId);
        var targetMonster = monsters.Find(obj => obj.monsterId == monsterId);
        if(targetMonster != null && targetTower != null)
            targetTower.OnAttackMonster(targetMonster);
    }

}