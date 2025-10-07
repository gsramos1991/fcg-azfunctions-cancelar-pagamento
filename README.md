# FCG Pagamentos — Cancelamento de Pagamentos Pendentes

Azure Function (modelo isolado, .NET 8) responsável por cancelar automaticamente pagamentos que permanecem com status `PENDING` por mais de 20 minutos no banco de dados SQL Server da plataforma FCG Pagamentos. A função é disparada por um gatilho do tipo Timer e atualiza o status dos registros para `CANCELED`, garantindo que transações antigas não fiquem presas na fila.

## Visão Geral do Fluxo
- **Gatilho**: `TimerTrigger("2 * * * * *")` executa no segundo 2 de cada minuto.
- **Consulta**: Seleciona `PaymentId` na tabela `Payments` com `StatusPayment = 'PENDING'` e `CreatedAt` há mais de 20 minutos.
- **Cancelamento**: Aguarda 10 segundos para garantir consistência e depois executa um `UPDATE` em lote para `StatusPayment = 'CANCELED'`.
- **Observabilidade**: Uso de `ILogger` para rastrear início da execução, número de registros afetados e eventuais falhas (como ausência da string de conexão).

## Pré-Requisitos
- .NET SDK 8.0+
- Azure Functions Core Tools v4 (`npm i -g azure-functions-core-tools@4 --unsafe-perm true` ou [instalador oficial](https://learn.microsoft.com/azure/azure-functions/functions-run-local))
- Ambiente de armazenamento para `AzureWebJobsStorage` (Azurite, Azure Storage Emulator ou conta de Storage no Azure)
- Instância SQL Server com a tabela `Payments` contendo, no mínimo, as colunas `PaymentId` (`uniqueidentifier`), `StatusPayment` (`varchar`) e `CreatedAt` (`datetime`)

## Configuração Local
1. **Clonar o repositório**
   ```bash
   git clone <url-do-repo>
   cd az-functions-cancelar-pagamento/FCGPagamentos.Functions
   ```
2. **Restaurar dependências**
   ```bash
   dotnet restore
   ```
3. **Configurar segredos**
   - Crie ou ajuste o arquivo `local.settings.json` (não deve ser commitado) com os valores a seguir:
     ```json
     {
       "IsEncrypted": false,
       "Values": {
         "AzureWebJobsStorage": "UseDevelopmentStorage=true",
         "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
         "SqlConnectionString": "<SUA-STRING-DE-CONEXAO-SQL>"
       }
     }
     ```
   - O valor de `SqlConnectionString` deve apontar para a base `FCG_Payments` (ou equivalente) com permissões de leitura e escrita. Evite versionar segredos; use Azure Key Vault ou variáveis de pipeline nos ambientes de build/deploy.

## Execução Local
- Inicie o host da Azure Function:
  ```bash
  func start
  ```
- A função `CancelPendingPayments` será executada automaticamente conforme o cron da trigger. Use o log para acompanhar a consulta e o número de registros atualizados.
- Para executar manualmente fora do agendamento, utilize a ferramenta [`func`](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=windows%2Ccsharp%2Cbash#start-the-runtime) com `func start --csharp` e aguarde o próximo ciclo do timer.

## Publicação & CI/CD
- O pipeline `azure-pipelines.yml` realiza:
  - Restore, build e publish do projeto `FCGPagamentos.Functions/FCGPagamentos.Functions.csproj`
  - Geração de artefato ZIP correto (com `host.json` na raiz)
  - Deploy usando `AzureFunctionApp@2` para a Function App Linux `fcg-payments-updates`
- Ajuste as variáveis de pipeline conforme o ambiente (nome da função, subscription, caminho do projeto). Segredos sensíveis (como `SqlConnectionString`) devem ser configurados como *secret variables* ou usando Azure Key Vault.

## Estrutura do Repositório
- `FCGPagamentos.Functions/Program.cs` — Configuração do Host isolado e registro de dependências.
- `FCGPagamentos.Functions/CancelPendingPayments.cs` — Implementação da Azure Function Timer.
- `FCGPagamentos.Functions/host.json` — Configuração global da Functions (telemetria e logging).
- `FCGPagamentos.Functions/local.settings.json` — Configurações locais (não publicar em produção).
- `azure-pipelines.yml` — Pipeline Azure DevOps para build/deploy.

## Observabilidade e Operação
- Logs são enviados para o Application Insights da Function App (quando configurado no Azure).
- Falhas comuns:
  - Ausência da variável `SqlConnectionString`.
  - Falha de rede/conexão com SQL Server (verificar firewall e credenciais).
  - Estrutura da tabela `Payments` divergente (ajuste a consulta conforme necessidade).

## Testes
Ainda não há testes automatizados. Recomenda-se adicionar testes de integração ou uma função mock para validar o fluxo de cancelamento em um banco de dados de homologação.

## Contribuindo
1. Crie uma branch a partir de `main`.
2. Adicione testes (quando aplicável) e atualização de documentação.
3. Abra um Pull Request descrevendo o impacto das mudanças e como validar.
