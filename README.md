<<<<<<< HEAD
# ARABULUCU
kampüs kayıp eşya bulma ve iletişim
=======
# <!-- Bu README dosyası proje hakkında genel bilgi ve geliştirici rehberi sağlar. Ekip üyeleri buradan proje yapısını, çalışma adımlarını ve branch stratejisini görebilir. -->
# ARABULUCU - Kayıp Eşya Web Uygulaması

Bu depo, çok katmanlı mimaride geliştirilecek "kayıp eşya" ilan ve iletişim uygulamasının başlangıç kodlarını içerir.

Özellikler (başlangıç):
- İlan oluşturma, listeleme, detay görüntüleme
- İlan sahibi ile iletişim (e-posta veya in-app)
- Resim yükleme (ilana fotoğraf ekleme)

Mimari:
- Presentation: ASP.NET Core MVC (.NET 10)
- Core: Domain modeller ve arayüzler
- Infrastructure: EF Core, repository implementasyonları
- Tests: Birim ve entegrasyon testleri

Geliştirme
1. Bu repoyu fork veya clone edin
2. Visual Studio 2022 veya dotnet CLI ile açın
3. Bağımlılıkları restore edin: `dotnet restore`
4. Veritabanını apply edin: `dotnet ef database update` (Infrastructure projesinde çalıştırın)

Branch stratejisi
- main: üretim hazır kod
- develop: günlük geliştirme
- feature/*: yeni özellikler

Katkıda bulunanlar için
- Öncelikle issue açın veya assign edilmemiş bir issue alın
- Her özellik için feature branch açıp PR oluşturun
## Oluşturulan dosyalar ve açıklamaları

- README.md: Proje hakkında genel bilgi ve geliştirici rehberi. (Bu dosya)
- .gitignore: Yerel derleme dosyaları, kullanıcı ayarları ve takip edilmemesi gereken geçici dosyalar için kullanılır.
- LICENSE: Projenin lisans metni (MIT lisansı örneği).
- .gitattributes: Git öznitelikleri - hangi dosya türlerinin nasıl işleneceğini belirler.
- .github/ISSUE_TEMPLATE/*.md: GitHub üzerinde yeni issue açılırken kullanılacak şablonlar (hata ve özellik talepleri).
- .github/PULL_REQUEST_TEMPLATE.md: Pull request oluştururken doldurulması gereken alanları belirten şablon.

Bu dosyalar ekip çalışması ve sürüm kontrolü için başlangıçta oluşturuldu; kod ve proje yapısı geliştikçe güncellenecektir.

## Gerekli kütüphaneler ve teknolojiler (taslak)

Bu projede kullanmanız önerilen paketler ve kütüphaneler:

- Sunucu (.NET / NuGet):
  - Microsoft.EntityFrameworkCore
  - Microsoft.EntityFrameworkCore.Sqlite (veya SQL Server: Microsoft.EntityFrameworkCore.SqlServer)
  - Microsoft.EntityFrameworkCore.Design (migrations için)
	- Microsoft.AspNetCore.SignalR (SignalR server fonksiyonları için; Presentation projesinde AddSignalR() kullandık)
  - Microsoft.AspNetCore.SignalR.Client (sunucu tarafı veya arka plan client'lar için, 8.x uyumlu sürüm)
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore (kullanıcı yönetimi için, isteğe bağlı)
  - AutoMapper.Extensions.Microsoft.DependencyInjection (DTO dönüşümleri için öneri)
  - FluentValidation.AspNetCore (girdi doğrulama için öneri)
  - Serilog.AspNetCore veya benzeri logging sağlayıcıları (isteğe bağlı)

- İstemci (JS/CSS):
  - Leaflet (harita gösterimi) — CDN veya npm
	- SignalR client (örn. @microsoft/signalr) — CDN veya npm
  - Bootstrap (veya tercih ettiğiniz CSS framework) — isteğe bağlı

Not: Paket sürümlerini .NET 10 ile uyumlu olacak şekilde seçin. Gerekli paketleri projeye eklemeyi unutmayın.

Örnek kurulum komutları (PowerShell, çözüm kökünde):

- DAL için (EF Core ve provider):
  - dotnet add DAL/ARABULUCU.DAL.csproj package Microsoft.EntityFrameworkCore --version 8.*
  - dotnet add DAL/ARABULUCU.DAL.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 8.*
  - dotnet add DAL/ARABULUCU.DAL.csproj package Microsoft.EntityFrameworkCore.Design --version 8.*

- Presentation için (SignalR, Identity vs.):
  - dotnet add src/Presentation/ARABULUCU.Web.csproj package Microsoft.AspNetCore.SignalR.Client --version 8.*
  - dotnet add src/Presentation/ARABULUCU.Web.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.*

- BLL için (opsiyonel yardımcı kütüphaneler):
  - dotnet add BLL/ARABULUCU.BLL.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection
  - dotnet add BLL/ARABULUCU.BLL.csproj package FluentValidation.AspNetCore

İstemci tarafı (npm veya CDN):

- npm kullanıyorsanız (isteğe bağlı):
  - npm init -y
  - npm install leaflet @microsoft/signalr

- CDN kullanmak isterseniz _Layout.cshtml içinde örnek linkler verilmiştir (Leaflet CSS/JS ve SignalR client).

Versiyon notu: .NET 10 ile uyumlu en güncel major sürümlerini kullanın (EF Core 8.x, SignalR 8.x). Paket sürümlerini projenizde test ederek sabitleyin.

## Tanımlanan veri modelleri (taslak)

Bu başlangıç taslağında Core projesine örnek domain modeller eklendi:

- LostItem: ilan bilgileri (başlık, açıklama, konum, resim, oluşturulma tarihi, vb.)
- Category: eşya kategorileri (ör. cüzdan, anahtar)
- LostItemFeature: ilan özellikleri (anahtar/değer) ve IsHidden bayrağı — ilan sahibi bazı özellikleri gizleyebilir
- ContactMessage: kullanıcılar arasında gönderilen iletişim mesajları (chat veya in-app mesajlar için)
- UserProfile: basit kullanıcı profili modeli (Identity ile entegrasyon tercih edildiğinde genişletilebilir)

Bu modeller, çok katmanlı mimaride Core katmanında yer alır; Infrastructure katmanında DbContext ve repository implementasyonları bulunur; Presentation katmanı MVC + SignalR hub içerir.

NOT: İstenen proje yapısına göre katman isimleri değiştirildi. Yeni önerilen katmanlar (takım talebine göre):

- ENT: Entity/POCO sınıfları (src/ENT)
- DAL: Data Access Layer (DbContext, Repositories) (src/DAL)
- BLL: Business Logic Layer (servisler, eşleştirme mantığı) (src/BLL)
- Presentation: MVC + SignalR (src/Presentation)

Projeyi Visual Studio'ya eklerken bu klasörleri kullanın; örnek scaffolding ve README dosyaları ilgili klasörlerde oluşturuldu.
>>>>>>> Ayırma
