using System.Collections.Concurrent;

namespace TelegramNavigation
{
    /// <summary>
    /// Processing incoming Telegram messages, routing them to the appropriate handlers of user commands, buttons or hooks.
    /// </summary>
    public class MessageHandler : IMessageHandler
    {
        /// <summary>
        /// A dictionarty of commamd handlers, where the key is command name
        /// </summary>
        private readonly ConcurrentDictionary<string, ICommandHandler> CommandHandlers = new();

        /// <summary>
        /// A dictionary of reply button handlers, where the key is button text
        /// </summary>
        private readonly ConcurrentDictionary<string, IReplyButtonHandler> ReplyButtonHandlers = new();

        /// <summary>
        /// A dictionary of user hooks, where the key is a tuple of chat and user IDs.
        /// </summary>
        private readonly ConcurrentDictionary<(long, long), MessageHook> UserHooks = new();

        /// <inheritdoc/>
        public virtual async Task HandleAsync(ITelegramBotClient botClient, Message message)
        {
            if (UserHooks.TryGetValue((message.Chat.Id, message.From.Id), out var hookHandler))
                await hookHandler.Invoke(botClient, message, message.From);
            else
            {
                if (Command.TryParse(message, out var command) && CommandHandlers.TryGetValue(command.Type, out var commandHandler))
                    await commandHandler.HandleAsync(botClient, command);
                else if (ReplyButtonHandlers.TryGetValue(message.Text, out var replyHandler))
                    await replyHandler.HandleAsync(botClient, message);
            }
        }

        /// <inheritdoc/>
        public virtual void RegisterHook(long chatId, long userId, MessageHook hook)
            => UserHooks[(chatId, userId)] = hook;

        /// <inheritdoc/>
        public virtual void UnregisterHook(long chatId, long userId)
            => UserHooks.TryRemove((chatId, userId), out _);

        /// <inheritdoc/>
        public virtual void RegisterCommand(string type, ICommandHandler handler)
            => CommandHandlers[type.ToLower()] = handler;

        /// <inheritdoc/>
        public virtual void UnregisterCommand(string type)
            => CommandHandlers.TryRemove(type.ToLower(), out _);

        /// <inheritdoc/>
        public virtual void RegisterReplyButton(string text, IReplyButtonHandler handler)
            => ReplyButtonHandlers[text] = handler;

        /// <inheritdoc/>
        public virtual void UnregisterReplyButton(string text)
            => ReplyButtonHandlers.TryRemove(text, out _);

        /// <inheritdoc/>
        public virtual IEnumerable<string> GetReplyButtons()
            => ReplyButtonHandlers.Keys;
    }
}
