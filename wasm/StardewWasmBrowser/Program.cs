using System;
using StardewValley;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Instantiating GameRunner...");
        GameRunner gameRunner = new GameRunner();
        GameRunner.instance = gameRunner;
        Console.WriteLine("Running GameRunner...");
        gameRunner.Run();
    }
}
