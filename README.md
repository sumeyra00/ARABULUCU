# ARABULUCU
kampüs kayıp eşya bulma ve iletişim

## Araç ilanları

Giriş yapmış kullanıcılar `/VehicleListing/Create` sayfasından araç başlığı, fiyat, kilometre ve açıklama bilgileriyle ilan oluşturabilir. Formdaki standart/açık ve aktivasyon yapılabilir gizli özellikler ayrı kutularda sunulur; seçilen özellik kimlikleri `SelectedFeatureIds` alanıyla gönderilir. Sunucu geçersiz özellik kimliklerini reddeder ve ilan ile özelliklerini `ListingFeatures` çoktan-çoğa tablosuna tek kayıtta bağlar.

Örnek özellik kataloğu araç ilanı şeması migration'ıyla eklenir. Yeni araç ilanı şeması ve katalog değişiklikleri için EF Core migration'ları `DAL/Data/Migrations` altında tutulur.
