namespace ExampleBot.Components.Forms.TicTacToe
{
    internal class StartForm : BaseForm
    {

        private long _creatorId;
        private readonly List<User> _joinedUsers = new();
        private readonly List<string> _inlineHooks = new();

        private InlineKeyboardButton? _joinButton;
        private InlineKeyboardButton? _closeButton;
        private InlineKeyboardButton? _confirmButton;

        public InlineKeyboardButton JoinButton
        {
            get
            {
                if(_joinButton == null)
                {
                    var btn = InlineMiddleware.CreateButton($"Join game ({_joinedUsers.Count}/2)", JoinGame);
                    _inlineHooks.Add(btn.HookId);
                    _joinButton = btn.Button;
                }
                return _joinButton;
            }
        }

        public InlineKeyboardButton CloseButton
        {
            get
            {
                if (_closeButton == null)
                {
                    var btn = InlineMiddleware.CreateButton($"Close", CloseForm);
                    _inlineHooks.Add(btn.HookId);
                    _closeButton = btn.Button;
                }
                return _closeButton;
            }
        }
        public InlineKeyboardButton ConfirmButton
        {
            get
            {
                if (_confirmButton == null)
                {
                    var btn = InlineMiddleware.CreateButton($"Confirm", SendConfirmation);
                    _inlineHooks.Add(btn.HookId);
                    _confirmButton = btn.Button;
                }
                return _confirmButton;
            }
        }

        public override async Task SendForm(ITelegramBotClient botClient, long chatId, long userId, int? messageThreadId = null)
        {
            _creatorId = userId;
            await botClient.SendMessage(chatId,
                "*\"Tic\\-Tac\\-Toe\"* game",
                messageThreadId: messageThreadId,
                replyMarkup: GetMarkup(),
                parseMode: ParseMode.MarkdownV2);
        }

        private async Task JoinGame(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            if (_joinedUsers.Count == 2 || _joinedUsers.Any(u=>u.Id == from.Id))
                return;
            _joinedUsers.Add(from);
            JoinButton.Text = string.Concat(string.Join(" ", JoinButton.Text.Split()[0..2]), $" ({_joinedUsers.Count}/2)");
            await botClient.EditMessageReplyMarkup(message.Chat.Id, message.MessageId, GetMarkup());
        }
        private async Task SendConfirmation(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            if (_creatorId != from.Id)
                return;
            await SendData(route, botClient, message, from);
        }

        protected override async Task SendData(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            await CloseForm(route, botClient, message, from);
            _= new GameForm(_joinedUsers[0], _joinedUsers[1]).SendForm(botClient, message.Chat.Id, _creatorId, message.MessageThreadId);
        }

        private InlineKeyboardMarkup GetMarkup()
        {
            var markup = new InlineKeyboardMarkup();
            markup.AddButton(JoinButton);
            markup.AddNewRow(CloseButton);
            if (_joinedUsers.Count == 2)
                markup.AddNewRow(ConfirmButton);
            return markup;
        }
        private async Task CloseForm(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            if (_creatorId != from.Id)
                return;
            _inlineHooks.ForEach(InlineMiddleware.UnregisterHook);
            _inlineHooks.Clear();
            await botClient.DeleteMessage(message.Chat.Id, message.MessageId);
        }
    }
}
