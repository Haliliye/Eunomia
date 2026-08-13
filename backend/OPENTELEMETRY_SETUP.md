# OpenTelemetry kurulumu

Bu ortamda NuGet'e erişimim yok (npm registry'sinin aksine), bu yüzden paket
adı/versiyonunu doğrulayamadan Program.cs'e körlemesine ekleyemiyorum. Aşağıdaki
adımları kendi ortamında (Visual Studio / dotnet CLI) çalıştırıp çıktıyı bana
verirsen, Program.cs kablolamasını güvenle yazarım.

## 1. Paketleri ekle

```powershell
cd backend/src/TodoApp.Api
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.Console
```

(MongoDB.Driver'ın kendi instrumentation paketi resmi olarak yok — Mongo çağrıları
otomatik izlenmeyecek, sadece HTTP/ASP.NET Core pipeline'ı izlenecek. Bu genelde
yeterli: dış API çağrıları (Jira/Azure DevOps) ve gelen HTTP istekleri görünür olur.)

Render'da harici bir OTLP collector'ın (Honeycomb, Grafana Cloud, Datadog vb.) varsa
onun yerine şunu da ekleyebilirsin:

```powershell
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

## 2. Bana ver

- Yukarıdaki komutların çıktısı (hangi versiyonlar eklendi)
- `TodoApp.Api.csproj`'un `<ItemGroup>` kısmının güncel hali
- Harici bir OTLP collector kullanacak mısın, yoksa sadece Console exporter (loglara
  yazan, geliştirme/deneme amaçlı) mı yeterli

Bunları verdiğinde `Program.cs`'e `AddOpenTelemetry().WithTracing(...)` kablolamasını
ekleyip zip'le teslim ederim.
