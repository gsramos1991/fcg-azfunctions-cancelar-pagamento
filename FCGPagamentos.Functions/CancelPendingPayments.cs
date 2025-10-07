using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

public class CancelPendingPayments
{
    private readonly ILogger _logger;

    public CancelPendingPayments(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CancelPendingPayments>();
    }

    [Function("CancelPendingPayments")]
    public async Task Run([TimerTrigger("2 * * * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation($"CancelPendingPayments executada em: {DateTime.UtcNow:O}");

        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError("SqlConnectionString não configurada.");
            return;
        }

        var pendingIds = new List<Guid>();

        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            using (var selectCmd = new SqlCommand(@"SELECT PaymentId FROM Payments WHERE StatusPayment = 'PENDING'
                and datediff(minute,createdat, getdate()) > 20", conn))
            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    if (Guid.TryParse(reader["PaymentId"].ToString(), out var id))
                        pendingIds.Add(id);
                }
            }

            if (pendingIds.Count == 0)
            {
                _logger.LogInformation("Nenhum pagamento PENDING encontrado.");
                return;
            }

            _logger.LogInformation($"Encontrados {pendingIds.Count} pagamentos. Aguardando 10s...");
            await Task.Delay(10000);

            var updateSql = "UPDATE Payments SET StatusPayment = 'CANCELED' WHERE StatusPayment = 'PENDING' AND PaymentId IN (" +
                string.Join(",", pendingIds.Select((_, i) => $"@id{i}")) + ")";

            using (var updateCmd = new SqlCommand(updateSql, conn))
            {
                for (int i = 0; i < pendingIds.Count; i++)
                    updateCmd.Parameters.AddWithValue($"@id{i}", pendingIds[i]);

                var rows = await updateCmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"Atualizados {rows} registros para CANCELED.");
            }
                conn.Close();
        }
    }
}
