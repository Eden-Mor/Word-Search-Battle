using WordSearchBattleAPI.Database;
using WordSearchBattleAPI.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WordSearchBattleAPI.Helper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WordSearchBattleAPI.Managers;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var services = builder.Services;

// Add services to the container.
services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters()
    {
        //ValidIssuer = config["JwtSettings:Issuer"],
        //ValidAudience = config["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DockerSecretHelper.ReadSecret("jwt-key"))),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});

services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("https://edenmor.com")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});


// Add services to the container.
services.AddAuthorization();
services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddHttpContextAccessor();

services.AddDbContext<GameContext>();
services.AddDbContext<PlayerContext>();

services.AddScoped<IClaimReader, ClaimReaderService>();

services.AddSingleton<IRoomCodeGenerator, RoomCodeGeneratorService>();
services.AddSingleton<GameServerManager>();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.TimestampFormat = "[HH:mm:ss] ");

var app = builder.Build();

app.UseCors("AllowSpecificOrigin");
app.UseWebSockets(new WebSocketOptions() { KeepAliveInterval = TimeSpan.FromSeconds(30) });

var gameServerManager = app.Services.GetRequiredService<GameServerManager>();

//app.UseForwardedHeaders(new ForwardedHeadersOptions
//{
//    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
//});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

//app.UseAuthentication();
//app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "API Works");

app.Run();
