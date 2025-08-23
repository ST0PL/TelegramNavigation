using System.Text;

namespace TelegramNavigation.Routing
{
    /// <summary>
    /// Represents a route for inline navigation, including type, path, and arguments.
    /// </summary>
    public class Route
    {
        /// <summary>
        /// Route type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Route path
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Route arguments
        /// </summary>
        public Dictionary<string, string>? Args { get; set; }

        /// <summary>Create a new <see cref="Route"/> instance.</summary>
        public Route(string type, string? path, Dictionary<string, string>? args)
        {
            Type = type;
            Path = path;
            Args = args;
        }

        /// <summary>
        /// Converting an object to a string representation.
        /// </summary>
        /// <returns>the route object in string representation.</returns>
        public override string ToString()
            => $"t={Type};p={Path}?{(Args != null ? string.Join('&', Args.Select(kv => $"{kv.Key}={kv.Value}")) : null)}";
        /// <summary>
        /// Casts route instance to string representation
        /// </summary>
        /// <param name="route"></param>
        public static explicit operator string(Route route)
            => route.ToString();

        //public string ToBase64String()
        //    => Convert.ToBase64String(Encoding.UTF8.GetBytes(ToString()));

        //public static string FromBase64String(string base64text)
        //    => Encoding.UTF8.GetString(Convert.FromBase64String(base64text));

        /// <summary>
        /// Converts a text presenetation of route to the <see cref="Route"/> instance
        /// </summary>
        /// <param name="text">Text presenetation of route</param>
        /// <returns></returns>
        public static Route Parse(string text)
        {
            Dictionary<string, string> args = ParseArgs(text, ";");
            Dictionary<string, string>? pathArgs = null;
            string[]? splittedPath = null;
            if (args.TryGetValue("p", out var fullPath))
            {
                splittedPath = fullPath.Split("?");
                pathArgs = (splittedPath is { Length: > 1 } && !string.IsNullOrWhiteSpace(splittedPath[1])) ? ParseArgs(splittedPath[1], "&") : null;
            }

            return new Route(
                args["t"], // type
                splittedPath is [var path, ..] ? path : null, // path
                pathArgs); // args
        }
        private static Dictionary<string, string> ParseArgs(string args, string separator)
        {
            Dictionary<string, string> dictionary = args.Split(separator).Select(p =>
            {
                string[] splittedPair = p.Split("=");
                return KeyValuePair.Create(splittedPair[0], string.Join("=",splittedPair.Skip(1)));
            }).ToDictionary();
            return dictionary;
        }
    }
}
