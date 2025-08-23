namespace TelegramNavigation.Routing
{
    /// <summary>
    /// A controller for page navigation
    /// </summary>
    public class PageController
    {
        /// <summary>
        /// Length of the source collection
        /// </summary>
        public int SourceLength { get; set; }
        /// <summary>
        /// Number of elements per page
        /// </summary>
        public int ElementsCount { get; }
        /// <summary>
        /// Offset from the source start
        /// </summary>
        public int Offset { get; private set; }
        /// <summary>
        /// Number of pages
        /// </summary>
        public int PagesCount => (int)Math.Ceiling((double)SourceLength / ElementsCount);
        /// <summary>
        /// Current page
        /// </summary>
        public int CurrentPage => Offset / ElementsCount + 1;
        /// <summary>
        /// Previous page number. <c>-1</c> if no previous page
        /// </summary>
        public int PreviousPage => Offset - ElementsCount >= 0 ? (Offset - ElementsCount) / ElementsCount + 1 : -1;
        /// <summary>
        /// Next page number. <c>-1</c> if no next page
        /// </summary>
        public int NextPage => Offset + ElementsCount < SourceLength ? (Offset + ElementsCount) / ElementsCount + 1 : -1;
        /// <summary>
        /// Title of current page
        /// </summary>
        public string? Title { get; set; }


        /// <summary>
        /// Create a <see cref="PageController"/> instance
        /// </summary>
        /// <param name="sourceLength"></param>
        /// <param name="elementsPerPage"></param>
        /// <param name="data"></param>
        /// <exception cref="ArgumentException"></exception>
        public PageController(int sourceLength, int elementsPerPage, string? data = null)
        {
            if (elementsPerPage <= 0)
                throw new ArgumentException("incorrect elements count");
            SourceLength = sourceLength;
            ElementsCount = elementsPerPage;
            Offset = 0;
            Title = data;
        }
        /// <summary>
        /// Move to next page by shifting the offset
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (NextPage > 0)
            {
                Offset += ElementsCount;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Move to previous page by shifting the offset
        /// </summary>
        /// <returns></returns>
        public bool MoveBack()
        {
            if (PreviousPage > 0)
            {
                Offset -= ElementsCount;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Move to specific page by shifting the offset
        /// </summary>
        /// <returns></returns>
        public bool MoveTo(int page)
        {
            if (page < 1)
                throw new ArgumentException("Page number cannot be less than 1");

            int shift = (page-1) * ElementsCount;
            if (shift > SourceLength)
                return false;
            Offset = shift;
            return true;
        }
    }
}
