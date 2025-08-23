namespace ExampleBot.Components.Forms
{
    internal class StateButtonsForm : BaseForm
    {
        private bool _toggle1 = false;
        private bool _toggle2 = false;
        private readonly bool[] _group = [true, false, false];

        private readonly List<string> _inlineHooks = new();

        private InlineKeyboardButton? _closeButton;

        private InlineKeyboardButton? _toggleButton1;
        private InlineKeyboardButton? _toggleButton2;
        private InlineKeyboardButton? _radioButton1;
        private InlineKeyboardButton? _radioButton2;
        private InlineKeyboardButton? _radioButton3;

        private InlineKeyboardButton CloseButton
            => _closeButton ??= AddButton("Close", CloseForm);

        private InlineKeyboardButton ToggleButton1
            => _toggleButton1 ??= AddStateButton("Toggle Button 1", async (_, _, _, _) => _toggle1 = !_toggle1);
        private InlineKeyboardButton ToggleButton2
            => _toggleButton2 ??= AddStateButton("Toggle Button 2", async (_, _, _, _) => _toggle2 = !_toggle2);
        private InlineKeyboardButton RadioButton1
            => _radioButton1 ??= AddStateButton("Radio Button 1", ChangeRadioGroup, new() { ["index"] = "0" });
        private InlineKeyboardButton RadioButton2
            => _radioButton2 ??= AddStateButton("Radio Button 2", ChangeRadioGroup, new() { ["index"] = "1" });
        private InlineKeyboardButton RadioButton3
            => _radioButton3 ??= AddStateButton("Radio Button 3", ChangeRadioGroup, new() { ["index"] = "2" });

        public override async Task SendForm(ITelegramBotClient botClient, long chatId, long userId, int? messageThreadId = null)
            => await botClient.SendMessage(chatId,
                "_State buttons_",
                messageThreadId: messageThreadId,
                replyMarkup: GetMarkup(),
                parseMode: ParseMode.MarkdownV2);

        protected override Task SendData(Route route, ITelegramBotClient botClient, Message message, User from)
            => Task.CompletedTask;

        private InlineKeyboardMarkup GetMarkup()
        {
            var markup = new InlineKeyboardMarkup();

            ToggleButton1.Text = ToggleFlag(ToggleButton1.Text, "✔️", _toggle1);
            ToggleButton2.Text = ToggleFlag(ToggleButton2.Text, "✔️", _toggle2);
            RadioButton1.Text = ToggleRadioFlag(RadioButton1.Text, "🔘", "⚪️", _group[0]);
            RadioButton2.Text = ToggleRadioFlag(RadioButton2.Text, "🔘", "⚪️", _group[1]);
            RadioButton3.Text = ToggleRadioFlag(RadioButton3.Text, "🔘", "⚪️", _group[2]);

            markup.AddButtons(ToggleButton1, ToggleButton2);
            markup.AddNewRow(RadioButton1, RadioButton2, RadioButton3);
            markup.AddNewRow(CloseButton);
            return markup;
        }

        private static string ToggleFlag(string text, string flag, bool isChecked)
        {
            if (text.Split($"{flag} ") is { Length: > 1 } splitted)
                return isChecked ? text : splitted[1];

            return isChecked ? string.Concat($"{flag} ", text) : text;
        }
        
        private static string ToggleRadioFlag(string text, string checkedFlag, string uncheckedFlag, bool isChecked)
        {
            if (isChecked)
            {
                if (text.Split($"{uncheckedFlag} ") is { Length: > 1 } split1)
                    return string.Concat($"{checkedFlag} ", split1[1]);
                else if (text.Split($"{checkedFlag} ") is { Length: > 1 })
                    return text;
                return string.Concat($"{checkedFlag} ", text);
            }

            if (text.Split($"{uncheckedFlag} ") is { Length: > 1 })
                return text;
            else if (text.Split($"{checkedFlag} ") is { Length: > 1 } split4)
                return string.Concat($"{uncheckedFlag} ", split4[1]);

            return string.Concat($"{uncheckedFlag} ", text);
        }

        private async Task CloseForm(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            _inlineHooks.ForEach(InlineMiddleware.UnregisterHook);
            _inlineHooks.Clear();
            await botClient.DeleteMessage(message.Chat.Id, message.Id);
        }
        private Task ChangeRadioGroup(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            int index = int.Parse(route.Args["index"]);
            if (!_group[index])
            {
                _group[index] = true;

                for (int i = 0; i < _group.Length; i++)
                    if(index != i)
                        _group[i] = false;
            }

            return Task.CompletedTask;
        }
        private InlineKeyboardButton AddButton(string text, InlineQueryHook handler, Dictionary<string,string>? args = null)
        {
            var btn = InlineMiddleware.CreateButton(text, handler, args);
            _inlineHooks.Add(btn.HookId);
            return btn.Button;
        }

        private InlineKeyboardButton AddStateButton(string text, InlineQueryHook handler, Dictionary<string, string>? args = null)
            => AddButton(text, async (route, bot, msg, from) =>
            {
                await handler.Invoke(route, bot, msg, from);
                await bot.EditMessageReplyMarkup(msg.Chat.Id, msg.Id, GetMarkup());
            }, args);
    }
}
