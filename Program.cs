using Microsoft.Extensions.FileProviders;
using System.Configuration;
using System.Text;
using tusdotnet;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Models.Expiration;
using tusdotnet.Stores;
using WebFileLoader.Entities;
using WebFileLoader.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 52428800;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 52428800;
});
// Add services to the container.
var services = builder.Services;
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var physicalProvider = new PhysicalFileProvider(builder.Configuration.GetValue<string>("StoredFilesPath"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
        .WithExposedHeaders("Upload-Offset", "Location", "Upload-Length", "Tus-Version", "Tus-Resumable", "Tus-Max-Size", "Tus-Extension", "Upload-Metadata", "Upload-Defer-Length", "Upload-Concat", "Location", "Upload-Offset", "Upload-Length");
        });
});
// To list physical files in the temporary files folder, use:
//var physicalProvider = new PhysicalFileProvider(Path.GetTempPath());

services.AddSingleton<IFileProvider>(physicalProvider);
services.AddSingleton(CreateTusConfiguration);
services.AddSingleton<IAWSConfig, AWSConfig>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(builder =>
{
    builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithExposedHeaders("Upload-Offset", "Location", "Upload-Length", "Tus-Version", "Tus-Resumable", "Tus-Max-Size", "Tus-Extension", "Upload-Metadata", "Upload-Defer-Length", "Upload-Concat", "Location", "Upload-Offset", "Upload-Length"); ;
});

app.Use(async (httpContext, next) =>
{
    // Specify timeout, in this case 60 seconds.
    var requestTimeout = TimeSpan.FromSeconds(60);

    // Add timeout to the current request cancellation token. 
    // If the client does a clean disconnect the cancellation token will also be flagged as cancelled.
    using var timoutCts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted);

    // Make sure to cancel the cancellation token after the timeout. 
    // Once this timeout has been reached, tusdotnet will cancel all pending reads 
    // from the client and save the parts of the file has been received so far.
    timoutCts.CancelAfter(requestTimeout);

    // Replace the request cancellation token with our token that supports timeouts.
    httpContext.RequestAborted = timoutCts.Token;

    // Continue the execution chain.
    await next();
});

app.UseHttpsRedirection();

app.UseTus(httpContext => Task.FromResult(httpContext.RequestServices.GetService<DefaultTusConfiguration>()));
//app.MapGet("/files/{fileId}", DownloadFileEndpoint.HandleRoute);


app.UseAuthorization();

app.MapControllers();

app.Run();

DefaultTusConfiguration CreateTusConfiguration(IServiceProvider serviceProvider)
{
    var env = (IWebHostEnvironment)serviceProvider.GetRequiredService(typeof(IWebHostEnvironment));
    //File upload path
    var tusFiles = builder.Configuration.GetValue<string>("StoredFilesPath");
    var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

    return new DefaultTusConfiguration
    {
        UrlPath = "/files",
        //File storage path
        Store = new TusDiskStore(tusFiles),
        //Does metadata allow null values
        MetadataParsingStrategy = MetadataParsingStrategy.AllowEmptyValues,
        //The file will not be updated after expiration
        Expiration = new AbsoluteExpiration(TimeSpan.FromMinutes(5)),
        //Event handling (various events, meet your needs)
        Events = new Events
        {
            OnBeforeCreateAsync = ctx =>
            {
                if (!ctx.Metadata.ContainsKey("filename"))
                {
                    ctx.FailRequest("name metadata must be specified. ");
                }

                if (!ctx.Metadata.ContainsKey("filetype"))
                {
                    ctx.FailRequest("contentType metadata must be specified. ");
                }

                return Task.CompletedTask;
            },
            OnCreateCompleteAsync = ctx =>
            {
                logger.LogInformation($"Created file {ctx.FileId} using {ctx.Store.GetType().FullName}");
                return Task.CompletedTask;
            },
            OnBeforeDeleteAsync = ctx =>
            {
                // Can the file be deleted? If not call ctx.FailRequest(<message>);
                return Task.CompletedTask;
            },
            OnDeleteCompleteAsync = ctx =>
            {
                logger.LogInformation($"Deleted file {ctx.FileId} using {ctx.Store.GetType().FullName}");
                return Task.CompletedTask;
            },
            //Upload completion event callback
            OnFileCompleteAsync = async ctx =>
            {
                //Get upload file
                var file = await ctx.GetFileAsync();

                //Get upload file=
                var metadatas = await file.GetMetadataAsync(ctx.CancellationToken);

                //Get the target file name in the above file metadata
                var fileNameMetadata = metadatas["filename"];

                //The target file name is encoded in Base64, so it needs to be decoded here
                var fileName = fileNameMetadata.GetString(Encoding.UTF8);

                var extensionName = Path.GetExtension(fileName);

                //Convert the uploaded file to the actual target file
                File.Move(Path.Combine(tusFiles, ctx.FileId), Path.Combine(tusFiles, $"{ctx.FileId}{extensionName}"));
            }
        }
    };
}
