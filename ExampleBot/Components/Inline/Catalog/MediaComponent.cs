using ExampleBot.Database;
using System.Text;
using Telegram.Bot.Extensions;

namespace ExampleBot.Components.Inline.Catalog
{
    internal class MediaComponent : IInlineQueryComponent
    {
        private readonly Dictionary<string, InlineQueryHook> _routes;
        public MediaComponent()
        {
            _routes = new()
            {
                ["/"] = NavigateTo,
                ["/moveBack"] = MoveBack,
                ["/moveNext"] = MoveNext,
            };
        }

        public async Task HandleQueryAsync(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
            => await _routes[queryRoute.Path].Invoke(queryRoute, botClient, message, from);

        public async Task<Message?> InitializeAsync(Route queryRoute, ITelegramBotClient botClient, long chatId, int? messageThreadId = null)
        {
            using var dbContext = new MediaContext();
            var type = dbContext.Types.Find(int.Parse(queryRoute.Args["id"]));

            var media = dbContext.Media.Where(m=>m.MediaTypeId == type.Id);

            var mediaCount = media.Count();

            var botMessage = await botClient.SendMessage(chatId, "Loading...", messageThreadId: messageThreadId);

            if (mediaCount > 0)
            {

                var page = InlineMiddleware.CreatePage(botMessage.Chat.Id, botMessage.MessageId, mediaCount, 1, "media");

                return await botClient.EditMessageText(botMessage.Chat.Id, botMessage.Id,
                    GetText(media.Skip(page.Offset).Take(page.ElementsCount).ToList(), page.CurrentPage, page.NextPage),
                    replyMarkup: await GetMarkup(type, page, botMessage.Chat.Id, botMessage.Id),
                    parseMode: ParseMode.MarkdownV2);
            }
            else
                return await botClient.EditMessageText(botMessage.Chat.Id,
                    botMessage.Id,
                    "_No data found_",
                    replyMarkup: await GetMarkup(type, null, botMessage.Chat.Id, botMessage.Id),
                    parseMode: ParseMode.MarkdownV2);
        }

        public async Task NavigateTo(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
        {
            using var dbContext = new MediaContext();
            var type = dbContext.Types.Find(int.Parse(queryRoute.Args["id"]));

            var media = dbContext.Media.Where(m => m.MediaTypeId == type.Id);

            var typesCount = media.Count();

            if (typesCount > 0)
            {

                var page = InlineMiddleware.CreatePage(message.Chat.Id, message.Id, typesCount, 1, "media");
                
                page ??= InlineMiddleware.GetPage(message.Chat.Id, message.Id);

                await botClient.EditMessageText(message.Chat.Id, message.Id,
                    GetText(media.Skip(page.Offset).Take(page.ElementsCount).ToList(), page.CurrentPage, page.PagesCount),
                    replyMarkup: await GetMarkup(type, page, message.Chat.Id, message.Id),
                    parseMode: ParseMode.MarkdownV2);
            }
            else
               await botClient.EditMessageText(message.Chat.Id, message.MessageId,
                    "_No data found_",
                    replyMarkup: await GetMarkup(type, null, message.Chat.Id, message.Id),
                    parseMode: ParseMode.MarkdownV2);


        }

        public async Task MoveBack(Route queryRoute, ITelegramBotClient botClient, Message message, User user)
        {

            var page = InlineMiddleware.GetPage(message.Chat.Id, message.MessageId, "media");
            page.MoveBack();

            var (media, type) = GetData(page, int.Parse(queryRoute.Args["id"]));

            await botClient.EditMessageText(message.Chat.Id, message.Id,
                GetText(media, page.CurrentPage, page.PagesCount),
                replyMarkup: await GetMarkup(type, page, message.Chat.Id, message.Id),
                parseMode: ParseMode.MarkdownV2);
        }

        public async Task MoveNext(Route queryRoute, ITelegramBotClient botClient, Message message, User user)
        {
            var page = InlineMiddleware.GetPage(message.Chat.Id, message.MessageId, "media");
            page.MoveNext();

            var (media, type) = GetData(page, int.Parse(queryRoute.Args["id"]));

            await botClient.EditMessageText(message.Chat.Id, message.Id,
                GetText(media, page.CurrentPage, page.PagesCount),
                replyMarkup: await GetMarkup(type, page, message.Chat.Id, message.Id),
                parseMode: ParseMode.MarkdownV2);
        }

        private static (List<Media>, MediaType) GetData(PageController page, int typeId)
        {
            using var dbContext = new MediaContext();
            var media = dbContext.Media;
            var type = dbContext.Types.Find(typeId);
            return (media.Skip(page.Offset).Take(page.ElementsCount).ToList(), type);
        }

        private static string GetText(List<Media> media, int currentPage, int pagesCount)
        {
            StringBuilder sb = new StringBuilder();
            if (media.Count > 0)
            {
                sb.Append($"*Page `{currentPage}` of `{pagesCount}`*\n");
                foreach(var item in media)
                {
                    sb.AppendLine(
                        $"\n*{Markdown.Escape(item.Title)}*" +
                        $"\nAuthor: {Markdown.Escape(item.Author)}" +
                        $"\nLink: {Markdown.Escape(item.Link)}");
                }
            }
            else
                sb.Append("_No results found_");
            return sb.ToString();
        }

        private static async Task<InlineKeyboardMarkup> GetMarkup(MediaType type, PageController? page, long chatId, int messageId)
        {
            var markup = new InlineKeyboardMarkup();

            markup.AddNewRow(new InlineKeyboardButton()
            {
                Text = page?.PreviousPage > -1 ? "<<" : " ",
                CallbackData = page?.PreviousPage > -1 ? new Route("media", "/moveBack", new() { ["meta"] = string.Empty, ["id"] = type.Id.ToString() }).ToString() : "none"
            });
            markup.AddButton(new InlineKeyboardButton(type.Text, "none"));
            markup.AddButton(new InlineKeyboardButton()
            {
                Text = page?.NextPage > -1 ? ">>" : " ",
                CallbackData = page?.NextPage > -1 ? new Route("media", "/moveNext", new() { ["meta"] = string.Empty,  ["id"] = type.Id.ToString() }).ToString() : "none"
            });
            InlineMiddleware.GetBackAndCloseButtons("Back", "Close", chatId, messageId)
                .ForEach(row=>row.ForEach(btn=>markup.AddNewRow(btn)));

            return markup;
        }
    }
}
