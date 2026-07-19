using MazeHunter.Game.Application;

namespace MazeHunter.Game;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (_, args) =>
            MessageBox.Show(args.Exception.Message, "Neon Labyrinth", MessageBoxButtons.OK, MessageBoxIcon.Error);

        using var game = new GameForm();
        System.Windows.Forms.Application.Run(game);
    }
}
