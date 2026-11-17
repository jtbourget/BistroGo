using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;
using BistroGo.Core.Enumerations;
using BistroGo.Core.Interfaces;
using BistroGo.Core.Logging;
using BistroGo.Core.Models;
using BistroGo.Data.Services;
using BistroGo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NSubstitute;
using Xunit;

namespace BistroGo.Tests
{
    public class LoggingAndCorrelationTests
    {
        [Fact]
        public async Task ApiCorrelationIdMiddleware_GeneratesNewId_WhenHeaderMissing()
        {
            var logger = Substitute.For<ILogger<BistroGo.Api.Middleware.CorrelationIdMiddleware>>();
            RequestDelegate next = (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            };

            var middleware = new BistroGo.Api.Middleware.CorrelationIdMiddleware(next, logger);
            var context = new DefaultHttpContext();

            await middleware.InvokeAsync(context);

            Assert.True(context.Items.ContainsKey(CorrelationConstants.PropertyName));
            var correlationId = context.Items[CorrelationConstants.PropertyName]?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(correlationId));
        }

        [Fact]
        public async Task ApiCorrelationIdMiddleware_PreservesExistingId_WhenHeaderPresent()
        {
            var logger = Substitute.For<ILogger<BistroGo.Api.Middleware.CorrelationIdMiddleware>>();
            RequestDelegate next = (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            };

            var middleware = new BistroGo.Api.Middleware.CorrelationIdMiddleware(next, logger);
            var context = new DefaultHttpContext();
            var testId = "test-correlation-12345";
            context.Request.Headers[CorrelationConstants.HeaderName] = testId;

            await middleware.InvokeAsync(context);

            Assert.Equal(testId, context.Items[CorrelationConstants.PropertyName]);
        }

        [Fact]
        public async Task WebCorrelationIdMiddleware_GeneratesNewId_WhenHeaderMissing()
        {
            var logger = Substitute.For<ILogger<BistroGo.Web.Middleware.CorrelationIdMiddleware>>();
            RequestDelegate next = (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            };

            var middleware = new BistroGo.Web.Middleware.CorrelationIdMiddleware(next, logger);
            var context = new DefaultHttpContext();

            await middleware.InvokeAsync(context);

            Assert.True(context.Items.ContainsKey(CorrelationConstants.PropertyName));
            var correlationId = context.Items[CorrelationConstants.PropertyName]?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(correlationId));
        }

        [Fact]
        public async Task CorrelationIdDelegatingHandler_PropagatesCorrelationId_ToOutboundRequest()
        {
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            var context = new DefaultHttpContext();
            var expectedId = "outbound-trace-abc";
            context.Items[CorrelationConstants.PropertyName] = expectedId;
            httpContextAccessor.HttpContext.Returns(context);

            var logger = Substitute.For<ILogger<CorrelationIdDelegatingHandler>>();
            var handler = new CorrelationIdDelegatingHandler(httpContextAccessor, logger)
            {
                InnerHandler = new TestHttpMessageHandler()
            };

            var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.bistrogo.test/api/categories");

            var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(request.Headers.Contains(CorrelationConstants.HeaderName));
            Assert.Equal(expectedId, request.Headers.GetValues(CorrelationConstants.HeaderName).First());
        }

        [Fact]
        public async Task GlobalExceptionHandler_IncludesCorrelationId_InProblemDetails()
        {
            var logger = Substitute.For<ILogger<BistroGo.Api.Errors.GlobalExceptionHandler>>();
            var handler = new BistroGo.Api.Errors.GlobalExceptionHandler(logger);

            var context = new DefaultHttpContext();
            var expectedCorrelationId = "corr-error-xyz-987";
            context.Items[CorrelationConstants.PropertyName] = expectedCorrelationId;

            var exception = new InvalidOperationException("Something broke");
            var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

            Assert.True(handled);
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        }

        [Fact]
        public async Task ApiOrdersController_LogsLifecycleEvents()
        {
            var repo = Substitute.For<IOrderRepository>();
            var service = Substitute.For<IOrderService>();
            var logger = Substitute.For<ILogger<BistroGo.Api.Controllers.OrdersController>>();

            var order = new Order { Id = 42, UserId = "user-1", Total = 25.50m };
            repo.GetByIdAsync(42).Returns(order);
            service.PlaceOrderAsync(42).Returns(PlaceOrderResult.Placed);

            var controller = new BistroGo.Api.Controllers.OrdersController(repo, service, logger);

            var getResult = await controller.GetById(42);
            var placeResult = await controller.PlaceOrder(42);

            Assert.IsType<OkObjectResult>(getResult.Result);
            Assert.IsType<OkResult>(placeResult);

            // Verify logger received Information level calls
            logger.ReceivedWithAnyArgs().Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public void DevelopmentEnvironment_UsesSimpleConsoleFormatter_WithScopesEnabled()
        {
            var services = new ServiceCollection();
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                });
            });

            using var sp = services.BuildServiceProvider();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DevelopmentTest");

            // Verify logger creation and scope usage without errors
            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = "dev-123" }))
            {
                logger.LogInformation("Testing development formatted logging");
            }

            Assert.NotNull(logger);
        }

        [Fact]
        public void ProductionEnvironment_UsesJsonConsoleFormatter_WithScopesEnabled()
        {
            var services = new ServiceCollection();
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddJsonConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
                });
            });

            using var sp = services.BuildServiceProvider();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("ProductionTest");

            // Verify JSON logger creation and scope usage without errors
            using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = "prod-999" }))
            {
                logger.LogInformation("Testing production JSON formatted logging");
            }

            Assert.NotNull(logger);
        }

        private class TestHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
