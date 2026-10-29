using System;

namespace RedoxExtensions.AgentCli;

public static class Program
{
    public static int Main(string[] args) => new Cli(Console.Out, Console.Error).Run(args);
}
