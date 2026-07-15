$interface_path = 'c:\Users\Administrator\Desktop\PigmyPro\PigmyPro.Data\Interfaces\IDashboardRepository.cs'
$text = Get-Content -Raw -Path $interface_path -Encoding UTF8
$text = $text -replace 'DateTime dateFrom, DateTime dateTo, ', ''
$text = $text -replace ', DateTime dateFrom, DateTime dateTo', ''
$text = $text -replace 'DateTime dateFrom, DateTime dateTo', ''
Set-Content -Path $interface_path -Value $text -Encoding UTF8

$repo_path = 'c:\Users\Administrator\Desktop\PigmyPro\PigmyPro.Data\Repositories\DashboardRepository.cs'
$text = Get-Content -Raw -Path $repo_path -Encoding UTF8
$text = $text -replace 'DateTime dateFrom, DateTime dateTo, ', ''
$text = $text -replace ', DateTime dateFrom, DateTime dateTo', ''
$text = $text -replace 'DateTime dateFrom, DateTime dateTo', ''
$text = $text -replace 'WHERE CAST\(Date AS DATE\) >= @DateFrom AND CAST\(Date AS DATE\) <= @DateTo\s*\{bankFilter\}', 'WHERE 1=1 {bankFilter}'
$text = $text -replace 'WHERE CAST\(Date AS DATE\) >= @DateFrom AND CAST\(Date AS DATE\) <= @DateTo\s*\{branchFilterTrn\}', 'WHERE 1=1 {branchFilterTrn}'
$text = $text -replace 'WHERE CAST\(Date AS DATE\) >= @DateFrom AND CAST\(Date AS DATE\) <= @DateTo', 'WHERE 1=1'
$text = $text -replace 'AND CAST\(Date AS DATE\) >= @DateFrom AND CAST\(Date AS DATE\) <= @DateTo\s*\{branchFilterTrn\}', '{branchFilterTrn}'
$text = $text -replace 'AND CAST\(Date AS DATE\) >= @DateFrom AND CAST\(Date AS DATE\) <= @DateTo', ''

$text = $text -replace '(?s)DateFrom = dateFrom\.Date,\s*DateTo = dateTo\.Date,?\s*', ''
$text = $text -replace '(?s),\s*DateFrom = dateFrom\.Date,\s*DateTo = dateTo\.Date', ''
Set-Content -Path $repo_path -Value $text -Encoding UTF8

$controller_path = 'c:\Users\Administrator\Desktop\PigmyPro\PigmyPro.Web\Controllers\DashboardController.cs'
$text = Get-Content -Raw -Path $controller_path -Encoding UTF8
$text = $text -replace '(?s)var from = new DateTime\(DateTime\.Today\.Year, DateTime\.Today\.Month, 1\);\s*var to = DateTime\.Today;\s*', ''
$text = $text -replace 'from, to, ', ''
$text = $text -replace ', from, to', ''
Set-Content -Path $controller_path -Value $text -Encoding UTF8

foreach ($view in @('SuperAdmin', 'BankAdmin', 'BranchAdmin')) {
    $view_path = "c:\Users\Administrator\Desktop\PigmyPro\PigmyPro.Web\Views\Dashboard\$view.cshtml"
    $text = Get-Content -Raw -Path $view_path -Encoding UTF8
    $text = $text.Replace('Period Total Collection', 'Total Collection')
    Set-Content -Path $view_path -Value $text -Encoding UTF8
}
