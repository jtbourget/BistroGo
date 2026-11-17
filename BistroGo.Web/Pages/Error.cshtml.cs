using System.Diagnostics;
using BistroGo.Core.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel>? logger = null)
    {
        _logger = logger ?? NullLogger<ErrorModel>.Instance;
    }

    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var correlationId = HttpContext.Items[CorrelationConstants.PropertyName]?.ToString()
            ?? HttpContext.Request.Headers[CorrelationConstants.HeaderName].FirstOrDefault();

        _logger.LogError("Error page rendered. RequestId: {RequestId}, CorrelationId: {CorrelationId}",
            RequestId, correlationId);
    }
}
