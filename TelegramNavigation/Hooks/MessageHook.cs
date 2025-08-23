/// <summary>
/// A delegate for processing telegram messages
/// </summary>
/// <param name="botClient">Telegram bot client</param>
/// <param name="message">Telegram message</param>
/// <param name="from">Telegram message sender</param>
public delegate Task MessageHook(ITelegramBotClient botClient, Message message, User from);