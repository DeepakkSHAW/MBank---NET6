using MBank.API.Authenticator;
using MBank.API.Models;
using MBank.API.Services;
using MBank.Data;
using MBank.Lib;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Principal;

//--Swagger related configuration--//
var securityScheme = new OpenApiSecurityScheme()
{
    Name = "Authorization",
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "JSON Web Token based security",
};

var securityReq = new OpenApiSecurityRequirement()
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }
};

var contact = new OpenApiContact()
{
    Name = "Deepak shaw",
    Email = "deepak.shaw@gmail.com",
    Url = new Uri("https://www.linkedin.com/in/shawdeepak/")
};

var license = new OpenApiLicense()
{
    Name = "Free License",
    Url = new Uri("https://www.linkedin.com/in/shawdeepak/")
};

var info = new OpenApiInfo()
{
    Version = "v1.1",
    Title = " MBank - PoC with .Net6 and Azure Table Storage as backed together with JWT Authentication & Swagger.",
    Description = "Implementing Swagger JWT Authentication & Azure Table Storage with Minimal API .Net 6",
    TermsOfService = new Uri("https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-6"),
    Contact = contact,
    License = license
};

var jsonBankBSB = JsonConvert.SerializeObject(Banks.MBankBSB, new StringEnumConverter());
var  _strMBankBSB = JsonConvert.DeserializeObject<string>(jsonBankBSB);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(o =>
{
    //o.SwaggerDoc("v1", new() { Title = "Bank API", Version = "v 1.1" });
    o.SwaggerDoc("v1", info);
    o.AddSecurityDefinition("Bearer", securityScheme);
    o.AddSecurityRequirement(securityReq);
});

//--Authentication JWT configuration--//
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateAudience = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateLifetime = false, // In any other application other then demo this needs to be true,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();


builder.Configuration.AddJsonFile("errorcodes.json", false, true);
builder.Services.AddSingleton<DummyCustomerRepository>();
builder.Services.AddSingleton<ApplicationInfo>();
builder.Services.AddSingleton<IAccountServices, AccountServices>();

// Add services to the container.

var app = builder.Build();
app.MapSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

#region Authenticate Users

app.MapPost("/user/login", [AllowAnonymous] async (TheUserDto user) =>
{
    var validation = await Authenticator.ValidateUser(user);
    if (validation == false) return Results.Unauthorized();
    else
    {
        var secureKey = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);

        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var securityKey = new SymmetricSecurityKey(secureKey);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

        var jwtTokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim("UserID", "007"),
                new Claim(JwtRegisteredClaimNames.Sub, user.username),
                new Claim(JwtRegisteredClaimNames.Email, user.username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.Now.AddMinutes(5),
            Audience = audience,
            Issuer = issuer,
            SigningCredentials = credentials
        };

        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = jwtTokenHandler.WriteToken(token);
        return Results.Ok(jwtToken);
    }
}).WithTags("Admin login & Generate Token");
#endregion

#region Application API

    app.MapGet("/api/applicationInfo", [AllowAnonymous] ([FromServices] ApplicationInfo appInfo) => {
    //var settings = builder.Configuration.GetSection("ApplicationInfo:Application").Value;
    return Results.Ok(appInfo.GetApplication());
}).WithTags("Application Info");

app.MapGet("/api/applicationVersion", [AllowAnonymous] ([FromServices] ApplicationInfo appInfo) => {
    return Results.Ok(appInfo.GetApplicationVersion());
}).WithTags("Application Info");

app.MapGet("/api/about", [AllowAnonymous] ([FromServices] ApplicationInfo appInfo) => {
    return Results.Ok(appInfo.GetDesignerInfo());
}).WithTags("Application Info");
#endregion

#region Customers API
//app.MapGet("/", () => "Hello there");
app.MapGet("/api/customers", [Authorize] ([FromServices] DummyCustomerRepository custRepo) => {
    return Results.Ok(custRepo.GetAll());
});

app.MapGet("/api/customers/{id}", [Authorize]([FromServices] DummyCustomerRepository custRepo, Guid id) => {
    var customer = custRepo.GetById(id);
    return customer is not null ? Results.Ok(customer) : Results.NotFound();
});

app.MapPost("/api/customers", [Authorize] ([FromServices] DummyCustomerRepository custRepo, DummyCustomer customer) => { 
    custRepo.Create(customer);
    return Results.Created($"/api/customers/{customer.Id}", customer);
});

app.MapPut("/api/customers/{id}", [Authorize] ([FromServices] DummyCustomerRepository custRepo, Guid id, DummyCustomer updateCustomer) => { 
     var customer = custRepo.GetById(id);
     if (customer is null) 
        return Results.NotFound();
     custRepo.Update(updateCustomer); 
     return Results.Ok(updateCustomer);
});

app.MapDelete("/api/customers/{id}", [Authorize] ([FromServices] DummyCustomerRepository custRepo, Guid id) => {
    custRepo.Delete(id);
    return Results.Ok(); 
});

#endregion

#region Bank API
app.MapGet("/api/Bank", [AllowAnonymous] async ([FromServices] IAccountServices account) => {

    return Results.Ok(await account.GetAllAccountsAsync(_strMBankBSB));
}).WithTags("MBank");

app.MapGet("/api/Bank/{id}", [AllowAnonymous] async ([FromServices] IAccountServices account, Guid id) => {

    return Results.Ok(await account.GetAccountAsync(_strMBankBSB, id));
}).WithTags("MBank");

app.MapPost("/api/Bank", [AllowAnonymous] async ([FromServices] IAccountServices account, AccountEntity acc) => {
    AccountEntity oNewAccount = new AccountEntity(_strMBankBSB, Guid.NewGuid())
    {
        AccountHolderName = "Deepak Shaw",
        AccountNumber = "1234567890",
        PhoneNumber = "123-456-7890",
        Email = "deepak.shaw@gmail.com"
    };

    var vResult = await account.AddAccountAsync(oNewAccount);
    if(vResult)
        return Results.Created($"/api/Bank/{oNewAccount.RowKey}", oNewAccount);
    else
    return Results.BadRequest();
}).WithTags("MBank");

app.MapPut("/api/Bank/{id}", [AllowAnonymous] async ([FromServices] IAccountServices account, AccountEntity acc, Guid id) =>
{
    acc.RowKey = id.ToString();
    var vResult = await account.UpdateAccountAsync(acc);
    return vResult ? Results.Ok(vResult) : Results.BadRequest();
}).WithTags("MBank");

app.MapDelete("/api/Bank/{id}", [AllowAnonymous] async ([FromServices] IAccountServices account, Guid id) =>
{
   var vResult = await account.DeleteAccountAsync(_strMBankBSB, id);
    return vResult ? Results.Ok(vResult) : Results.BadRequest();
}).WithTags("MBank");

#endregion
// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.Run();


