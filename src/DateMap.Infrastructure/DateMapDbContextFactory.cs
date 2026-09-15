using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace DateMap.Infrastructure;
public sealed class DateMapDbContextFactory:IDesignTimeDbContextFactory<DateMapDbContext>
{ public DateMapDbContext CreateDbContext(string[] args){var cs=Environment.GetEnvironmentVariable("DATEMAP_CONNECTION_STRING")??"Host=localhost;Port=5432;Database=datemap_dev;Username=postgres";var o=new DbContextOptionsBuilder<DateMapDbContext>().UseNpgsql(cs,n=>n.UseNetTopologySuite()).Options;return new(o);} }
