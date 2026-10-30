# ARABULUCU

Kampüs kayıp eşya bulma ve iletişim platformu.

## Araç ilanları

Giriş yapmış kullanıcılar `/VehicleListing/Create` sayfasından araç başlığı, fiyat, kilometre ve açıklama bilgileriyle ilan oluşturabilir. Formdaki standart/açık ve aktivasyon yapılabilir gizli özellikler ayrı kutularda sunulur; seçilen özellik kimlikleri `SelectedFeatureIds` alanıyla gönderilir. Sunucu geçersiz özellik kimliklerini reddeder ve ilan ile özelliklerini `ListingFeatures` çoktan-çoğa tablosuna tek kayıtta bağlar.

Örnek özellik kataloğu araç ilanı şeması migration'ıyla eklenir. Yeni araç ilanı şeması ve katalog değişiklikleri için EF Core migration'ları `DAL/Data/Migrations` altında tutulur.

## Uygulamayı çalıştırma

Güncel ASP.NET Core MVC uygulaması `src/Presentation` klasöründedir. Depo kökündeki `ARABULUCU.csproj` eski şablon projesidir; onu çalıştırmayın.

Depo kökünde PowerShell açıp şu komutu çalıştırın:

```powershell
dotnet run --project .\src\Presentation\ARABULUCU.Web.csproj
```

Uygulama başlangıçta PostgreSQL migration'larını çalıştırır. Bağlantı dizesini depoya koymak yerine User Secrets'a kaydedin:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<Supabase pooler host>;Port=5432;Database=postgres;Username=<Supabase pooler user>;Password=<database password>;SSL Mode=Require;Trust Server Certificate=true" --project .\src\Presentation\ARABULUCU.Web.csproj
dotnet run --project .\src\Presentation\ARABULUCU.Web.csproj
```

Supabase Dashboard'da **Connect** bölümünden **Session pooler** bağlantı bilgilerini alın. Projenin doğrudan `db.<project-ref>.supabase.co` veritabanı adresi yalnızca IPv6 DNS kaydı döndürebilir; internet bağlantınız IPv6 desteklemiyorsa bu adres çözümlenemez. Bu durumda doğrudan bağlantı yerine Session pooler host, kullanıcı adı ve port değerlerini kullanın. Parolayı kaynak koda veya sohbete eklemeyin.

Visual Studio kullanıyorsanız `ARABULUCU.slnx` dosyasını açın ve `ARABULUCU.Web` projesini başlangıç projesi olarak seçin.

## Şifre sıfırlama e-postası

Şifre sıfırlama bağlantılarının test edilmesi için Mailtrap Email Testing'de bir inbox oluşturun ve inbox'ın SMTP credentials bölümündeki host, port, kullanıcı adı ve parolayı alın. Ayarları proje User Secrets alanına girin; parolayı depodaki `appsettings.json` dosyasına yazmayın:

```powershell
dotnet user-secrets set "Email:Smtp:Host" "<Mailtrap SMTP host>" --project .\src\Presentation\ARABULUCU.Web.csproj
dotnet user-secrets set "Email:Smtp:Port" "<Mailtrap SMTP port>" --project .\src\Presentation\ARABULUCU.Web.csproj
dotnet user-secrets set "Email:Smtp:UserName" "<Mailtrap SMTP username>" --project .\src\Presentation\ARABULUCU.Web.csproj
dotnet user-secrets set "Email:Smtp:Password" "<Mailtrap SMTP password>" --project .\src\Presentation\ARABULUCU.Web.csproj
dotnet user-secrets set "Email:Smtp:FromAddress" "no-reply@example.test" --project .\src\Presentation\ARABULUCU.Web.csproj
dotnet user-secrets set "Email:Smtp:FromName" "ARABULUCU" --project .\src\Presentation\ARABULUCU.Web.csproj
dotnet run --project .\src\Presentation\ARABULUCU.Web.csproj
```

Mailtrap test inbox'ına gönderilen mesajları Mailtrap panelinde görebilirsiniz; Email Testing mesajları gerçek kullanıcılara teslim etmez. Gerçek e-posta teslimi için daha sonra gerçek bir SMTP sağlayıcısı yapılandırın. E-posta ayarları eksik veya hatalıysa gönderim hatası sunucu günlüğüne kaydedilir.
