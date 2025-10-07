using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
.ConfigureServices(services =>
{
    services.AddSingleton<CancelPendingPayments>();
})
    .Build();

host.Run();
