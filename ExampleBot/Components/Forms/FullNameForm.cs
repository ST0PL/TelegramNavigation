using ExampleBot.Handlers;
using Telegram.Bot.Extensions;

namespace ExampleBot.Components.Forms
{
    internal class FullNameForm : BaseForm
    {
        private readonly List<string> _inlineHooks = new();
        private readonly List<string> _data = new();
        private readonly Stack<MessageHook?> _messageHooks = new();
        private readonly Stack<Message> _messages = new();

        private Message? _previousMessage = null;

        private InlineKeyboardButton? _closeButton;
        private InlineKeyboardButton? _skipButton;
        private InlineKeyboardButton? _backButton;
        private InlineKeyboardButton? _confirmButton;

        private InlineKeyboardButton CloseButton
            => _closeButton ??= AddButton("Close", CloseForm);

        private InlineKeyboardButton BackButton
            => _backButton ??= AddButton("Back", MoveBack);

        private InlineKeyboardButton SkipButton
            => _skipButton ??= AddButton("Skip", SkipPatronymic);

        private InlineKeyboardButton ConfirmButton
            => _confirmButton ??= AddButton("Confirm", SendData);

        public override async Task SendForm(ITelegramBotClient botClient, long chatId, long userId, int? messageThreadId = null)
            => await GetLastName(botClient, new Message() { Chat = new Chat() { Id = chatId } }, new User() { Id = userId });

        public async Task GetLastName(ITelegramBotClient botClient, Message message, User from)
        {
            if(_previousMessage != null)
                await botClient.EditMessageReplyMarkup(_previousMessage.Chat.Id, _previousMessage.Id);
            _previousMessage = await botClient.SendMessage(message.Chat.Id,
                 "Enter last name",
                 messageThreadId: message.MessageThreadId,
                 replyMarkup: new InlineKeyboardMarkup([[CloseButton]]));
            _messages.Push(_previousMessage);
            RegisterHook(botClient, message.Chat.Id, from.Id, GetFirstName);
        }

        public async Task GetFirstName(ITelegramBotClient botClient, Message message, User from)
        {
            await botClient.EditMessageReplyMarkup(_previousMessage.Chat.Id, _previousMessage.Id);
           _previousMessage = await botClient.SendMessage(message.Chat.Id,
                "Enter first name",
                messageThreadId: message.MessageThreadId,
                replyMarkup: new InlineKeyboardMarkup([[BackButton], [CloseButton]]));
            _messages.Push(_previousMessage);
            RegisterHook(botClient, message.Chat.Id, message.From.Id, GetPatronymic);
        }

        public async Task GetPatronymic(ITelegramBotClient botClient, Message message, User from)
        {
            await botClient.EditMessageReplyMarkup(_previousMessage.Chat.Id, _previousMessage.Id);

            _previousMessage = await botClient.SendMessage(message.Chat.Id,
                "Enter patronymic",
                messageThreadId: message.MessageThreadId,
                replyMarkup: new InlineKeyboardMarkup([[BackButton], [SkipButton], [CloseButton]]));
            _messages.Push(_previousMessage);

            RegisterHook(botClient, message.Chat.Id, message.From.Id, SendConfirmationRequest);
        }

        private async Task SendConfirmationRequest(ITelegramBotClient botClient, Message message, User from)
        {
            await botClient.EditMessageReplyMarkup(_previousMessage.Chat.Id, _previousMessage.Id);
            _previousMessage = await botClient.SendMessage(message.Chat.Id,
                "*You are about to submit the following data:*\n\n" +
                $"Last name: {Markdown.Escape(_data[0])}" +
                $"\nFirst name: {Markdown.Escape(_data[1])}" +
                $"\nPatronymic: {Markdown.Escape(_data[2])}",
                messageThreadId: message.MessageThreadId,
                replyMarkup: new InlineKeyboardMarkup([[BackButton],[CloseButton],[ConfirmButton]]),
                parseMode: ParseMode.MarkdownV2);
            _messages.Push(_previousMessage);
            _messageHooks.Push(null);
        }

        protected override async Task SendData(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            await botClient.DeleteMessage(_previousMessage.Chat.Id, _previousMessage.Id);
            await botClient.SendMessage(message.Chat.Id,
                "*You have submitted the following data*\n\n" +
                $"Last name: {Markdown.Escape(_data[0])}" +
                $"\nFirst name: {Markdown.Escape(_data[1])}" +
                $"\nPatronymic: {Markdown.Escape(_data[2])}",
                messageThreadId: message.MessageThreadId,
                parseMode: ParseMode.MarkdownV2);
            DeInitializeForm(message, from);
        }

        private async Task MoveBack(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            _data.Remove(_data[^1]);
            _messageHooks.Pop();
            _messages.Pop();
            var prevMessage = _messages.Peek();
            _previousMessage = await botClient.EditMessageText(message.Chat.Id, message.Id,
                prevMessage.Text,
                replyMarkup: prevMessage.ReplyMarkup,
                parseMode: ParseMode.MarkdownV2);
            RegisterHook(botClient, message.Chat.Id, from.Id, _messageHooks.Pop());
        }

        private async Task SkipPatronymic(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            _data.Add("Not specified");
            UpdateHandler.MessageHandler.UnregisterHook(message.Chat.Id, from.Id);
            await SendConfirmationRequest(botClient, message, from);
        }

        private void DeInitializeForm(Message message, User from)
        {
            UpdateHandler.MessageHandler.UnregisterHook(message.Chat.Id, from.Id);
            _inlineHooks.ForEach(InlineMiddleware.UnregisterHook);
            _inlineHooks.Clear();
            _data.Clear();
            _messageHooks.Clear();
            _messages.Clear();
        }

        private async Task CloseForm(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            DeInitializeForm(message, from);
            await botClient.DeleteMessage(message.Chat.Id, message.Id);
            await botClient.SendMessage(message.Chat.Id, "Data submission canceled.", messageThreadId: message.MessageThreadId);
        }

        private async Task ValidateText(ITelegramBotClient botclient, Message message, MessageHook nextHandler)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await botclient.EditMessageReplyMarkup(_previousMessage.Chat.Id, _previousMessage.Id);
                _previousMessage = await botclient.SendMessage(message.Chat.Id,
                    "Try again",
                    messageThreadId: message.MessageThreadId,
                    replyMarkup: new InlineKeyboardMarkup([[CloseButton]]));
            }
            else
            {
                UpdateHandler.MessageHandler.UnregisterHook(message.Chat.Id, message.From.Id);
                _data.Add(message.Text);
                await nextHandler.Invoke(botclient, message, message.From);
            }
        }

        private void RegisterHook(ITelegramBotClient botClient, long chatId, long userId, MessageHook handler)
        {
            _messageHooks.Push(handler);
            UpdateHandler.MessageHandler.RegisterHook(chatId, userId, async (bot, msg, _) => await ValidateText(bot, msg, handler));
        }

        private InlineKeyboardButton AddButton(string text, InlineQueryHook handler)
        {
            var btn = InlineMiddleware.CreateButton(text, handler);
            _inlineHooks.Add(btn.HookId);
            return btn.Button;
        }
    }
}
