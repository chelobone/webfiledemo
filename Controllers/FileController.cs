using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Net;
using WebFileLoader.Helpers;
using WebFileLoader.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebFileLoader.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private const long MaxFileSize = 10L * 1024L * 1024L * 1024L; // 10GB, ajustar

        private readonly long _fileSizeLimit;
        private readonly ILogger<FileController> _logger;
        private readonly string[] _permittedExtensions = { ".txt", ".pdf" };
        private readonly string _targetFilePath;
        private readonly IAWSConfig _appConfiguration;
        private readonly IAWSHelper _aws3Services;

        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        public FileController(ILogger<FileController> logger, IConfiguration config, IAWSConfig appConfiguration)
        {
            _logger = logger;
            _appConfiguration = appConfiguration;

            _fileSizeLimit = config.GetValue<long>("FileSizeLimit");

            // To save physical files to a path provided by configuration:
            _targetFilePath = config.GetValue<string>("StoredFilesPath");

            // To save physical files to the temporary files folder, use:
            //_targetFilePath = Path.GetTempPath();
        }
        // GET: api/<FileController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<FileController>/name
        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            var _aws3Services = new AWSHelper("{userAccessKey}", "{userSecretId}", "", "{region}", "{s3BucketName}");
            var file = await _aws3Services.DownloadFileAsync(name);
            var image = PdfHelper.RenderPDFAsImages(file, name, _targetFilePath);

            var stream = new FileStream(image, FileMode.Open);

            return File(stream, "application/octet-stream");
        }

        // POST api/<FileController>
        [HttpPost]
        [DisableFormValueModelBinding]
        [RequestSizeLimit(MaxFileSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
        public async Task<IActionResult> Post()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File",
                    $"The request couldn't be processed (Error 1).");
                // Log error

                return BadRequest(ModelState);
            }

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            var trustedFileNameForFileStorage = string.Empty;
            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    // This check assumes that there's a file
                    // present without form data. If form data
                    // is present, this method immediately fails
                    // and returns the model error.
                    if (!MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        ModelState.AddModelError("File",
                            $"The request couldn't be processed (Error 2).");
                        // Log error

                        return BadRequest(ModelState);
                    }
                    else
                    {
                        // Don't trust the file name sent by the client. To display
                        // the file name, HTML-encode the value.
                        var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                contentDisposition.FileName.Value);
                        trustedFileNameForFileStorage = Path.GetRandomFileName();

                        // **WARNING!**
                        // In the following example, the file is saved without
                        // scanning the file's contents. In most production
                        // scenarios, an anti-virus/anti-malware scanner API
                        // is used on the file before making the file available
                        // for download or for use by other systems. 
                        // For more information, see the topic that accompanies 
                        // this sample.

                        var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                            section, contentDisposition, ModelState,
                            _permittedExtensions, _fileSizeLimit);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        using (var targetStream = System.IO.File.Create(
                            Path.Combine(_targetFilePath, trustedFileNameForFileStorage)))
                        {
                            await targetStream.WriteAsync(streamedFileContent);

                            var _aws3Services = new AWSHelper("{userAccessKey}", "{userSecretId}", "", "{region}", "{s3BucketName}");
                            var cargado = await _aws3Services.UploadFileAsync(streamedFileContent, trustedFileNameForDisplay, section.ContentType);

                            _logger.LogInformation(
                                "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                                "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                                trustedFileNameForDisplay, _targetFilePath,
                                trustedFileNameForFileStorage);
                        }
                    }
                }

                // Drain any remaining section body that hasn't been consumed and
                // read the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            GC.Collect();

            return Created(nameof(FileController), trustedFileNameForFileStorage);
        }
    }
}
