# ARABULUCU

Kampüs kayıp eşya bulma ve iletişim platformu.

## Uygulamayı çalıştırma

Güncel ASP.NET Core MVC uygulaması `src/Presentation` klasöründedir. Depo kökündeki `ARABULUCU.csproj` eski şablon projesidir; onu çalıştırmayın.

Depo kökünde PowerShell açıp şu komutu çalıştırın:

```powershell
dotnet run --project .\src\Presentation\ARABULUCU.Web.csproj
```

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
