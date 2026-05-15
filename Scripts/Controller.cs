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



    private void LogEvent(string msg)
    {
        GD.Print(msg);
        Lobby?.AddLogEvent(msg);
    }

    private void OnPlayerConnected(int peerId, Godot.Collections.Dictionary<string, string> playerInfo)
    {
        string playerName = playerInfo.ContainsKey("Name")
            ? playerInfo["Name"]
            : $"Player {peerId}";

        LogEvent($"Player ID: {peerId} | Name: {playerName}");

        if (peerId != Multiplayer.GetUniqueId())
        {
            OpponentName = playerName;
            OpponentId = peerId;
            LogEvent($"Opponent set -> ID: {OpponentId} | Name: {OpponentName}");
        }
    }

    private void OnPlayerDisconnected(int peerId)
    {
        LogEvent($"Player disconnected: {peerId}");
        if (peerId == OpponentId)
        {
            OpponentName = "";
            OpponentId = 0;
            LogEvent("Opponent disconnected.");
        }
    }

    private void OnServerDisconnected()
    {
        LogEvent("Disconnected from server.");
    }


}