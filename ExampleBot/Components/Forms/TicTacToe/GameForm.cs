namespace ExampleBot.Components.Forms.TicTacToe
{
    internal class GameForm : BaseForm
    {
        private readonly List<string> _inlineHooks = new();
        private readonly List<List<InlineMiddleware.InlineButton>> _buttons = new();
        private readonly int[][] _board = new int[3][];
        private int _movesLeft;
        private bool _isX = true;
        private readonly Stack<int> _stack = new();
        private InlineKeyboardButton? _closeButton;
        private InlineKeyboardButton? _replayButton;

        public InlineKeyboardButton CloseButton
        {
            get
            {
                if(_closeButton == null)
                {
                    var btn = InlineMiddleware.CreateButton("Close", CloseForm);
                    _inlineHooks.Add(btn.HookId);
                    _closeButton = btn.Button;
                }
                return _closeButton;

            }
        }

        public InlineKeyboardButton ReplayButton
        {
            get
            {
                if (_replayButton == null)
                {
                    var btn = InlineMiddleware.CreateButton("Restart", RestartGame);
                    _inlineHooks.Add(btn.HookId);
                    _replayButton = btn.Button;
                }
                return _replayButton;

            }
        }

        private readonly User _xUser;
        private readonly User _oUser;
        private long _creatorId;

        public GameForm(User xUser, User oUser)
        {
            _xUser = xUser;
            _oUser = oUser;
        }

        public override async Task SendForm(ITelegramBotClient botClient, long chatId, long userId, int? messageThreadId = null)
        {
            _creatorId = userId;
            InitGame();
            var (type, user) = GetPlayer();
            await botClient.SendMessage(chatId,
                $"*{type} \\([{GetName(user)}](tg://user?id={user.Id})\\) turn*",
                messageThreadId: messageThreadId,
                replyMarkup: GetMarkup(),
                parseMode: ParseMode.MarkdownV2);
        }

        protected override Task SendData(Route route, ITelegramBotClient botClient, Message message, User from)
            => Task.CompletedTask;

        private async Task UpdateMessage(Route route, ITelegramBotClient botClient, Message message, User from)
        {

            if(_isX && from.Id == _xUser.Id || !_isX && from.Id == _oUser.Id)
            {
                _movesLeft -= 1;
                int x = int.Parse(route.Args["x"]);
                int y = int.Parse(route.Args["y"]);
                _board[x][y] = _isX ? 1 : 0;
                UpdateButton(x, y);
                int winner = GetWinner();
                string text = string.Empty;
                var markup = GetMarkup();
                if (winner != -1 || _movesLeft < 1)
                {
                    var currentPlayer = GetPlayer();
                    text = _movesLeft < 1 ? "Draw\\!" : $"*{currentPlayer.Item1} \\([{GetName(currentPlayer.Item2)}](tg://user?id={currentPlayer.Item2.Id})\\)*\\ wins\\!";
                    DisableButtons();
                    markup.AddNewRow(ReplayButton);
                }
                else
                {
                    _isX = !_isX;
                    var nextPlayer = GetPlayer();
                    text = $"*{nextPlayer.Item1} \\([{GetName(nextPlayer.Item2)}](tg://user?id={nextPlayer.Item2.Id})\\) turn*";
                }
                await botClient.EditMessageText(message.Chat.Id, message.MessageId,
                    text,
                    replyMarkup: markup,
                    parseMode: ParseMode.MarkdownV2);
            }
        }
        private void DeinitializeForm()
        {
            _inlineHooks.ForEach(InlineMiddleware.UnregisterHook);
            _inlineHooks.Clear();
            _buttons.Clear();
        }
        private void DisableButtons()
            => _buttons.ForEach(row => row.ForEach(b => b.Button.CallbackData = "none"));

        private void DeinitGame()
        {
            _stack.Clear();
            _buttons.ForEach(row => row.ForEach(b => _inlineHooks.Remove(b.HookId)));
            _buttons.Clear();
        }
        private async Task RestartGame(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            if (_creatorId != from.Id)
                return;

            DeinitGame();
            InitGame();
            var (type, user) = GetPlayer();
            await botClient.EditMessageText(message.Chat.Id,
                message.MessageId,
                $"*{type} \\([{GetName(user)}](tg://user?id={user.Id})\\) turn*",
                replyMarkup: GetMarkup(),
                parseMode: ParseMode.MarkdownV2);
        }
        private async Task CloseForm(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            if (_creatorId != from.Id)
                return;
            DeinitializeForm();
            await botClient.DeleteMessage(message.Chat.Id, message.MessageId);
            await botClient.SendMessage(message.Chat.Id,
                "_Session was closed\\._",
                messageThreadId: message.MessageThreadId,
                parseMode: ParseMode.MarkdownV2);
        }

        private (string, User) GetPlayer()
            => _isX ? ("X", _xUser) : ("O", _oUser);

        private int GetWinner()
        {
            for(int i = 0; i < _board.Length; i++)
            {
                for (int j = 0; j < _board.Length; j++)
                {
                    if (!TryPush(_board[i][j]))
                        break;
                }
                if (_stack.Count == 3)
                    return _stack.Pop();
                _stack.Clear();
            }

            for (int i = 0; i < _board.Length; i++)
            {
                for (int j = 0; j < _board.Length; j++)
                {
                    if (!TryPush(_board[j][i]))
                        break;
                }
                if (_stack.Count == 3)
                    return _stack.Pop();
                _stack.Clear();
            }

            for (int i = 0; i < _board.Length; i++)
            {
                if (!TryPush(_board[i][i]))
                    break;
            }
            if (_stack.Count == 3)
                return _stack.Pop();
            _stack.Clear();

            for (int i = 0; i < _board.Length; i++)
            {
                if (!TryPush(_board[i][_board.Length - i - 1]))
                    break;
            }
            if (_stack.Count == 3)
                return _stack.Pop();
            _stack.Clear();

            return -1;
        }

        private bool TryPush(int value)
        {
            if (value == -1)
                return true;

            if(_stack.Count < 1 || _stack.Peek() == value)
            {
                _stack.Push(value);
                return true;
            }

            return false;
        }

        private InlineKeyboardMarkup GetMarkup()
        {
            var markup = new InlineKeyboardMarkup();

            foreach(var row in _buttons)
            {
                foreach(var button in row)
                    markup.AddButton(button.Button);
                markup.AddNewRow();
            }
            markup.AddNewRow(CloseButton);
            return markup;
        }

        private void UpdateButton(int x, int y)
        {
            var button = _buttons[x][y];
            if (_board[x][y] != -1)
            {
                button.Button.Text = _isX ? "X" : "0";
                button.Button.CallbackData = "none";
                _inlineHooks.Remove(button.HookId);
                InlineMiddleware.UnregisterHook(button.HookId);
            }
        }
        private void InitGame()
        {
            _movesLeft = _board.Length*_board.Length;
            for(int i = 0; i < _board.Length; i++)
            {
                _board[i] = new int[3];
                _buttons.Add(new ());
                for (int j = 0; j < _board.Length; j++)
                {
                    _board[i][j] = -1;
                    var btn = InlineMiddleware.CreateButton(" ", 
                        UpdateMessage, new() { ["x"] = i.ToString(), ["y"] = j.ToString() });
                    _inlineHooks.Add(btn.HookId);
                    _buttons[^1].Add(btn);
                }
            }
        }
        private static string GetName(User user)
            => $"{user.FirstName}{(!string.IsNullOrWhiteSpace(user.LastName) ? $" {user.LastName}" : "")}";
    }
}
