namespace TelegramNavigation
{
    /// <summary>
    /// Attribute for naming components
    /// </summary>
    /// <param name="name"></param>
    [AttributeUsage(AttributeTargets.Class)]
    public class InlineComponentAttribute : Attribute
    {
        /// <summary>
        /// Component name for registration
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Create <see cref="InlineComponentAttribute"/>
        /// </summary>
        /// <param name="name"></param>
        public InlineComponentAttribute(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
            Name = name;
        }
    }
}
