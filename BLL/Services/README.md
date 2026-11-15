# BLL (Business Logic Layer)

Bu klasörde iş mantığı servisleri yer alır. BLL, Presentation (UI) ve DAL (Data Access) arasında yer alır.

Takım notları:
- Örnek servisler: ItemService (ilan oluşturma, eşleştirme), MatchingService (gizli özelliklere göre eşleştirme), ContactService (ileti gönderme).
- Servisler ENT tiplerini kullanır ve DAL üzerinden veri okur/yazar.
- Dependency Injection ile servisleri Presentation tarafına kaydedin.

Önerilen paketlere genelde ihtiyaç yok; ancak doğrulama için FluentValidation ekleyebilirsiniz.
