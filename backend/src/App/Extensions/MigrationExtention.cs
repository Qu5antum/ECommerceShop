using Microsoft.EntityFrameworkCore;
using App.Database;

namespace App.Exceptions;


public static class MigraionExtention
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.Migrate();
    }
}