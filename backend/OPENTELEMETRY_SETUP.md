# OpenTelemetry — kurulum tamamlandı

ASP.NET Core + HttpClient instrumentation, `Program.cs`'e kablolandı. Varsayılan
olarak Console exporter kullanıyor (loglara trace yazıyor, ekstra altyapı gerekmiyor).

## Gerçek bir collector'a (Honeycomb, Grafana Cloud, Datadog vb.) göndermek istersen

`appsettings.json`'daki (ya da Render'da environment variable olarak) `Otel:OtlpEndpoint`'i
doldur:

```powershell
# Render'da environment variable olarak:
Otel__OtlpEndpoint = https://senin-collector-adresin:4317
```

Bu değer boşsa (varsayılan), otomatik olarak Console exporter'a düşer — hiçbir şey
kırılmaz, sadece traceleri loglara yazar.
