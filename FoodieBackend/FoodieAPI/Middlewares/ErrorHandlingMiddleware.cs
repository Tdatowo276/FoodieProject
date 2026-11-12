using Microsoft.AspNetCore.Mvc;
using FoodieAPI.Common;

namespace FoodieAPI.Middlewares
{
    public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                context.Response.StatusCode = 499;
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                await WriteProblemAsync(context, ex);
            }
        }

        private async Task WriteProblemAsync(HttpContext context, Exception ex)
        {
            ProblemDetails problem;

            switch (ex)
            {
                case ApiException api:
                    logger.LogWarning("{Status} {Message} ({Path})", api.StatusCode, api.Message, context.Request.Path);
                    problem = api.Errors is null
                        ? new ProblemDetails { Title = api.Message, Status = api.StatusCode }
                        : new HttpValidationProblemDetails(api.Errors) { Title = api.Message, Status = api.StatusCode };
                    break;

                case BadHttpRequestException bad:
                    logger.LogWarning(bad, "Bad request: {Path}", context.Request.Path);
                    problem = new ProblemDetails
                    {
                        Status = bad.StatusCode,
                        Title = bad.StatusCode switch
                        {
                            StatusCodes.Status413PayloadTooLarge => "Dữ liệu gửi lên quá lớn.",
                            StatusCodes.Status415UnsupportedMediaType => "Định dạng dữ liệu gửi lên không được hỗ trợ.",
                            _ => "Yêu cầu không hợp lệ."
                        }
                    };
                    break;

                default:
                    logger.LogError(ex, "Unhandled exception: {Method} {Path}", context.Request.Method, context.Request.Path);
                    problem = new ProblemDetails
                    {
                        Title = "Đã xảy ra lỗi máy chủ. Vui lòng thử lại sau.",
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = env.IsDevelopment() ? ex.ToString() : null
                    };
                    break;
            }

            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                problem, problem.GetType(), options: null, contentType: "application/problem+json");
        }
    }
}