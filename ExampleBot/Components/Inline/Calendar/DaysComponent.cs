using System.Globalization;

namespace ExampleBot.Components.Inline.Calendar
{
    internal class DaysComponent : IInlineQueryComponent
    {
        private readonly string[] _daysOfWeek = DateTimeFormatInfo.InvariantInfo.AbbreviatedDayNames;
        private readonly Dictionary<string, InlineQueryHook?> _routes;

        public DaysComponent()
        {
            _routes  = new()
            {
                ["/"] = NavigateTo,
                ["/send"] = SendDate,
            };
        }

        public async Task HandleQueryAsync(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
        {
            if (_routes[queryRoute.Path] is { } handler)
                await handler.Invoke(queryRoute, botClient, message, from);
        }

        public Task<Message?> InitializeAsync(Route queryRoute, ITelegramBotClient botClient, long chatId, int? messageThreadId = null)
            => Task.FromResult<Message?>(null);

        public async Task NavigateTo(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
        {
            var (year, month) = (int.Parse(queryRoute.Args["year"]), int.Parse(queryRoute.Args["month"]));
            await botClient.EditMessageText(message.Chat.Id, message.Id,
                "_Select day_",
                replyMarkup: GetMarkup(year, month, message.Chat.Id, message.Id),
                parseMode: ParseMode.MarkdownV2);
        }

        public async Task SendDate(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
        {
            await InlineMiddleware.NavigateTo(new Route("standard", "/close", null), botClient, message, from);
            await botClient.SendMessage(message.Chat.Id,
                $"You have selected the following date: {queryRoute.Args["day"]}.{queryRoute.Args["month"]}.{queryRoute.Args["year"]}",
                messageThreadId: message.MessageThreadId);
        }

        private InlineKeyboardMarkup GetMarkup(int year, int month, long chatId, int messageId)
        {
            var markup = new InlineKeyboardMarkup();

            DateTime datetime = new DateTime(year, month, 1);
            int days = DateTime.DaysInMonth(year, month);
            int offsetBefore = ((int)datetime.DayOfWeek) % 7;
            int offsetAfter = (6 - ((int)datetime.DayOfWeek + days - 1) % 7) % 7;

            markup.AddButton(InlineMiddleware.GetBackButton($"{DateTimeFormatInfo.InvariantInfo.GetMonthName(datetime.Month)} {year}", chatId, messageId));

            markup.AddNewRow();

            foreach (var dow in _daysOfWeek)
                markup.AddButton(new InlineKeyboardButton(dow, "none"));

            markup.AddNewRow();

            for (int i = 0; i < offsetBefore; i++)
                markup.AddButton(new InlineKeyboardButton(" ", "none"));
             
            for (int i = 1; i <= days; i++)
            {
                datetime = new DateTime(year, month, i);
                markup.AddButton(new InlineKeyboardButton()
                {
                    Text = DateTime.Now.Date == datetime.Date ? $"({i})" : i.ToString(),
                    CallbackData = new Route("days", "/send",
                        new() { ["year"] = year.ToString(), ["month"] = month.ToString("00"), ["day"] = i.ToString("00") }).ToString(),
                });
                if (markup.InlineKeyboard.Last().Count() % 7 == 0)
                    markup.AddNewRow();
            }

            for (int i = 0; i < offsetAfter; i++)
                markup.AddButton(new InlineKeyboardButton(" ", "none"));

            markup.AddNewRow(new InlineKeyboardButton()
            {
                Text = month > 1 ? "<<" : " ",
                CallbackData = month > 1 ? new Route("days", "/", GetArgs(year, month - 1)).ToString() : "none",
            });

            markup.AddButton(new InlineKeyboardButton(" ", "none"));

            markup.AddButton(new InlineKeyboardButton()
            {
                Text = month < 12 ? ">>" : " ",
                CallbackData = month < 12 ? new Route("days", "/", GetArgs(year, month + 1)).ToString() : "none",
            });
            return markup;
        }
        private static Dictionary<string, string> GetArgs(int year, int month)
            => new()
            {
                ["year"] = year.ToString(),
                ["month"] = month.ToString(),
                ["meta"] = string.Empty
            };
    }
}
