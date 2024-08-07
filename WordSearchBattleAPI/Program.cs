using Microsoft.AspNetCore.HttpOverrides;
using WordSearchBattleAPI.Database;
using WordSearchBattleAPI.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WordSearchBattleAPI.Helper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WordSearchBattleAPI.Managers;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add services to the container.
builder.Services.AddAuthentication(x =>
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

// Add services to the container.
builder.Services.AddAuthorization();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<GameContext>();
builder.Services.AddDbContext<PlayerContext>();

builder.Services.AddScoped<IClaimReader, ClaimReaderService>();
builder.Services.AddScoped<IRoomCodeGenerator, RoomCodeGeneratorService>();

builder.Services.AddSingleton<GameServerManager>();



var app = builder.Build();

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
