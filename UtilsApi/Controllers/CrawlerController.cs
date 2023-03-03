using System.Net;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using UtilsApi.Models;

namespace UtilsApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class CrawlerController : ControllerBase
{
    private static int _apiTimeoutSetting = 30000; // 10000 milliseconds == 10 seconds
    private readonly ILogger<CrawlerController> _logger;

    public CrawlerController(ILogger<CrawlerController> logger)
    {
        _logger = logger;
    }

    class RawBodyObj
    {
        public string operationName { get; set; }
        public object variables { get; set; }
        public string query { get; set; }
    }

    [HttpPost(Name = "List")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BadRequestObjectResult), 400)]
    public async Task<IActionResult> List([FromBody] ListCrawlerRequest request)
    {
        if (!request.ItemsPerPage.HasValue || request.ItemsPerPage == 0)
        {
            request.ItemsPerPage = 10;
        }

        if (!request.SkipTo.HasValue)
        {
            request.SkipTo = 0;
        }
        List<ListCrawlerDataListItemResponse> items = new List<ListCrawlerDataListItemResponse>();
        try
        {
            ListCrawlerResponse firstCall = await GetData(request);
            items.AddRange(firstCall.Data.FirstOrDefault().Value.List);

            try
            {
                while (firstCall.Data.FirstOrDefault().Value.TotalCount >= request.SkipTo)
                {
                    ListCrawlerResponse nextCall = await GetData(request);
                    items.AddRange(nextCall.Data.FirstOrDefault().Value.List);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetData Exception");
            }

            _logger.LogInformation($"Run up to {request.SkipTo}");

            var a = CreateWorkbookAndFillData(items.Select(x => new ExcelRecord
            {
                Id = x.Id,
                Username = x.Username,
                Email = x.Email,
                IsActive = x.IsActive,
                Name = x.AssociatedUser?.Name,
                Source = x.AssociatedUser?.Source,
                StaffId = x.AssociatedUser?.FieldObject?.StaffId,
                AgentCode = x.AssociatedUser?.FieldObject?.AgentCode,
                SaleUserId = x.AssociatedUser?.FieldObject?.SaleUserId,
                AgentCodeName = x.AssociatedUser?.FieldObject?.AgentCodeName,
            }).ToList());

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(a)
            };
            result.Content.Headers.ContentDisposition =
                new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"Output-{DateTime.UtcNow.ToString()}.xlsx"
                };
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

            return File(a, "application/vnd.ms-excel");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List Exception");
            return BadRequest();
        }
    }

    private async Task<ListCrawlerResponse> GetData(ListCrawlerRequest request)
    {
        _logger.LogInformation($"GetData from {request.SkipTo} to {request.SkipTo + request.ItemsPerPage}");
        var restRequest = new RestRequest(request.Endpoint, Method.Post);

        foreach (var header in request.Headers)
        {
            restRequest.AddHeader(header.Key, header.Value);
        }
        restRequest.AddJsonBody(JsonConvert.DeserializeObject<RawBodyObj>(request.Body.Replace("[[perPage]]", request.ItemsPerPage.ToString()).Replace("[[skip]]", request.SkipTo.ToString())));

        var ret = await ExecuteAsync<ListCrawlerResponse>(request.Endpoint, restRequest, request.Cookies);

        request.SkipTo += request.ItemsPerPage;
        return ret;
    }

    private async Task<T?> ExecuteAsync<T>(string endpoint, RestRequest request, CookieAbc[] cookies)
    {
        _logger.LogInformation($"ExecuteAsync to {endpoint}");
        try
        {
            var client = new RestClient(endpoint);

            Uri uri = new Uri(endpoint);

            foreach (var coookie in cookies)
            {
                client.AddCookie(coookie.Key, coookie.Value, "/", uri.Host);
            }

            RestResponse response = await client.ExecuteAsync(request);
            return HandleResponse<T>(response, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExecuteAsync Error");
            return default(T);
        }
    }

    T? HandleResponse<T>(RestResponse response, string endpoint)
    {
        string? responseBody = response.Content;

        if (!response.IsSuccessful)
        {
            string message = $"Request to ({endpoint}) failed. ||| StatusCode: {response.StatusCode} ||| response: {responseBody}";
            _logger.LogError($"HandleResponse Error | {message}");
            return default(T);
        }

        if (string.IsNullOrEmpty(responseBody))
        {
            _logger.LogWarning($"HandleResponse Warning: Empty response body");
            return default(T);
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"HandleResponse Error | Response body: {responseBody}");
            return default(T);
        }
    }

    public static byte[] CreateWorkbookAndFillData(List<ExcelRecord> data)
    {
        IXLWorkbook wb = InitialiseSummaryExcelWorkBook();

        if (wb.TryGetWorksheet("Sheet 1", out IXLWorksheet ws))
        {
            ws.Cell(2, 1).InsertData(data);
        }

        var stream = new MemoryStream();
        wb.SaveAs(stream);

        return stream.ToArray();
    }

    public static IXLWorkbook InitialiseSummaryExcelWorkBook()
    {
        XLWorkbook workbook = new XLWorkbook();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Sheet 1");

        string[] columnHeader = new string[]
        {
            "Id",
            "Username",
            "Email",
            "IsActive",
            "Name",
            "Source",
            "StaffId",
            "AgentCode",
            "SaleUserId",
            "AgentCodeName"
        };

        for (int i = 0; i < columnHeader.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = columnHeader[i];
        }

        return workbook;
    }
}