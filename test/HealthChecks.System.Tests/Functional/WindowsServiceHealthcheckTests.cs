using System.Net;
using System.ServiceProcess;
using FluentAssertions;
using HealthChecks.System.Tests.Seedwork;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HealthChecks.System.Tests.Functional
{
    [Collection("execution")]
    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Windows only")]
    public class windows_service__healthcheck_should
    {
        [SkipOnPlatform(Platform.LINUX, Platform.OSX)]
        public async Task be_healthy_when_the_service_is_running()
        {
            var webhostBuilder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddWindowsServiceHealthCheck("Windows Update", s => s.StartType == ServiceStartMode.Manual);
                })
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions
                    {
                        Predicate = r => true
                    });
                });

            var server = new TestServer(webhostBuilder);
            var response = await server.CreateRequest("/health").GetAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [SkipOnPlatform(Platform.LINUX, Platform.OSX)]
        public async Task be_unhealthy_when_the_service_does_not_exist()
        {
            var webhostBuilder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddWindowsServiceHealthCheck("someservice", s => s.Status == ServiceControllerStatus.Running);
                })
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions
                    {
                        Predicate = r => true
                    });
                });

            var server = new TestServer(webhostBuilder);
            var response = await server.CreateRequest("/health").GetAsync();
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }

        [SkipOnPlatform(Platform.WINDOWS)]
        public void throw_exception_when_registering_it_in_a_no_windows_system()
        {
            var webhostBuilder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHealthChecks()
                        .AddWindowsServiceHealthCheck("dotnet", s => s.Status == ServiceControllerStatus.Running);
                })
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions
                    {
                        Predicate = r => true
                    });
                });

            var exception = Assert.Throws<PlatformNotSupportedException>(() =>
            {
                var server = new TestServer(webhostBuilder);
            });

            exception.Message.Should().Be("WindowsServiceHealthCheck can only be registered in Windows Systems");
        }
    }
}
