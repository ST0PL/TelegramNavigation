/// <summary>
/// A delegate for processing telegram inline queries
/// </summary>
/// <param name="route">Inline query route.</param>
/// <param name="botClient">Telegram bot client.</param>
/// <param name="message">Inline query message.</param>
/// <param name="from">Inline query sender.</param>
public delegate Task InlineQueryHook(Route route, ITelegramBotClient botClient, Message message, User from);
