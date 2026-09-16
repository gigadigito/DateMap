using System.Text;
using DateMap.Application;
using DateMap.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder=WebApplication.CreateBuilder(args);
var cs=builder.Configuration.GetConnectionString("DateMap")??(builder.Environment.IsEnvironment("Testing")?"Host=unused;Database=unused;Username=unused":throw new InvalidOperationException("ConnectionStrings:DateMap is required. Configure it with user-secrets or environment variables."));
builder.Services.AddDbContext<DateMapDbContext>(o=>o.UseNpgsql(cs,n=>n.UseNetTopologySuite()));
builder.Services.AddScoped<PasswordService>(); builder.Services.AddScoped<ITokenService,JwtTokenService>(); builder.Services.AddHttpContextAccessor();
var jwtKey=builder.Configuration["Jwt:Key"]??(builder.Environment.IsEnvironment("Testing")?"integration-test-key-with-at-least-thirty-two-bytes":throw new InvalidOperationException("Jwt:Key is required. Configure it securely."));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>o.TokenValidationParameters=new(){ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,ValidIssuer=builder.Configuration["Jwt:Issuer"],ValidAudience=builder.Configuration["Jwt:Audience"],IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))});
builder.Services.AddCors(o=>o.AddPolicy("dev",p=>p.WithOrigins("http://localhost:5051","https://localhost:5052","http://localhost:5155","http://localhost:5243").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthorization(); builder.Services.AddControllers(); builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<ApiExceptionHandler>(); builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o=>{o.SwaggerDoc("v1",new(){Title="Date Map API",Version="v1"});o.AddSecurityDefinition("Bearer",new OpenApiSecurityScheme{Type=SecuritySchemeType.Http,Scheme="bearer",BearerFormat="JWT",Description="Paste the JWT token."});});
var app=builder.Build(); app.UseExceptionHandler(); if(app.Environment.IsDevelopment()){app.UseCors("dev");app.UseSwagger();app.UseSwaggerUI();} if(!app.Environment.IsEnvironment("Testing"))app.UseHttpsRedirection();app.UseAuthentication();app.UseAuthorization();app.MapControllers();app.Run();
public partial class Program { }
