using System.Globalization;

namespace ExampleBot.Components.Inline.Calendar
{
    [InlineComponent(name: "months")]
    internal class MonthsComponent : IInlineQueryComponent
    {
        private readonly string[] _months = DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames;
        private readonly Dictionary<string, InlineQueryHook> _routes;

        public MonthsComponent()
        {
            _routes = new()
            {
                ["/"] = NavigateTo
            };
        }
        public async Task HandleQueryAsync(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
            => await _routes[queryRoute.Path].Invoke(queryRoute, botClient, message, from);

        public Task<Message?> InitializeAsync(Route queryRoute, ITelegramBotClient botClient, long chatId, int? messageThreadId = null)
            => Task.FromResult<Message?>(null);

        public async Task NavigateTo(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
        {
            var year = int.Parse(queryRoute.Args["year"]);
            var month = int.Parse(queryRoute.Args["month"]);
            var (rows, columns) = (int.Parse(queryRoute.Args["rows"]), int.Parse(queryRoute.Args["columns"]));
            await botClient.EditMessageText(message.Chat.Id, message.Id,
                "_Select month_",
                replyMarkup: GetMarkup(year, month, rows, columns, message.Chat.Id, message.Id),
                parseMode: ParseMode.MarkdownV2);
        }

        private InlineKeyboardMarkup GetMarkup(int year, int startMonth, int rows, int columns, long chatId, int messageId)
        {
            var markup = new InlineKeyboardMarkup();
            int offset = 0;
            int month;
            for (int i = 0; i < rows; i++)
            {
                for(int j = 0; j < columns; j++)
                {
                    month = startMonth + offset++;
                    markup.AddButton(_months[month-1],
                        new Route("days", "/", new() { ["year"] = year.ToString(), ["month"] = month.ToString() }).ToString());
                }
                markup.AddNewRow();
            }
            int prevMonth = startMonth - rows * columns;
            int nextMonth = startMonth + rows * columns;

            markup.AddNewRow(new InlineKeyboardButton()
            {
                Text = prevMonth>= 1 ? "<<" : " ",
                CallbackData = prevMonth >= 1 ? new Route("months", "/", GetArgs(year, prevMonth, rows, columns)).ToString() : "none"
            });
            markup.AddButton(InlineMiddleware.GetBackButton(year.ToString(), chatId, messageId));
            markup.AddButton(new InlineKeyboardButton()
            {
                Text = nextMonth <= 12 ? ">>" : " ",
                CallbackData = nextMonth <= 12  ? new Route("months", "/", GetArgs(year, nextMonth, rows, columns)).ToString() : "none"
            });
            return markup;
        }

        private static Dictionary<string, string> GetArgs(int year, int startMonth, int rows, int columns)
            => new()
            {
                ["year"] = year.ToString(),
                ["month"] = startMonth.ToString(),
                ["rows"] = rows.ToString(),
                ["columns"] = columns.ToString(),
                ["meta"] = string.Empty
            };
    }
}
