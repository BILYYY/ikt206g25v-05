using Example.Models;

namespace Example.Data
{
    public static class ApplicationDbInitializer
    {
        public static void Initialize(ApplicationDbContext db)
        {
            // Only seed the database if no authors exist
            if (!db.Authors.Any())
            {
                var authors = new[]
                {
                    new Author("Author 1", "Author 1", new DateTime(1981, 1, 1)),
                    new Author("Author 2", "Author 2", new DateTime(1982, 2, 2)),
                    new Author("Author 3", "Author 3", new DateTime(1983, 3, 3))
                };

                db.Authors.AddRange(authors);
                db.SaveChanges();
            }
        }
    }
}