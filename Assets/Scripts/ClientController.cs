﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChessClient;
using System.Threading;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;


public class ClientController : MonoBehaviour
{
    [SerializeField] string host = "http://localhost:44334/api/";


    [HideInInspector]
    public string PlayerColor { get; private set; } = "";

    Client Client;

    [HideInInspector]
    public GameInfo GameInfo { get; private set; }

    [HideInInspector]
    public GameState GameState { get; private set; } 

    [HideInInspector]
    public PlayerInfo PlayerInfo { get; private set; }


    SynchronizationContext mainSyncContext;
    public static ClientController Instance;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        mainSyncContext = SynchronizationContext.Current;// Context of main thread
    }

    void Start()
    {
        Client = new Client(host);

        //int id = UnityEngine.Random.Range(1, 100000);
        string deviceId = SystemInfo.deviceUniqueIdentifier;// "test123";// id.ToString();//SystemInfo.deviceUniqueIdentifier;// TEST
        Player player = new Player() { GUID = deviceId, Name = "testname1" };

        AuthenticatePlayer(player, (result) =>
        {
            PlayerInfo = result;
            LobbyController.Instance.SetPlayerName(result.playerName);
        });
    }

    public async void GetPlayer(string color, Action<PlayerInfo> callback)
    {
        await Client.GetPlayer(GameInfo.gameID, color, (result, content) =>
        {
            mainSyncContext.Post(s =>// runs the following code on the main thread
            {
                if (result == Client.Result.Ok)
                    Debug.Log("move has been applied");
                else
                    return;

                Debug.Log(JsonConvert.SerializeObject(content));
                callback?.Invoke(content);
            }, null);
        });
    }

    async void AuthenticatePlayer(Player player, Action<PlayerInfo> callback)
    {
        await Client.GetPlayer(player, (result, content) =>
        {
            mainSyncContext.Post(s =>// runs the following code on the main thread
            {
                if (result == Client.Result.Created)
                    Debug.Log("new account has been created");
                else if (result == Client.Result.Ok)
                    Debug.Log("authentication succeeded");
                else
                    return;

                Debug.Log(JsonConvert.SerializeObject(content));
                callback?.Invoke(content);
            }, null);
        });
    }

    public async void StartNewGame(RequestedGame game, Action<GameInfo> callback)
    {
        await Client.FindGame(game, (result, content) =>
        {
            mainSyncContext.Post(s =>// runs the following code on the main thread
            {
                if (result == Client.Result.Created)
                    Debug.Log("new game has been created");
                else if (result == Client.Result.Ok)
                    Debug.Log("joined an existing game");
                else
                    return;

                PlayerColor = game.playerColor;
                GameInfo = content;
                Debug.Log(JsonConvert.SerializeObject(content));
                callback?.Invoke(content);
            }, null);
        });
    }

    public async void UpdateGameState(Action<GameState> callback)
    {
        await Client.GetGame(GameInfo.gameID, (result, content) =>
        {
            mainSyncContext.Post(s =>// runs the following code on the main thread
            {
                if (result == Client.Result.Ok)
                    Debug.Log("game has been updated");
                else
                    return;

                GameState = content;  
                Debug.Log(JsonConvert.SerializeObject(content));
                callback?.Invoke(content);
            }, null);
        });
    }

    public async void SendMove(string fenMove, Action<GameState> callback)
    {
        MoveInfo moveInfo = new MoveInfo() { gameID = GameInfo.gameID, fenMove = fenMove, playerID = PlayerInfo.playerID };

        await Client.SendMove(moveInfo, (result, content) =>
        {
            mainSyncContext.Post(s =>// runs the following code on the main thread
            {
                if (result == Client.Result.Created)
                    Console.WriteLine("move has been applied");
                else
                    return;

                GameState = content;
                Debug.Log(JsonConvert.SerializeObject(content));
                callback?.Invoke(content);
            }, null);
        });
    }
}
