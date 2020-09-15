using System;

/// <summary>
/// Commands declaration only
/// </summary>
public static partial class Commands
{
    /// <summary>
    /// Run command by name
    /// </summary>
    /// <param name="name">command name</param>
    public static partial void Run(string name);

    /// <summary>
    /// Run command by name and id
    /// </summary>
    /// <param name="name">command name</param>
    /// <param name="id">command parameter</param>
    public static partial void Run(string name, int id);
}
