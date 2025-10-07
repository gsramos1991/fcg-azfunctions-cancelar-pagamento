# FCG Pagamentos — Azure Function de Cancelamento Automático

Azure Function (modelo isolado, .NET 8) responsável por cancelar automaticamente pagamentos que permanecem com status `PENDING` por mais de 20 minutos no banco SQL Server da plataforma FCG Pagamentos. A execução é agendada por `TimerTrigger` e realiza atualização em lote para `CANCELED`, evitando que transações fiquem presas na fila.

## 🚀 Features Implementadas

### 🎮 Funcionalidades de Negócio
- Cancelamento automático de pagamentos `PENDING` após 20 minutos
- Atualização em lote com espera breve para consistência (10s)
- Execução agendada por `TimerTrigger` (cron: `2 * * * * *`)

### 🏗️ Arquitetura & Padrões
- Worker isolado Azure Functions em .NET 8
- Injeção de dependências mínima via `HostBuilder` (DI nativo)
- Acesso a dados com `Microsoft.Data.SqlClient`
- Configuração via variáveis de ambiente e `local.settings.json`

## 🛠️ Bibliotecas e Componentes

### ⏱️ Gatilho (Timer)
```csharp
[Function("CancelPendingPayments")]
public async Task Run([TimerTrigger("2 * * * * *")] TimerInfo timerInfo) { /* ... */ }
```
- Agendamento no segundo 2 de cada minuto
- Sem endpoints HTTP (função não expõe Swagger)

### 📝 Logging
```csharp
public CancelPendingPayments(ILoggerFactory factory)
{
    _logger = factory.CreateLogger<CancelPendingPayments>();
}
```
- Logs estruturados com `ILogger`
- Suporte opcional a Application Insights (configuração em `host.json`)

### 🗄️ Acesso a Dados (SQL Server)
```csharp
using var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString"));
await conn.OpenAsync();
```
- `Microsoft.Data.SqlClient` para consultas e atualização em lote
- Seleção de `PaymentId` com filtro de tempo e status

## 🏗️ Arquitetura da Solução

```
FCG Pagamentos Functions
├── FCGPagamentos.Functions/           # Projeto Azure Functions (isolado)
│   ├── CancelPendingPayments.cs       # Função Timer (regra de cancelamento)
│   ├── Program.cs                     # Bootstrap do host e DI
│   ├── host.json                      # Telemetria e logging
│   └── local.settings.json            # Config local (não versionar)
│
├── FCGPagamentos.Functions.Tests/     # Testes
│   ├── UnitTest1.cs                   # Cenários de logging/erros
│   └── TestLogging.cs                 # Utilitários de logger em memória
│
└── azure-pipelines.yml                # Pipeline de CI/CD (build e deploy)
```

## 🚀 Como Executar

### Pré-requisitos
- .NET 8 SDK
- Azure Functions Core Tools v4
- Armazenamento para `AzureWebJobsStorage` (Azurite ou Azure Storage)
- SQL Server com tabela `Payments`

### 1. Clonar o Repositório
```bash
git clone <url-do-repo>
cd fcg-azfunctions-cancelar-pagamento/FCGPagamentos.Functions
```

### 2. Configurar `local.settings.json`
Crie/edite o arquivo (não versionar):
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "Server=localhost;Database=FCG_Payments;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### 3. Restaurar e Rodar Localmente
```bash
dotnet restore
func start
```
A função `CancelPendingPayments` executa conforme o cron. Acompanhe os logs no console.

## 📦 CI/CD (Azure DevOps)

O pipeline `azure-pipelines.yml`:
- Faz restore, build e publish do projeto `FCGPagamentos.Functions`
- Gera artefato `.zip` com `host.json` na raiz (validação automática)
- Realiza deploy usando `AzureFunctionApp@2` para `functionAppLinux`

Variáveis principais (ajuste conforme ambiente):
- `azureSubscription`: Service connection ARM
- `functionAppName`: Nome da Function App no Azure
- `relFunctionProject`: Caminho do `.csproj`

Segredos (ex.: `SqlConnectionString`) devem ir como secret variables ou via Azure Key Vault.

## 🔎 Notas de Observabilidade
- `host.json` já possui amostragem para Application Insights configurada
- Para ativar AI, adicione os pacotes recomendados (comentados no `.csproj`) e configure a instrumentation key/connection string no ambiente

## ✅ Testes
```bash
dotnet test
```
O projeto `FCGPagamentos.Functions.Tests` valida cenários de logging e ausência de connection string.

## 💻 Idealizadores do Projeto (Discord)
- Clovis Alceu Cassaro (cloves_93258)
- Gabriel Santos Ramos (_gsramos)
- Júlio César de Carvalho (cesarsoft)
- Marco Antonio Araujo (_marcoaz)
- Yasmim Muniz Da Silva Caraça (yasmimcaraca)

