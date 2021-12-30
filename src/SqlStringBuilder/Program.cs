using System;

using SqlStringBuilder.Compilers;

namespace SqlStringBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = SqlStringBuilder
                .CreateSelectStatement()
                .From("users")
                .Select("users.id", "users.name as username", "users.age")
                .Where("users.id", ">", 1)
                .Where("users.age", "=", 28)
                .OrWhere("users.id", "<", 1)
                .WhereNotNull("users.name");

            var compiler = new Compiler();
            var result = compiler.Compile(builder);

            Console.WriteLine($"Raw SQL: {result.RawSql}");
            Console.WriteLine($"Prepared SQL: {result.Sql}");
        }
    }
}
