namespace TelegramNavigation
{
    /// <summary>
    /// Represents a command extracted from a message, including its type, arguments, and the original message.
    /// </summary>
    public class Command
    {
        /// <summary>
        /// Gets or sets the type of the command.
        /// </summary>
        public required string Type { get; set; }
        /// <summary>
        /// Gets or sets the command arguments.
        /// </summary>
        public required string[] Args { get; set; }
        /// <summary>
        /// Gets or sets the command message.
        /// </summary>
        public required Message Message { get; set; }

        /// <summary>
        /// Trying to recognize command data in message text started with "/"
        /// </summary>
        /// <param name="message"></param>
        /// <param name="command"></param>
        /// <returns>true if command recognized otherwise false</returns>
        public static bool TryParse(Message message, out Command? command)
        {
            if (message?.Text?[0] == '/')
            {
                var splitted = message.Text.Split();
                command = new Command()
                {
                    Type = splitted[0][1..].ToLower(),
                    Args = splitted[1..],
                    Message = message
                };
                return true;
            }
            command = null;
            return false;
        }
    }
}
