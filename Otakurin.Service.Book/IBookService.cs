namespace Otakurin.Service.Book;

public interface IBookService
{
    Task<List<APIBookBasic>> SearchBookByTitle(string title);
    
    Task<APIBook?> GetBookById(string id);
}