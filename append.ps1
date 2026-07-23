
$cssPath = 'c:\Users\Administrator\Desktop\PigmyPro\PigmyPro.Web\wwwroot\css\site.css'
$content = @"

/* Compact Horizontal Card Modifier */
.stat-card-compact {
    padding: 16px 20px !important;
    flex-direction: row !important;
    align-items: center !important;
}
"@
Add-Content -Path $cssPath -Value $content
