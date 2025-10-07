using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;

namespace FCGPagamentos.Functions.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Run_LogsError_WhenConnectionStringMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("SqlConnectionString", null);
            var factory = new TestLoggerFactory();
            var fn = new global::CancelPendingPayments(factory);

            // Act
            await fn.Run(timerInfo: null!);

            // Assert
            var entries = factory.Entries.ToList();
            Assert.Contains(entries, e => e.Level == Microsoft.Extensions.Logging.LogLevel.Information && e.Message.Contains("CancelPendingPayments executada"));
            Assert.Contains(entries, e => e.Level == Microsoft.Extensions.Logging.LogLevel.Error && e.Message.Contains("SqlConnectionString"));
        }
    }
}
