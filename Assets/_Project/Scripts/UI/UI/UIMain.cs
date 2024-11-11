using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class UIMain : UIBase
{
    [SerializeField] private TMP_Text desc;
    [SerializeField] private Button button;
    [SerializeField] private GameObject touch;
    [SerializeField] private GameObject bottom;
    [SerializeField] private GameObject registerButton;
    [SerializeField] private GameObject loginButton;
    [SerializeField] private GameObject gameStartButton;

    public override void Opened(object[] param)
    {
        button.interactable = false;
        bottom.SetActive(false);
        StartCoroutine(Init());
        gameStartButton.SetActive(false);
    }

    IEnumerator Init()
    {
        DataManager.instance.Init();
        desc.text = "������ �ε���";
        yield return new WaitUntil(() => DataManager.instance.isInit);
        desc.text = "터치해주세요.";
        UnityAction callback = () =>
        {
            button.interactable = true;
        };
        if(!GameManager.instance.isLogin)
            UIManager.Show<PopupServerSetting>(callback);
        else
        {
            OnClickMain();
            SetLogin(true);
        }
    }

    public override void HideDirect()
    {
        UIManager.Hide<UIMain>();
    }

    public void OnClickMain()
    {
        touch.SetActive(false);
        bottom.SetActive(true);
    }

    public void OnClickSignUp()
    {
        UIManager.Show<PopupRegister>();
    }

    public void OnClickLogin()
    {
        UIManager.Show<PopupLogin>();
    }

    public void OnClickGameStart()
    {
        //SceneManager.LoadSceneAsync("Game");
        GamePacket gamePacket = new GamePacket();
        gamePacket.MatchRequest = new C2SMatchRequest();
        SocketManager.instance.Send(gamePacket);
        UIManager.Show<PopupMatching>();
    }

    public void OnMatchResult(S2CMatchStartNotification response)
    {
        GameManager.instance.playerData = response.PlayerData;
        GameManager.instance.opponentData = response.OpponentData;
        GameManager.instance.initialGameState = response.InitialGameState;
        SceneManager.LoadSceneAsync("Game");
    }

    public void SetLogin(bool isSuccess)
    {
        gameStartButton.SetActive(isSuccess);
        loginButton.SetActive(!isSuccess);
    }
}