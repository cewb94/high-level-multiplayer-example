using Godot;
using System;

[GlobalClass]
public partial class Game : Node
{
    [Export] public Lobby Lobby;

    public override void _Ready()
    {
        if (Lobby == null)
        {
            Lobby = GetNodeOrNull<Lobby>("../Lobby");
        }

        // Preconfigure game.

        Lobby?.RpcId(1, Lobby.MethodName.PlayerLoaded); // Tell the server that this peer has loaded.
    }

    // Called only on the server.
    public void StartGame()
    {
        // All peers are ready to receive RPCs in this scene.
    }
}
