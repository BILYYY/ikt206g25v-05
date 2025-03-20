using Example.Models;
using Microsoft.EntityFrameworkCore;

namespace Example.Data
{
    public static class ApplicationDbInitializer
    {
        public static void Initialize(ApplicationDbContext db)
        {
            // ✅ Ensure database is migrated before seeding
            Console.WriteLine("Applying migrations...");
            db.Database.Migrate();  // Ensures migrations are applied before inserting data
            
            try
            {
                Console.WriteLine("Checking if 'Authors' table exists...");

                if (!db.Authors.Any())  // Safe check after ensuring table exists
                {
                    Console.WriteLine("Seeding database...");
                    var authors = new[]
                    {
                        new Author("Author 1", "Author 1", new DateTime(1981, 1, 1)),
                        new Author("Author 2", "Author 2", new DateTime(1982, 2, 2)),
                        new Author("Author 3", "Author 3", new DateTime(1983, 3, 3))
                    };

                    db.Authors.AddRange(authors);
                    db.SaveChanges();
                    Console.WriteLine("✅ Database seeding complete.");
                }
                else
                {
                    Console.WriteLine("✅ 'Authors' table already populated. Skipping seeding.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during database initialization: {ex.Message}");
            }
        }
    }
}