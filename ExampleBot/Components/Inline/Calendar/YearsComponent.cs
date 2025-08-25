namespace ExampleBot.Components.Inline.Calendar
{
    [InlineComponent(name: "years")]
    internal class YearsComponent : IInlineQueryComponent
    {
        private readonly Dictionary<string, InlineQueryHook> _routes;

        public YearsComponent()
        {
            _routes = new()
            {
                ["/"] = NavigateTo,
            };
        }
        public async Task HandleQueryAsync(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
            => await _routes[queryRoute.Path].Invoke(queryRoute, botClient, message, from);

        public async Task<Message?> InitializeAsync(Route queryRoute, ITelegramBotClient botClient, long chatId, int? messageThreadId = null)
        {
            var year = int.Parse(queryRoute.Args["year"]);
            var (rows, columns) = (int.Parse(queryRoute.Args["rows"]), int.Parse(queryRoute.Args["columns"]));
            return await botClient.SendMessage(chatId,
                "_Select year_",
                messageThreadId: messageThreadId,
                replyMarkup: GetMarkup(year, rows, columns),
                parseMode: ParseMode.MarkdownV2);
        }

        public async Task NavigateTo(Route queryRoute, ITelegramBotClient botClient, Message message, User user)
        {
            var year = int.Parse(queryRoute.Args["year"]);
            var (rows, columns) = (int.Parse(queryRoute.Args["rows"]), int.Parse(queryRoute.Args["columns"]));
            await botClient.EditMessageText(message.Chat.Id, message.Id,
                "_Select year_",
                replyMarkup: GetMarkup(year, rows, columns),
                parseMode: ParseMode.MarkdownV2);
        }

        private static InlineKeyboardMarkup GetMarkup(int startYear, int rows, int columns)
        {
            var markup = new InlineKeyboardMarkup();
            string yearString = string.Empty;
            int offset = 0;

            for(int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    yearString = (startYear + offset++).ToString();
                    markup.AddButton(new InlineKeyboardButton(yearString,
                        new Route("months", "/", new()
                        {
                            ["year"] = yearString,
                            ["month"] = "1",
                            ["rows"] = "2",
                            ["columns"] = "2"
                        }).ToString()));
                }
                markup.AddNewRow();
            }

            var prevYear = startYear - rows * columns;
            var nextYear = startYear + rows * columns;

            markup.AddNewRow(new InlineKeyboardButton("<<", new Route("years", "/",
                GetArgs(prevYear, rows, columns)).ToString()));
            
            markup.AddButton(new InlineKeyboardButton(" ", "none"));

            markup.AddButton(new InlineKeyboardButton(">>", new Route("years", "/",
                GetArgs(nextYear, rows, columns)).ToString()));

            markup.AddNewRow(InlineMiddleware.GetCloseButton("Close"));
            return markup;
        }

        private static Dictionary<string,string> GetArgs(int startYear, int rows, int columns)
            => new Dictionary<string, string>()
            {
                ["year"] = startYear.ToString(),
                ["rows"] = rows.ToString(),
                ["columns"] = columns.ToString(),
                ["meta"] = string.Empty
            };
    }
}
