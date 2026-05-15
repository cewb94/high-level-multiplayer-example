using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Lobby : Control
{
    [Export] public LineEdit NameInput;
    [Export] public LineEdit IPInput;
    [Export] public Button HostButton;
    [Export] public Button JoinButton;
    [Export] public RichTextLabel EventLog;
    [Export] public LineEdit PlayerInputBox;
    [Export] public Button SyncInputButton;
    [Export] public Label OpponentInput;
    // public static Lobby Instance { get; private set; }

    // These signals can be connected to by a UI lobby scene or the game scene.
    [Signal]
    public delegate void PlayerConnectedEventHandler(int peerId, Dictionary<string, string> playerInfo);
    [Signal]
    public delegate void PlayerDisconnectedEventHandler(int peerId);
    [Signal]
    public delegate void ServerDisconnectedEventHandler();

    private const int Port = 7000;
    private const string DefaultServerIP = "127.0.0.1"; // IPv4 localhost
    private const int MaxConnections = 20;

    // This will contain player info for every player,
    // with the keys being each player's unique IDs.
    private Dictionary<long, Dictionary<string, string>> _players = new Dictionary<long, Dictionary<string, string>>();

    // This is the local player info. This should be modified locally
    // before the connection is made. It will be passed to every other peer.
    // For example, the value of "name" can be set to something the player
    // entered in a UI scene.
    private Dictionary<string, string> _playerInfo = new Dictionary<string, string>()
    {
        { "Name", "PlayerName" },
    };

    private int _playersLoaded = 0;

    public void SetPlayerName(string name)
    {
        _playerInfo["Name"] = name;
    }

    public override void _Ready()
    {
        // GUI node binding
        if (NameInput == null) NameInput = GetNodeOrNull<LineEdit>("VBoxContainer/NameInput");
        if (IPInput == null) IPInput = GetNodeOrNull<LineEdit>("VBoxContainer/IPInput");
        if (HostButton == null) HostButton = GetNodeOrNull<Button>("VBoxContainer/HBoxContainer/HostButton");
        if (JoinButton == null) JoinButton = GetNodeOrNull<Button>("VBoxContainer/HBoxContainer/JoinButton");
        if (EventLog == null) EventLog = GetNodeOrNull<RichTextLabel>("VBoxContainer/EventLog");
        if (PlayerInputBox == null) PlayerInputBox = GetNodeOrNull<LineEdit>("VBoxContainer/PlayerInputBox");
        if (SyncInputButton == null) SyncInputButton = GetNodeOrNull<Button>("VBoxContainer/SyncInputButton");
        if (OpponentInput == null) OpponentInput = GetNodeOrNull<Label>("VBoxContainer/OpponentInput");

        if (PlayerInputBox != null)
        {
            PlayerInputBox.TextChanged += OnPlayerInputTextChanged;
        }

        if (SyncInputButton != null)
        {
            SyncInputButton.Pressed += OnSyncInputButtonPressed;
        }

        if (HostButton != null)
        {
            HostButton.Pressed += OnHostButtonPressed;
        }

        if (JoinButton != null)
        {
            JoinButton.Pressed += OnJoinButtonPressed;
        }

        // Instance = this;
        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectOk;
        Multiplayer.ConnectionFailed += OnConnectionFail;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    public Error JoinGame(string address = "")
    {
        if (string.IsNullOrEmpty(address))
        {
            address = DefaultServerIP;
        }

        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateClient(address, Port);

        if (error != Error.Ok)
        {
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        return Error.Ok;
    }

    public Error CreateGame()
    {
        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(Port, MaxConnections);

        if (error != Error.Ok)
        {
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        _players[1] = _playerInfo;
        EmitSignal(SignalName.PlayerConnected, 1, _playerInfo);
        return Error.Ok;
    }

    private void RemoveMultiplayerPeer()
    {
        Multiplayer.MultiplayerPeer = null;
        _players.Clear();
    }

    // When the server decides to start the game from a UI scene,
    // do Rpc(Lobby.MethodName.LoadGame, filePath);
    [Rpc(CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void LoadGame(string gameScenePath)
    {
        GetTree().ChangeSceneToFile(gameScenePath);
    }

    // Every peer will call this when they have loaded the game scene.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void PlayerLoaded()
    {
        if (Multiplayer.IsServer())
        {
            _playersLoaded += 1;
            if (_playersLoaded == _players.Count)
            {
                // GetNode<Game>("/Game").StartGame();
                _playersLoaded = 0;
            }
        }
    }

    // When a peer connects, send them my player info.
    // This allows transfer of all desired data for each player, not only the unique ID.
    private void OnPlayerConnected(long id)
    {
        RpcId(id, MethodName.RegisterPlayer, _playerInfo);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RegisterPlayer(Dictionary<string, string> newPlayerInfo)
    {
        int newPlayerId = Multiplayer.GetRemoteSenderId();
        _players[newPlayerId] = newPlayerInfo;
        EmitSignal(SignalName.PlayerConnected, newPlayerId, newPlayerInfo);
    }

    private void OnPlayerDisconnected(long id)
    {
        _players.Remove(id);
        EmitSignal(SignalName.PlayerDisconnected, id);
    }

    private void OnConnectOk()
    {
        int peerId = Multiplayer.GetUniqueId();
        _players[peerId] = _playerInfo;
        EmitSignal(SignalName.PlayerConnected, peerId, _playerInfo);
    }

    private void OnConnectionFail()
    {
        Multiplayer.MultiplayerPeer = null;
    }

    private void OnServerDisconnected()
    {
        Multiplayer.MultiplayerPeer = null;
        _players.Clear();
        EmitSignal(SignalName.ServerDisconnected);
    }

    public void AddLogEvent(string message)
    {
        if (EventLog != null)
        {
            EventLog.AppendText(message + "\n");
        }
    }

    private void OnHostButtonPressed()
    {
        if (NameInput != null) SetPlayerName(NameInput.Text);
        CreateGame();
        string msg = $"Hosting Game as {(NameInput != null ? NameInput.Text : "Unknown")}";
        GD.Print(msg);
        AddLogEvent(msg);
    }

    private void OnJoinButtonPressed()
    {
        if (NameInput != null) SetPlayerName(NameInput.Text);
        string address = IPInput != null ? IPInput.Text : "";
        JoinGame(address);
        string msg = $"Joining Game at {address} as {(NameInput != null ? NameInput.Text : "Unknown")}";
        GD.Print(msg);
        AddLogEvent(msg);
    }

    private void OnPlayerInputTextChanged(string newText)
    {
        GD.Print($"[Local] Typed: {newText}");
        if (Multiplayer.MultiplayerPeer != null)
        {
            Rpc(MethodName.SyncOpponentInput, newText);
        }
    }

    private void OnSyncInputButtonPressed()
    {
        string textToSend = PlayerInputBox != null ? PlayerInputBox.Text : "";
        GD.Print($"[Local] Button Sync: {textToSend}");
        if (Multiplayer.MultiplayerPeer != null)
        {
            Rpc(MethodName.SyncOpponentInput, textToSend);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SyncOpponentInput(string text)
    {
        GD.Print($"[RPC Receive] SyncOpponentInput: {text}");
        if (OpponentInput != null)
        {
            OpponentInput.Text = text;
        }
    }
}
