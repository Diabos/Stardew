using System;
using StardewValley;

public class Program
{
    private static string FlattenException(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        int depth = 0;
        Exception? cur = ex;
        while (cur != null)
        {
            sb.Append("[").Append(depth).Append("] ").Append(cur.GetType().FullName).Append(": ").AppendLine(cur.Message);
            sb.AppendLine(cur.StackTrace ?? "<no stack>");
            cur = cur.InnerException;
            depth++;
            if (cur != null)
            {
                sb.AppendLine("--- INNER ---");
            }
        }
        return sb.ToString();
    }

    public static void Main(string[] args)
    {
        try
        {
            // FNA defaults to SDL3, but the browser build links SDL2 web libs.
            Environment.SetEnvironmentVariable("FNA_PLATFORM_BACKEND", "SDL2");

            Console.WriteLine("Instantiating GameRunner...");
            GameRunner gameRunner = new GameRunner();
            GameRunner.instance = gameRunner;
            Console.WriteLine("Running GameRunner...");
            gameRunner.Run();
        }
        catch (Exception ex)
        {
            string details = FlattenException(ex);
            Console.Error.WriteLine("Fatal startup exception: " + details);
            throw new Exception("Fatal startup exception details:\n" + details, ex);
        }
    }
}
