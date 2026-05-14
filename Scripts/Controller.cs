using System;
using Godot;

[GlobalClass]
public partial class Controller : Node
{
    [Export] public Lobby Lobby;

    [Export] private String PlayerName = "Player N";
    [Export] private String HostIp = "127.0.0.1";
    // [Export] private ItemList PlayerList;
    [Export] public String OpponentName;
    [Export] public int OpponentId;

    public override void _Ready()
    {
        if (Lobby == null)
        {
            Lobby = GetNodeOrNull<Lobby>("../Lobby");
        }

        if (Lobby != null)
        {
            Lobby.PlayerConnected += OnPlayerConnected;
            Lobby.PlayerDisconnected += OnPlayerDisconnected;
            Lobby.ServerDisconnected += OnServerDisconnected;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("host"))
        {
            OnHostPressed();
        }
        else if (@event.IsActionPressed("join"))
        {
            OnJoinPressed();
        }
        else if (@event.IsActionPressed("connect"))
        {
            // OnPlayerConnected(null, null);
        }
        else if (@event.IsActionPressed("disconnect"))
        {
            // OnPlayerDisconnected(null);
        }
    }

    private void OnJoinPressed()
    {
        if (Lobby != null)
        {
            Lobby.SetPlayerName(PlayerName);
            Lobby.JoinGame(HostIp);
            GD.Print($"Joining Game at {HostIp} as {PlayerName}");
        }
    }

    private void OnPlayerConnected(int peerId, Godot.Collections.Dictionary<string, string> playerInfo)
    {
        string playerName = playerInfo.ContainsKey("Name")
            ? playerInfo["Name"]
            : $"Player {peerId}";

        GD.Print($"Player ID: {peerId} | Name: {playerName}");

        if (peerId != Multiplayer.GetUniqueId())
        {
            OpponentName = playerName;
            OpponentId = peerId;
            GD.Print($"Opponent set -> ID: {OpponentId} | Name: {OpponentName}");
        }
    }

    private void OnPlayerDisconnected(int peerId)
    {
        GD.Print($"Player disconnected: {peerId}");
        if (peerId == OpponentId)
        {
            OpponentName = "";
            OpponentId = 0;
            GD.Print("Opponent disconnected.");
        }
    }

    private void OnServerDisconnected()
    {
        // PlayerList.Clear();

        GD.Print("Disconnected from server.");
    }

    private void OnHostPressed()
    {
        if (Lobby != null)
        {
            Lobby.SetPlayerName(PlayerName);
            Lobby.CreateGame();
            GD.Print($"Hosting Game as {PlayerName}");
        }
    }
}