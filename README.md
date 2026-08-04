# Eunomia

Azure DevOps / Jira tarzı, takım bazlı user story ve iş akışı yönetim uygulaması.
CQRS + DDD mimarisiyle yazılmış bir C# backend ve React/TypeScript bir frontend'den oluşuyor.

> **Not:** Uygulamanın görünen adı "Eunomia" — backend'deki C# proje/namespace adları
> (`TodoApp.*`) tarihsel nedenlerle böyle kaldı; kod tabanını da yeniden adlandırmak
> ayrı, kapsamlı bir refactor gerektirir.

---

## İçindekiler

- [Özellikler](#özellikler)
- [Teknoloji yığını](#teknoloji-yığını)
- [Mimari](#mimari)
- [Kurulum](#kurulum)
- [Test](#test)
- [Test verisi (seed script)](#test-verisi-seed-script)
- [Tasarım sistemi](#tasarım-sistemi)
- [Bilinen sınırlar ve yol haritası](#bilinen-sınırlar-ve-yol-haritası)
- [Mimari notlar](#mimari-notlar)

---

## Özellikler

**Takım yönetimi**
- Takım oluşturma/düzenleme/silme, gerçek hesaplara e-posta ile davet gönderme
  (rastgele bir kimlik değil — davet edilen kişinin gerçekten kayıtlı bir hesabı olmalı)
- Davet kabul/reddet/iptal akışı, uygulama içi bildirimlerle
- Takım özeti, üye listesi, son aktivite akışı

**User story yönetimi**
- Oluşturma, düzenleme, silme, önceliklendirme, atama, story point tahmini
- Arşivleme (silme değil — geri getirilebilir)
- Backlog'da arama, filtreleme (durum/öncelik/atanan/sprint), toplu işlemler
  (çoklu seçim ile arşivleme / durum değiştirme / sprint'e taşıma)

**Board (Kanban)**
- 6 aşamalı iş akışı: `To Do → Analyze → Dev → Test → Done`, `Test`'ten `Debug`'a dallanma
- Sürükle-bırak (mouse + dokunmatik), arama/filtreleme çubuğu
- Geçersiz durum geçişleri backend tarafından reddediliyor

**Sprint planlama**
- Sprint oluşturma, başlatma, tamamlama (bir takımda aynı anda tek bir aktif sprint)
- Sprint tamamlanınca bitmemiş işler otomatik olarak backlog'a döner

**İşbirliği**
- Yorumlar (düzenleme/silme desteğiyle), canlı "@" mention autocomplete
- Atama ve mention bildirimleri (SignalR ile anlık)

**Kimlik doğrulama**
- JWT tabanlı gerçek auth, kısa ömürlü access token + döndürülebilir (rotating) refresh token
- Şifremi unuttum / sıfırlama akışı

**Diğer**
- Gündüz/gece modu, gerçek zamanlı canlı güncellemeler (SignalR), rate limiting,
  yapılandırılmış loglama (Serilog), health check endpoint'i

---

## Teknoloji yığını

| Katman | Teknoloji |
|---|---|
| Backend | C# / ASP.NET Core 8, CQRS ([MediatR](https://github.com/jbogard/MediatR)), DDD, [FluentValidation](https://docs.fluentvalidation.net/) |
| Veritabanı | MongoDB |
| Gerçek zamanlı | SignalR |
| Loglama | Serilog |
| Frontend | React 18 + TypeScript, Vite |
| Sürükle-bırak | [@dnd-kit](https://dndkit.com/) |
| Test | xUnit + Moq (unit), [Testcontainers](https://dotnet.testcontainers.org/) (entegrasyon), [Playwright](https://playwright.dev/) (E2E) |
| CI | GitHub Actions |

---

## Mimari

### Backend katmanları

```
backend/
├── src/
│   ├── TodoApp.Domain/          # Aggregate root'lar, domain event'ler, repository interface'leri
│   ├── TodoApp.Application/     # CQRS command/query handler'lar (MediatR), FluentValidation
│   ├── TodoApp.Infrastructure/  # MongoDB repository implementasyonları, JWT, e-posta/token hash'leme
│   └── TodoApp.Api/             # Controller'lar, Program.cs, SignalR hub, middleware
└── tests/
    ├── TodoApp.UnitTests/          # Mock'lu handler testleri
    └── TodoApp.IntegrationTests/   # Testcontainers ile gerçek MongoDB'ye karşı
```

Yeni bir özellik eklerken izlenen desen:

1. `Domain/<Alan>/` — aggregate root + repository interface
2. `Application/<Alan>/Commands|Queries/` — MediatR handler'lar
3. `Infrastructure/Persistence/Repositories/` — Mongo repository implementasyonu + DI kaydı
4. `Api/Controllers/` — controller

### Frontend katmanları

```
frontend/
├── src/
│   ├── api/           # axios tabanlı API çağrıları (backend DTO'larıyla eşleşir)
│   ├── types/          # TypeScript tipleri
│   ├── pages/          # Route bazlı sayfalar
│   ├── components/     # Yeniden kullanılabilir UI parçaları
│   └── styles/         # Tasarım sistemi (theme.css)
└── e2e/                # Playwright uçtan uca testleri
```

---

## Kurulum

### Seçenek A — Docker Compose (tek komut)

```bash
cp .env.example .env
# .env dosyasını aç, JWT_SECRET_KEY için 32+ karakterlik rastgele bir değer yapıştır
# (örn. openssl rand -base64 32)
docker compose up
```

Mongo, backend (`http://localhost:5000`) ve frontend (`http://localhost:5173`) tek seferde
ayağa kalkar. Bu, geliştirme/deneme içindir — production için ayrı, sertleştirilmiş
`Dockerfile`'lar gerekir ([Bilinen sınırlar](#bilinen-sınırlar-ve-yol-haritası) bölümüne bak).

### Seçenek B — Yerelde

**Backend** (MongoDB'nin `localhost:27017`'de çalıştığını varsayar):

```bash
cd backend/src/TodoApp.Api
dotnet user-secrets set "Jwt:SecretKey" "32-karakterden-uzun-rastgele-bir-deger"
cd backend
dotnet restore
dotnet run --project src/TodoApp.Api
```

**Frontend:**

```bash
cd frontend
npm install
npm run dev
```

Uygulama açılınca `/register`'dan bir hesap oluşturman gerekiyor.

---

## Test

```bash
# Unit testler (mock'lu)
cd backend && dotnet test tests/TodoApp.UnitTests

# Entegrasyon testleri (gerçek MongoDB — Testcontainers, Docker gerektirir)
cd backend && dotnet test tests/TodoApp.IntegrationTests

# E2E testler (Playwright — backend + frontend'in ayrıca çalışıyor olması gerekir)
cd frontend && npx playwright install chromium   # ilk çalıştırmada bir kere
npm run test:e2e
```

Detaylar için `backend/tests/TodoApp.IntegrationTests/README.md` ve `frontend/e2e/README.md`'ye bak.

GitHub Actions (`.github/workflows/ci.yml`) her push/PR'da backend build+testleri ve frontend
build'ini çalıştırıyor. `npm ci` için repoda bir `package-lock.json` bulunması gerekiyor —
yoksa bir kere `npm install` çalıştırıp oluşan dosyayı commit'le.

---

## Test verisi (seed script)

`scripts/seed-test-data.mjs`, gerçek API üzerinden 5 üyeli bir takım + 30 user story
oluşturuyor (kayıt, davet kabul, durum geçiş kuralları dahil — uygulamanın kendi
kurallarına uyarak):

```bash
node scripts/seed-test-data.mjs
```

Varsayılan olarak `http://localhost:5000/api`'yi hedefler (Docker Compose kurulumu).
Windows/Visual Studio'daki `https://localhost:5001` kurulumuna karşı çalıştırmak için:

```bash
API_BASE_URL=https://localhost:5001/api NODE_TLS_REJECT_UNAUTHORIZED=0 node scripts/seed-test-data.mjs
```

Node 18+ gerektirir. Script sonunda oluşturulan 5 hesabın e-posta/şifresini konsola yazdırır.

**Phase 2'nin tüm özelliklerini gösteren daha zengin bir veri seti için** `scripts/seed-phase2-demo.mjs`'i kullan — 7 kişi, aralarında bir kişi iki takımda birden, 25 user story; label'lar, checklist'ler, recurrence, dosya ekleri, zaman takibi, RBAC rolleri (Owner/Admin/Member), gecikmiş ve yaklaşan teslim tarihleri (Calendar + hatırlatmalar için), aktif sprint'ler, arşivlenmiş story'ler, mention'lı yorumlar ve kişisel görevler (biri takıma dönüştürülmüş) dahil:

```bash
node scripts/seed-phase2-demo.mjs
```

Aynı ortam değişkenleri (`API_BASE_URL`, `NODE_TLS_REJECT_UNAUTHORIZED`) burada da geçerli. Script sonunda hangi hesabın hangi takımda olduğunu ve hangi hesapla My Work/My Tasks'ın en dolu görüneceğini (Can Yıldız — iki takımda birden) konsola yazdırır.

---

## Tasarım sistemi

Sol sidebar + içerik alanı düzeni, `src/styles/theme.css`'te tanımlı bir token sistemi
(renk, tipografi, boşluk, gölge). Üç yazı tipi: başlıklar için **Space Grotesk**, gövde
metni için **Inter**, kimlik/tarih/sayı gibi veriler için **JetBrains Mono** (Google Fonts
üzerinden — offline kullanım için self-host gerekir).

Her user story satırının solunda önceliği kodlayan bir renk ve Jira/Linear tarzı kısa bir
mono ticket kodu (`PLT-4F9A1B` gibi, `src/lib/ticketCode.ts`) var — tamamen frontend'de,
takım adı + story id'sinden türetiliyor.

---

## Bilinen sınırlar ve yol haritası

Bilinçli olarak kapsam dışı bırakılanlar:

- **Production Docker imajı non-root kullanıcı ile çalışmıyor** — `backend/Dockerfile`
  artık gerçek bir multi-stage build (Render gibi platformlarda kullanılıyor), ama
  container'ın kendisi hâlâ varsayılan (root) kullanıcıyla çalışıyor. TLS sonlandırma
  ve MongoDB'nin auth/yedekleme stratejisi de barındırma platformuna (Render, Atlas
  vb.) bırakılıyor, bu repo'nun kapsamında değil.
- Modal'larda focus-trap var (`useFocusTrap`) ama kapsamlı, otomatik bir erişilebilirlik
  denetimi (axe/Lighthouse) hâlâ yapılmadı — el ile yapılan bir gözden geçirmeydi.

---

## Mimari notlar

Kod tabanına katkıda bulunurken bilinmesi faydalı olan birkaç tasarım kararı:

- **MongoDB mapping deseni:** Domain aggregate'leri (private field + computed property)
  doğrudan MongoDB.Driver ile serialize edilmiyor — driver'ın LINQ filtre çevirisi
  bunlarla başa çıkamıyor. Bunun yerine her aggregate için `Infrastructure/Persistence/Documents/`
  altında düz get/set property'li bir "document" DTO'su var, repository bu ikisi
  arasında map'liyor (`Rehydrate` factory + `ToDocument`/`ToDomain`).
- **Domain event zinciri:** Aggregate'ler (`UserStory.Assign()`, `Comment.Create()` vb.)
  domain event fırlatıyor; `DomainEventDispatchExtensions.PublishDomainEventsAsync`
  bunları MediatR üzerinden yayınlıyor. Asıl yan etki (bildirim oluşturma, aktivite kaydı)
  ayrı `INotificationHandler<DomainEventNotification<T>>` handler'larında — command
  handler'lar bundan habersiz, sadece kendi işini yapıp event fırlatıyor.
- **Optimistic concurrency:** `UserStory.Version`, sadece `UpdateDetails` ile artıyor
  (durum/öncelik/atama değişiklikleri artırmıyor — bunlar düşük çakışma riskli, tek-alanlı
  işlemler). `UpdateUserStoryCommand` bir `ExpectedVersion` taşıyor; hem handler'da hem
  Mongo filtresinde kontrol ediliyor, uyuşmazlıkta `409 Conflict` dönüyor.
- **Server-side filtreleme + arama:** `UserStoryRepository.SearchAsync` filtreleri ve
  anahtar kelime aramasını (bir text index ile) doğrudan MongoDB sorgusuna gömüyor —
  bellekte filtreleme yapmıyor. Text index kelime bazlı eşleşme yapıyor (substring değil).
- **Gerçek zamanlılık:** Tek bir SignalR hub'ı (`Api/Realtime/AppHub.cs`) hem kişisel
  bildirimleri (`user:{userId}` grubu) hem takım bazlı canlı güncellemeleri
  (`team:{teamId}` grubu, `teamUpdate` sinyali) taşıyor. `IRealtimeNotifier` arayüzü
  Application katmanında, SignalR implementasyonu Api katmanında — DDD katman sınırını
  korumak için.
- **Mongo index'leri:** `MongoIndexInitializer` uygulama her başladığında idempotent
  olarak gerekli index'leri oluşturuyor (manuel migration adımı gerekmez).

## Gerçek e-posta gönderimi

Artık gerçek SMTP e-posta gönderimi var ([MailKit](https://github.com/jstedfast/MailKit)
ile — Microsoft'un eski `System.Net.Mail.SmtpClient`'ı artık önermediği modern alternatif).
E-posta doğrulama ve şifre sıfırlama, `IEmailSender` soyutlaması üzerinden çalışıyor.

- **Yapılandırma:** `appsettings.json`'daki (ya da `.env`/ortam değişkenleriyle)
  `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:FromEmail`,
  `Smtp:FromName`, `Smtp:FrontendBaseUrl` alanları — herhangi bir standart SMTP
  sağlayıcısıyla çalışıyor (SendGrid, Mailgun, Amazon SES'in SMTP arayüzü, Gmail SMTP
  relay, şirket mail sunucusu vb.), sağlayıcıya özel kod gerekmiyor.
- **`Smtp:Host` boşsa** (varsayılan), gerçek gönderim hiç denenmiyor — bunun yerine API,
  doğrulama/sıfırlama linkini doğrudan yanıtın içinde döndürüyor (önceki "dev mode" davranışı)
  — yerel geliştirme SMTP kurmadan test edilebilsin diye. `Smtp:Host` doldurulunca bu
  otomatik olarak devre dışı kalıyor, gerçek e-posta gidiyor.
- **Gönderim hataları sessizce yutuluyor** (`try/catch` ile) — bir mail sunucusu geçici
  arızası, kayıt/şifre sıfırlama isteğinin kendisini başarısız kılmasın diye. Gerçek bir
  dağıtımda bu hataların loglanması gerekir (Serilog zaten uygulama genelinde kurulu).
- **E-posta şablonları** (`Application/Common/EmailTemplates.cs`) kasıtlı olarak basit —
  iki kısa transactional e-posta için ayrı bir templating motoru gerekmedi.

## Rol tabanlı yetkilendirme (RBAC)

Bu çalışırken gerçek bir güvenlik boşluğu ortaya çıktı ve düzeltildi: story silme, durum
değiştirme, arşivleme, atama gibi işlemlerin çoğunda **hiçbir takım üyeliği kontrolü
yoktu** — sadece geçerli bir JWT yeterliydi, o kişi ilgili takımın üyesi olmasa bile.

- `TeamRole` artık üç katmanlı: `Owner` (tam kontrol) → `Admin` (sprint yönetimi + story
  silme yapabiliyor ama takımı silemiyor/üye rollerini değiştiremiyor) → `Member`
  (günlük story işleri: oluşturma, düzenleme, durum/öncelik/atama değiştirme, yorum).
- `Team` aggregate'inde yeniden kullanılabilir kontroller: `EnsureIsMember(userId)` (temel
  yetki — artık her UserStory/Sprint/Comment komutu bunu çağırıyor) ve
  `EnsureIsOwnerOrAdmin(userId)` (yıkıcı/yönetimsel işlemler: story silme, sprint
  oluşturma/başlatma/tamamlama).
- Owner, Members sekmesinden bir üyeyi Admin yapabiliyor/geri alabiliyor
  (`PUT /api/teams/{id}/members/{userId}/role`).
- **Bilinçli olarak basit tutulan:** Owner'lık devri (ownership transfer) yok — bir
  Owner, rolünü başka bir Admin/Member'a devredemiyor, sadece takımı silebiliyor veya
  başkalarını Admin yapabiliyor. Story bazında (örn. "sadece kendi oluşturduğun story'yi
  silebilirsin") bir izin katmanı da yok — silme yetkisi tamamen role bağlı, story'nin
  kimin tarafından oluşturulduğuna bakılmıyor.

## Phase 2 — Sprint 7 (Due Dates, Reminders & Checklists)

`todo-app-backlog-phase2.md`'deki 10 epic'lik genişletme backlog'unun ilk sprint'i
tamamlandı (US-119 → US-124):

- **US-119 (Due date):** Zaten kısmen vardı — eksik kalan kısımları (board kartında ve
  backlog satırında görünme, temizlenebilme, "No due date" metni) tamamlandı.
- **US-120 (Hatırlatmalar):** Yeni bir `DueDateReminderBackgroundService`
  (`Api/BackgroundServices/`) — singleton bir `BackgroundService`, her 15 dakikada bir
  DB'yi kontrol ediyor, her kontrol için ayrı bir DI scope açıyor (repository'ler scoped
  olduğu için). Kullanıcı bazında ayarlanabilir bir hatırlatma süresi var (Settings
  sayfasında, varsayılan 24 saat). `UserStory.ReminderSentOn`, aynı teslim tarihi için
  hatırlatmanın tekrar tekrar gönderilmesini engelliyor — tarih değişince otomatik
  sıfırlanıyor. Zaten gecikmiş (overdue) bir story için hatırlatma gönderilmiyor (amaç
  teslim tarihini kaçırmayı önlemek, kaçırılmış bir şeyi hatırlatmak değil).
- **US-121 (Overdue vurgusu):** Board kartı, backlog satırı ve story detay sayfasında
  gecikmiş story'ler kırmızı vurgulanıyor — `Done` durumundakiler hiçbir zaman gecikmiş
  sayılmıyor.
- **US-122/123/124 (Checklist):** `UserStory` aggregate'ine gömülü bir `ChecklistItem`
  koleksiyonu (Comment gibi ayrı bir aggregate değil, Team içindeki TeamMember gibi —
  bir checklist item'ın story'sinden bağımsız bir anlamı yok). Ekleme, işaretleme, silme,
  yukarı/aşağı taşıma (tam bir drag-and-drop yerine basit ok butonlarıyla — daha az
  bağımlılık, daha güvenilir), board kartında "3/5" ilerleme göstergesi.
- **Kapsam dışı bırakılan (Sprint 7'nin geri kalanı için not):** Hatırlatma kontrol
  aralığı (15 dk) şu an sabit kodlanmış, yapılandırılabilir değil — gerçek bir üretim
  dağıtımında `appsettings.json`'a taşınması gerekir.

## Phase 2 — Sprint 8 (Labels & Categories)

- **US-125 (Label oluşturma/yönetme):** `Label`, `Team` aggregate'i içinde (TeamMember
  gibi — bağımsız bir yaşam döngüsü yok). Sadece owner oluşturabiliyor/silebiliyor, aynı
  isimde (büyük/küçük harf duyarsız) ikinci bir label'a izin verilmiyor. Members sayfasında
  bir "Labels" kartı olarak yönetiliyor.
- **US-126 (Story'lere uygulama):** `UserStory.LabelIds` — label'ın kendisini değil,
  sadece id referansını taşıyor (bir label'ı yeniden adlandırmak/renklendirmek her
  story'yi güncellemeyi gerektirmiyor). Herhangi bir takım üyesi ekleyip çıkarabiliyor
  (story detay sayfasında tıklanabilir rozetler).
- **US-127 (Label'a göre filtreleme):** Backlog'da mevcut durum/öncelik/atanan/sprint
  filtrelerine eklenen bir label dropdown'u — hepsi birleşebiliyor.
- **Cascade silme:** Bir label silinince, o label'ı taşıyan tüm story'lerden otomatik
  kaldırılıyor (AC'de açıkça istenen davranış).
- **Bilinçli olarak basit tutulan:** Label düzenleme (isim/renk değiştirme) backend'de
  var (`UpdateLabelCommand`) ama frontend'de bir "Edit" butonu yok — şu an sadece
  oluşturma ve silme arayüzü var; renk seçimi 7 sabit renkten biriyle sınırlı (serbest
  bir renk seçici değil).

## Phase 2 — Sprint 9 (Recurring Tasks + File Attachments)

- **US-128/129/130 (Recurring Tasks):** `UserStory.CreateNextOccurrence()` — aggregate'in
  kendisi bir sonraki occurrence'ını nasıl doğuracağını biliyor (DDD prensibi:
  mantık Domain'de, handler sadece çağırıyor). Bir story `Done` olunca
  (`ChangeUserStoryStatusCommandHandler`), recurring ise otomatik yeni bir occurrence
  oluşuyor — aynı başlık/açıklama/atanan, bir sonraki teslim tarihi frekansa göre
  hesaplanıyor (Daily/Weekly/Monthly). End date geçmişse yeni occurrence oluşturulmuyor.
  Recurring story'ler her yerde 🔁 ikonuyla işaretleniyor.
- **US-134/135/136 (File Attachments):** `IAttachmentStorage` soyutlaması — iki
  implementasyonu var, `AttachmentStorage:RootPath`'e göre değil, `R2Storage:Enabled`'a
  göre seçiliyor:
  - **`LocalDiskAttachmentStorage`** (varsayılan) — dosyaları yerel diskte saklıyor
    (`AttachmentStorage:RootPath`, varsayılan `App_Data/attachments`). Sadece metadata
    (dosya adı, boyut, tip, yükleyen, tarih) Mongo'da.
  - **`R2AttachmentStorage`** — `R2Storage:Enabled=true` olduğunda devreye giriyor,
    Cloudflare R2'ye (S3 uyumlu API, AWS SDK ile) yazıyor. Bu, tek bir kod satırına
    dokunmadan Application/Domain katmanlarını hiç etkilemeden yapılan bir değişim —
    tam da `IAttachmentStorage` soyutlamasının amacı buydu.
  - **Neden gerekliydi:** Yerel disk depolama tek bir API instance'ı için çalışır;
    birden fazla instance (load balancer arkasında) veya container yeniden
    oluşturulduğunda (mount edilen bir volume yoksa — **Render'ın ücretsiz katmanı
    dahil**) dosyalar kaybolur. R2, ücretsiz katmanında 10GB'a kadar depolama +
    egress ücreti almıyor (S3'ün aksine), bu yüzden ücretsiz bir dağıtım için
    doğal bir seçim.
  - 10MB boyut limiti ve bir dosya tipi allowlist'i (görseller + yaygın belgeler —
    çalıştırılabilir dosyalar hariç) hem client hem server tarafında kontrol ediliyor.
    Görsel/PDF dosyalar tarayıcıda inline açılıyor, diğerleri indiriliyor.
  - `App_Data/` klasörü `.gitignore`'a eklendi — yerel disk kullanılırken bile
    yüklenen dosyalar repo'ya gitmiyor.

  **R2'yi ayarlamak için:**
  1. [Cloudflare dashboard](https://dash.cloudflare.com)'da **R2 Object Storage**'a git,
     bir bucket oluştur (örn. `eunomia-attachments`)
  2. **Manage R2 API Tokens** → yeni bir API token oluştur (bu bucket için okuma+yazma
     yetkili), **Access Key ID** ve **Secret Access Key**'i kopyala
  3. Bucket'ın "S3 API" adresini kopyala — `https://<account-id>.r2.cloudflarestorage.com`
     formatında
  4. Docker Compose kullanıyorsan `.env` dosyana ekle:
     ```
     R2_ENABLED=true
     R2_SERVICE_URL=https://<account-id>.r2.cloudflarestorage.com
     R2_ACCESS_KEY_ID=<access-key-id>
     R2_SECRET_ACCESS_KEY=<secret-access-key>
     R2_BUCKET_NAME=eunomia-attachments
     ```
     Visual Studio'dan çalıştırıyorsan (Docker yerine) aynı değerleri `dotnet user-secrets`
     ile ayarla (`backend/src/TodoApp.Api` klasöründen):
     ```
     dotnet user-secrets set "R2Storage:Enabled" "true"
     dotnet user-secrets set "R2Storage:ServiceUrl" "https://<account-id>.r2.cloudflarestorage.com"
     dotnet user-secrets set "R2Storage:AccessKeyId" "<access-key-id>"
     dotnet user-secrets set "R2Storage:SecretAccessKey" "<secret-access-key>"
     dotnet user-secrets set "R2Storage:BucketName" "eunomia-attachments"
     ```
  5. `R2Storage:Enabled` false ya da bu bölüm hiç ayarlanmamışsa, hiçbir şey
     kırılmıyor — otomatik olarak yerel diske düşüyor (yerel geliştirme için hâlâ
     sıfır-konfigürasyon).

## Phase 2 — Sprint 10 (Activity & Audit Log + Time Tracking)

- **US-131/132/133 (Activity & Audit Log):** `Activity` aggregate'i (Phase 1'den zaten
  vardı — Summary sekmesindeki "Recent activity" için) genişletildi:
  - Yeni bir `ActivityType` alanı (Created/StatusChanged/Assigned/Archived/Commented) —
    US-133'ün "aksiyon tipine göre filtrele" isteği için gerekliydi, önceden sadece
    serbest metin bir `Message` vardı.
  - Yeni **Activity** sekmesi — tüm takımın aktivite akışı, sayfalanabiliyor, üyeye
    ve/veya aksiyon tipine göre filtrelenebiliyor (ikisi birlikte de kullanılabiliyor).
  - Story detay sayfasında ayrı bir **Activity** bölümü (US-131) — o story'nin kendi
    geçmişi. **Bilinçli bir tasarım kararı:** yorumlarla aynı zaman çizelgesinde
    birleştirilmedi, ayrı bir bölüm olarak tutuldu — "ne değişti"yi tarayan biri
    sohbeti eleyip geçmek zorunda kalmasın diye.
  - Yorum eklemek artık bir aktivite kaydı da oluşturuyor (daha önce oluşturmuyordu).
- **US-137/138/139 (Time Tracking):** `UserStory.EstimatedHours` (tahmin) + `TimeLogEntry`
  koleksiyonu (gerçek harcanan zaman, ChecklistItem gibi story'nin bir parçası).
  Dashboard sekmesine bir "Time report" tablosu eklendi — her story için tahmin/gerçek/fark,
  ve takım geneli toplam (US-139, mevcut dashboard'u genişletiyor). Tarih aralığı filtresi
  backend'de var (`?startDate=&endDate=`) ama frontend'de henüz bir tarih seçici yok —
  şu an her zaman "tüm zamanlar" gösteriliyor.
- **Bu sprint'i yaparken bulunan iki ek güvenlik açığı:** `GetUserStoryByIdQuery` ve
  `GetTeamActivityQuery`, `GetTeamByIdQuery`'de daha önce bulduğumuzla birebir aynı
  boşluğa sahipti — hiç üyelik kontrolü yoktu. İkisi de aynı `team.EnsureIsMember()`
  deseniyle düzeltildi. Bu, RBAC turunda tüm handler'ları taramış olsak da bazı
  **query**'lerin (sadece command'ların değil) gözden kaçmış olabileceğini gösteriyor —
  ileride yeni bir query eklerken bu kontrolü unutmamak gerekiyor.

## Phase 2 — Sprint 11 (Personal Tasks + Bulk Import/Export)

- **US-140/141/142 (Personal Task Lists):** Yeni bir `PersonalTask` aggregate'i —
  `UserStory`'nin aksine bir Team'e bağlı değil, tamamen bağımsız bir yaşam döngüsü var
  (sadece sahibi görebiliyor, hiçbir takım board'unda görünmüyor). Sidebar'a iki yeni
  bağlantı: **My Tasks** (kendi özel listeni yönet) ve **My Work** (kişisel görevlerin +
  atandığın tüm takım story'lerinin birleşik görünümü, her biri kaynağıyla etiketlenmiş).
  Bir görevi bir takıma "dönüştürme" (`ConvertPersonalTaskCommand`), sadece üyesi olduğun
  takımları hedef olarak sunuyor — hem frontend'de hem backend'de kontrol ediliyor.
- **US-146 (CSV Export):** Backlog'daki aktif filtrelerle aynı parametreleri kullanıyor,
  o an ne görüyorsan onu export ediyor. Assignee sütunu ham ID yerine **e-posta**
  gösteriyor (hem okunabilir hem tekrar import edilebilir olsun diye).
  - Genel geliştirme talimatındaki 30-arama sınırı burada geçerli değil, ama benzer bir
    ölçek kısıtı ekledim: export tek seferde en fazla 10.000 satır çekiyor.
- **US-147 (CSV Import):** İki adımlı akış — önce `/import/preview` (hiçbir şey
  oluşturmuyor, sadece ayrıştırıp doğruluyor), sonra `/import/confirm`. Sadece **Title**
  zorunlu; diğer alanlar boş/geçersizse makul varsayılanlara düşüyor (Status→ToDo,
  Priority→Medium). Geçersiz satırlar atlanıyor, tüm import'u başarısız kılmıyor.
  **Bilinçli bir basitleştirme:** AC'deki "CSV kolonlarını alanlara eşleştirme önizlemesi"
  yerine sabit bir şablon kullanıldı (export'unkiyle aynı kolonlar) — serbest bir
  column-mapping arayüzü çok daha büyük bir iş olurdu, bu ölçek için gereksiz karmaşıklık.
  Import, owner/admin ile sınırlı (sprint yönetimiyle aynı yetki seviyesi — toplu bir
  oluşturma/migrasyon işlemi, günlük story işi değil).
- **US-148 (Bulk Edit):** Mevcut toplu işlem çubuğuna atanan/öncelik/label eklendi (daha
  önce sadece durum/arşivleme/sprint vardı). Tüm toplu işlemler artık `Promise.allSettled`
  kullanıyor ve kısmi başarısızlıkları raporluyor (AC'nin istediği gibi) — önceden
  archive tek bir hata olursa tüm işlemi başarısız gösteriyordu.

## Phase 3 — Sprint'e özel görünümler, tam Kanban seti, güvenlik sertleştirmesi

- **Sprint'e özel Board/Dashboard görünümü:** İkisine de bir sprint filtresi eklendi —
  "Whole backlog/team" (varsayılan, hiçbir şey değişmedi) ya da belirli bir sprint.
- **WIP limitleri (isteğe bağlı Kanban özelliği):** Owner, her kolona bir üst sınır
  koyabiliyor (Members sayfasında). Limit aşılınca board'da kolon kırmızıya boyanıyor
  ama **hiçbir işlem engellenmiyor** — sadece görsel bir uyarı, bilinçli bir tasarım kararı.
- **Sprint burndown chart:** Dashboard'da, bir sprint seçildiğinde gösteriliyor. Sprint
  başlarken (`Start`) o anki toplam story point taahhüdü kaydediliyor ("ideal" çizginin
  kaynağı), her gün en fazla bir kez "kalan iş" anlık görüntüsü alınıyor (dashboard her
  görüntülendiğinde tembel bir şekilde tetikleniyor — ayrı bir arka plan işi gerekmiyor).
- **Şifre politikası:** Kayıt/şifre sıfırlamada artık büyük harf + rakam + sembol zorunlu.
- **HttpOnly cookie'ye tam geçiş (önemli güvenlik sertleştirmesi):** JWT ve refresh
  token'lar artık **hiçbir zaman** JS'in erişebileceği bir yere (yanıt gövdesi, `localStorage`)
  konmuyor — sadece `httpOnly`+`SameSite=Lax` cookie olarak set ediliyor. Bu, XSS
  senaryosunda bile token'ların çalınmasını engelliyor. SignalR bağlantısı da aynı
  cookie'yi kullanıyor (`?access_token=` query string hack'i artık sadece SignalR
  dışı istemciler için bir fallback).
- **WebApplicationFactory entegrasyon testleri:** Gerçek HTTP isteğiyle uçtan uca
  (`AuthFlowTests.cs`) — register→cookie set edildi mi→cookie ile yetkili istek→
  logout→cookie artık geçersiz. Daha önce hiç bu seviyede bir test yoktu.
- **Bu süreçte bulunan ek güvenlik boşlukları:** `GetTeamDashboardQuery` ve
  `GetTeamSprintsQuery`'de de daha önce bulduğumuz aynı desendeki eksiklik
  (üyelik kontrolü yok) tespit edilip düzeltildi.

## Jira / Azure DevOps'tan proje aktarımı

Sprint 11'deki CSV import'u (sabit bir şablon bekliyordu) esnek bir **kolon eşleştirme
sihirbazına** genişletildi — artık Jira'nın ya da Azure DevOps'un kendi export'unu
(hangi kolon isimleriyle gelirse gelsin) doğrudan yükleyebiliyorsun:

1. **Yükle** — CSV'yi seç, backend başlık satırını + verinin tamamını okuyor
2. **Kolon eşleştir** — Title/Description/Status/Priority/Due Date/Story Points/Labels
   alanlarının hangi kolona karşılık geldiğini seçiyorsun. Yaygın isimler (Jira'nın
   "Summary"si, Azure DevOps'un "State"i gibi) **otomatik tahmin ediliyor**, çoğu
   zaman hiç dokunmadan "Next" diyebilirsin.
3. **Değer eşleştir** (Status ya da Priority bir kolona eşlendiyse) — kaynağın kendi
   sözlüğünü (Jira'nın "In Progress"i, Azure DevOps'un "Doing"i gibi) bizim
   durum/öncelik değerlerimize eşliyorsun. Yaygın karşılıklar burada da **otomatik
   öneriliyor**.
4. **Önizleme** — hangi satırların içe aktarılacağını, hangilerinin (örn. başlık boşsa)
   atlanacağını görüyorsun, hiçbir şey henüz oluşturulmuyor.
5. **Onayla** — geçerli satırlar story olarak oluşturuluyor.

**Bilinçli olarak yapılmayan:** Atanan kişi (assignee) içe aktarılmıyor — Jira/Azure
DevOps genelde bir görünen ad/kullanıcı adı export ediyor, e-posta değil, bu yüzden
bizim hesaplarımızla güvenilir bir şekilde eşleştirilemiyor. İçe aktarılan story'ler
atanmamış geliyor, elle atama yapman gerekiyor.

## Phase 4 — İşbirliği, verimlilik, raporlama, erişilebilirlik

- **Board'da 3 nokta menüsü** — çift tıklama yerine kartın köşesindeki ⋮ butonuna
  basınca yan panel açılıyor.
- **Story bağımlılıkları/ilişkileri** — `Blocks`/`BlockedBy`/`RelatesTo`. Bir tarafa
  "Blocks" eklendiğinde karşı tarafa otomatik "BlockedBy" ekleniyor (simetrik çift).
  Takımlar arası bağlantıya izin veriliyor.
- **Story şablonları** — owner yönetiyor (Members sayfası), story oluştururken
  seçilince açıklama/öncelik/checklist otomatik uygulanıyor.
- **Genel arama (Ctrl/Cmd+K)** — tüm takımlar arasında arama, sidebar'da da bir giriş
  noktası var.
- **Toplu story oluşturma** — çok satırlı bir metin kutusu, her satır bir story.
- **Markdown desteği** — açıklama ve yorumlarda kalın/italik/liste/kod bloğu.
  `marked` + `dompurify` (XSS'e karşı temizleme) kullanıyor — **yeni bir npm
  bağımlılığı**, `npm install` gerekiyor. Mention (`@isim`) vurgusuyla birlikte
  çalışıyor.
- **Takım velocity grafiği** — tamamlanan her sprint'te o anki toplam story point
  taahhüdü ve gerçekten tamamlanan puan kaydediliyor (`Sprint.Complete()` artık bunu
  alıyor), Dashboard'da sprint'ler arası bir bar grafiği olarak gösteriliyor.
- **PDF/rapor export'u** — backend'e ağır bir PDF kütüphanesi eklemek yerine,
  tarayıcının yerleşik "Yazdır → PDF olarak kaydet" özelliğini kullanan baskıya
  optimize bir görünüm (`@media print` CSS'i, "Print / Export as PDF" butonu).
- **Erişilebilirlik (a11y) denetimi** — tüm modallerde artık gerçek bir focus-trap var
  (`useFocusTrap` hook'u — Tab/Shift+Tab modal içinde döngü yapıyor, kapanınca odak
  tetikleyen elemana geri dönüyor). Daha önce hiç bu seviyede bir kontrol yoktu.
- **Bu süreçte bulunan ek güvenlik boşlukları:** `GetTeamSprintsQuery` benzeri bir
  desende, bu turda hiçbir yeni boşluk bulunmadı — ama her yeni sorgu eklerken
  üyelik kontrolünü unutmama alışkanlığı korundu (`AddStoryLink`, `GlobalSearch`,
  `GetTeamVelocity`, `BulkCreate` hepsinde var).

## E-posta gönderimi — SMTP yerine Brevo HTTP API

Render'ın ücretsiz katmanı, kötüye kullanımı önlemek için **giden SMTP bağlantılarını
(25/465/587 portları) tamamen engelliyor** (Eylül 2025'ten beri) — bu yüzden SMTP
ayarları doğru olsa bile Render'da e-posta gönderilemiyordu (bağlantı sessizce
askıda kalıyor, hata bile vermiyor).

Çözüm: `IEmailSender` soyutlamasının yanına bir de **Brevo'nun HTTP API'si**
üzerinden gönderim yapan bir implementasyon (`BrevoApiEmailSender`) eklendi — bu,
normal HTTPS (443 portu) kullanıyor, hiçbir platformda engellenmiyor. Hangisinin
kullanılacağı otomatik seçiliyor:

- `BrevoApi:ApiKey` doluysa → Brevo API kullanılıyor (Render için önerilen)
- Boşsa → eski SMTP yoluna düşülüyor (SMTP portlarını engellemeyen ortamlar için,
  örn. yerel geliştirme)

**Not:** Bu, SMTP key'inden **farklı** bir anahtar — Brevo dashboard'da
**Settings → SMTP & API → API Keys** sekmesinden alınıyor, `Smtp:Password`'de
kullanılan SMTP key'le karıştırılmamalı.

**Ayrıca düzeltilen bir şey:** E-posta gönderimindeki hatalar daha önce
(`RegisterCommandHandler`, `RequestPasswordResetCommandHandler`,
`ResendEmailVerificationCommandHandler`'da) tamamen sessizce yutuluyordu — hiç
loglanmıyordu. Bu, gerçek bir SMTP/API hatasını "hiçbir şey olmuyor" durumundan
ayırt etmeyi imkansız kılıyordu. Üçüne de `ILogger` eklenip artık gerçek hata
mesajı loglanıyor (kullanıcıya hâlâ sessizce davranılıyor — hesap varlığını
sızdırmamak için — ama artık en azından loglardan görülebiliyor).
