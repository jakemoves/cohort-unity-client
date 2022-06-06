// TODO: Namespace

public abstract class CHCommand
{
    public const string CommandHeader = "COMMAND";
    public const string SubCommandSeparator = "::";
    public const char SelectorSeperator = '.';
    public const char CommandSeparator = ';';

    public abstract string Command { get; }

    public override string ToString()
    {
        return GetCommandStart();
    }

    protected string GetCommandStart() => string.Join(SubCommandSeparator, CommandHeader, Command);

    //public static bool TryParse<TCommand>(string commandString, out TCommand command) where TCommand : CHCommand
    //{
    //    if (typeof(TCommand) == typeof(DecisionCommand))
    //}
}

public class DecisionCommand : CHCommand
{
    public const string DecisionCommandHeader = "Decision";

    public override string Command => DecisionCommandHeader;

    public string NodeId { get; private set; }
    public string Group { get; private set; }

    public bool Decision { get; set; } = default;

    public DecisionCommand(string nodeId, string group)
    {
        NodeId = nodeId;
        Group = group;

        if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(nodeId))
            throw new System.InvalidOperationException($"'{nameof(nodeId)}' and '{nameof(group)}' can not be null or empty.");
    }

    public static bool TryParse(string commandString, out DecisionCommand command)
    {
        command = null;

        commandString = commandString.Trim(CommandSeparator, ' ');
        var commandStart = $"{CommandHeader}{SubCommandSeparator}{DecisionCommandHeader}";
        if (string.IsNullOrEmpty(commandString) || !commandString.StartsWith(commandStart) || !commandString.Contains("="))
            return false;

        var operation = commandString.Substring(commandStart.Length);

        var selectors = operation.Substring(0, operation.IndexOf('='))
            .Trim()
            .Split(new char[] { SelectorSeperator }, System.StringSplitOptions.RemoveEmptyEntries);

        if (selectors.Length > 2)
            return false;

        var valueStr = operation.Substring(operation.IndexOf('=') + 1);

        bool value;
        if (!bool.TryParse(valueStr, out value))
            value = valueStr.ToLower().Contains("true");

        command = new DecisionCommand(selectors[0].Trim(), selectors[1].Trim())
        {
            Decision = value
        };
        return true;
    }

    public override string ToString()
    {
        return $"{GetCommandStart()}{SelectorSeperator}{NodeId}{SelectorSeperator}{Group}={Decision}";
    }

    public static implicit operator string(DecisionCommand command) => command.ToString(); 
}