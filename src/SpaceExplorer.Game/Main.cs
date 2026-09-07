using Godot;
using SpaceExplorer.Persistence;

namespace SpaceExplorer.Game;

/// <summary>
/// Root node of the game. Runs the exported-build smoke check when launched with <c>-- --smoke</c>
/// and will host the M0a primitive viewer.
/// </summary>
public partial class Main : Node
{
    public override void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Contains("--smoke"))
        {
            RunSmokeCheck();
        }
    }

    private void RunSmokeCheck()
    {
        try
        {
            string godotVersion = (string)Engine.GetVersionInfo()["string"];
            string sqliteVersion = SqliteRuntime.GetLibraryVersion();
            GD.Print($"SMOKE OK godot={godotVersion} dotnet={System.Environment.Version} sqlite={sqliteVersion}");
            GetTree().Quit(0);
        }
        catch (Exception e)
        {
            GD.PrintErr($"SMOKE FAIL {e}");
            GetTree().Quit(1);
        }
    }
}
