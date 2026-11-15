param(
	[string]$RemoteUrl,            # Örnek: https://github.com/username/repo.git
	[string]$Branch = "main",
	[string]$CommitMessage = "Projeyi düzenleme: gereksiz dosyalar temizlendi ve proje yapısı güncellendi"
)

function ExitWithError($msg) {
	Write-Error $msg
	exit 1
}

# Git yüklü mü?
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
	ExitWithError "Git bulunamadı. Lütfen yerel makinenize Git kurun: https://git-scm.com/downloads"
}

# Çalışma dizini kontrolü
$repoRoot = Resolve-Path -Path "." -ErrorAction Stop
Write-Host "Repo kökünde çalışılıyor: $repoRoot"

# Uzaktaki origin var mı?
$hasOrigin = (& git remote) -contains 'origin'
if (-not $hasOrigin) {
	if (-not $RemoteUrl) {
		Write-Host "Uzak 'origin' bulunamadı ve RemoteUrl parametresi verilmedi. Lütfen RemoteUrl parametresiyle yeniden çalıştırın." -ForegroundColor Yellow
		Write-Host "Örnek: .\scripts\push-to-github.ps1 -RemoteUrl 'https://github.com/username/repo.git' -Branch 'main'"
		exit 1
	}
	Write-Host "Origin ekleniyor: $RemoteUrl"
	git remote add origin $RemoteUrl || ExitWithError "remote eklenemedi"
} else {
	Write-Host "Origin zaten tanımlı."
}

# Değişiklik var mı?
$changes = git status --porcelain
if (-not $changes) {
	Write-Host "Commit yapılacak değişiklik yok. Yine de branch'ı push etmek ister misiniz? (y/n)"
	$answer = Read-Host
	if ($answer -ne 'y') { Write-Host "İşlem iptal edildi."; exit 0 }
}

# Add, Commit, Push
Write-Host "Tüm değişiklikler ekleniyor..."
git add -A || ExitWithError "git add başarısız"

# Commit mesajı
if (-not $CommitMessage) { $CommitMessage = "Update" }

# Eğer commit gerekliyse
$needCommit = git status --porcelain
if ($needCommit) {
	Write-Host "Commit yapılıyor: $CommitMessage"
	git commit -m "$CommitMessage" || ExitWithError "git commit başarısız"
} else {
	Write-Host "Commit için yeni değişiklik yok."
}

Write-Host "Push yapılıyor: origin/$Branch"
# Push; HTTPS için credential yöneticisi veya PAT gerekebilir
git push -u origin $Branch || ExitWithError "git push başarısız. Eğer HTTPS kullanıyorsanız Git credential manager veya PAT (personal access token) gereklidir."

Write-Host "Push tamamlandı. GitHub'da değişiklikleri kontrol edin." -ForegroundColor Green
