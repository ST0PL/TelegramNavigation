namespace ExampleBot.Database;

internal class Media
{
    public int Id { get; set; }
    public int MediaTypeId { get; set; }
    public MediaType? Type { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Link { get; set; }

    public Media WithType(int mediaTypeId)
    {
        MediaTypeId = mediaTypeId;
        return this;
    }
    public Media WithTitle(string title)
    {
        Title = title;
        return this;
    }
    public Media WithAuthor(string author)
    {
        Author = author;
        return this;
    }
    public Media WithLink(string link)
    {
        Link = link;
        return this;
    }
}
