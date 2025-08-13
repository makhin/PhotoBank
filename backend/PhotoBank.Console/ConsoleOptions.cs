namespace PhotoBank.Console
{
    public record ConsoleOptions(bool RegisterPersons = true, int? StorageId = null)
    {
        public static ConsoleOptions Parse(string[] args)
        {
            var options = new ConsoleOptions();
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--storage":
                    case "-s":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var id))
                        {
                            options = options with { StorageId = id };
                            i++;
                        }
                        break;
                    case "--no-register":
                        options = options with { RegisterPersons = false };
                        break;
                }
            }
            return options;
        }
    }
}

