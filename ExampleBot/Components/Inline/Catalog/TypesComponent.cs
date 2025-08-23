using ExampleBot.Database;
using Microsoft.EntityFrameworkCore;

namespace ExampleBot.Components.Inline.Catalog
{
    internal class TypesComponent : IInlineQueryComponent, IInlinePageComponent
    {
        private readonly Dictionary<string, InlineQueryHook> _routes;
        public TypesComponent()
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
            var botMessage = await botClient.SendMessage(chatId, "Loading...", messageThreadId: messageThreadId);
            using var dbContext = new MediaContext();
            var types = dbContext.Types;
            var typesCount = types.Count();
            if(typesCount > 0)
            {
                var page = InlineMiddleware.CreatePage(botMessage.Chat.Id, botMessage.MessageId, typesCount, 1, "types");

                return await botClient.EditMessageText(botMessage.Chat.Id, botMessage.Id,
                    "_Select type_",
                    replyMarkup: await GetMarkup(page, types),
                    parseMode: ParseMode.MarkdownV2);
            }
            else
                return await botClient.EditMessageText(botMessage.Chat.Id,
                    botMessage.Id,
                    "_No data found_",
                    parseMode: ParseMode.MarkdownV2);
        }

        public async Task NavigateTo(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
        {
            using var dbContext = new MediaContext();
            var types = dbContext.Types;
            int typesCount = types.Count();

            if(typesCount > 0)
            {
                var page = InlineMiddleware.GetPage(message.Chat.Id, message.MessageId, "types");

                page ??= InlineMiddleware.CreatePage(message.Chat.Id, message.MessageId, typesCount, 1, "types");

                await botClient.EditMessageText(message.Chat.Id, message.Id,
                    "_Select type_",
                    replyMarkup: await GetMarkup(page, types),
                    parseMode: ParseMode.MarkdownV2);
            }
            else
                await botClient.EditMessageText(message.Chat.Id, message.Id,
                    "_No data found_",
                    parseMode: ParseMode.MarkdownV2);
        }

        public async Task MoveBack(Route queryRoute, ITelegramBotClient botClient, Message message, User user)
        {
            using var dbContext = new MediaContext();
            var types = dbContext.Types;
            var page = InlineMiddleware.GetPage(message.Chat.Id, message.MessageId, "types");
            page.MoveBack();
            await botClient.EditMessageReplyMarkup(message.Chat.Id, message.Id,
                replyMarkup: await GetMarkup(page, types));
        }

        public async Task MoveNext(Route queryRoute, ITelegramBotClient botClient, Message message, User user)
        {
            using var dbContext = new MediaContext();
            var types = dbContext.Types;
            var page = InlineMiddleware.GetPage(message.Chat.Id, message.MessageId, "types");
            page.MoveNext();
            await botClient.EditMessageReplyMarkup(message.Chat.Id, message.Id,
                replyMarkup: await GetMarkup(page, types));
        }
        private static async Task <InlineKeyboardMarkup> GetMarkup(PageController page, IQueryable<MediaType> types)
        {
            var markup = new InlineKeyboardMarkup();
            await types.Skip(page.Offset).Take(page.ElementsCount).ForEachAsync(t=>markup.AddButton(t.Text,
                new Route("media", "/", new() { ["id"] = t.Id.ToString() }).ToString()));

            markup.AddNewRow(new InlineKeyboardButton()
            {
                Text = page.PreviousPage != -1 ? "<<" : " ",
                CallbackData = page.PreviousPage != -1 ? new Route("types", "/moveBack", new() { ["meta"] = string.Empty }).ToString() : "none"
            });
            markup.AddButton(new InlineKeyboardButton($"{page.CurrentPage}/{page.PagesCount}", "none"));
            markup.AddButton(new InlineKeyboardButton()
            {
                Text = page.NextPage != -1 ? ">>" : " ",
                CallbackData = page.NextPage != -1 ? new Route("types", "/moveNext", new() { ["meta"] = string.Empty }).ToString() : "none"
            });
            markup.AddNewRow(InlineMiddleware.GetCloseButton("Close"));
            return markup;
        }
    }
}
