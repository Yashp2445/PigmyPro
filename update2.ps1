$repo = 'c:\Users\Administrator\Desktop\PigmyPro\PigmyPro.Data\Repositories\DashboardRepository.cs'
$repoText = Get-Content -Raw -Path $repo -Encoding UTF8
$repoText = $repoText -replace '\(SELECT COUNT\(\*\) FROM agntmast WHERE BankID = @BankID AND brnc_code = @BranchID\) AS TotalAgents', '(SELECT COUNT(*) FROM agntmast WHERE BankID = @BankID AND brnc_code = @BranchID AND Block = 0) AS TotalAgents'
Set-Content -Path $repo -Value $repoText -Encoding UTF8

function Update-DashboardView ($file, $isBranch) {
    if (-not (Test-Path $file)) { return }
    $text = Get-Content -Raw -Path $file -Encoding UTF8
    
    $totalBranchesHtml = ""
    if (-not $isBranch) {
        $totalBranchesHtml = @"
    <div class="col-lg-2 col-md-4 col-sm-6">
        <div class="stat-card info h-100 p-3">
            <div class="stat-icon mb-2" style="font-size:1.25rem; width:36px; height:36px;"><i class="bi bi-diagram-3-fill"></i></div>
            <div class="stat-value fs-5">@Model.TotalBranches</div>
            <div class="stat-label small">Active Branches</div>
        </div>
    </div>
"@
    }

    $newCards = @"
<!-- Summary Cards -->
<div class="row g-3 mb-4">
$totalBranchesHtml
    <div class="col-lg-2 col-md-4 col-sm-6">
        <div class="stat-card warning h-100 p-3">
            <div class="stat-icon mb-2" style="font-size:1.25rem; width:36px; height:36px;"><i class="bi bi-person-badge-fill"></i></div>
            <div class="stat-value fs-5">@Model.TotalAgents</div>
            <div class="stat-label small">Total Active Agents</div>
        </div>
    </div>
    <div class="col-lg-2 col-md-4 col-sm-6">
        <div class="stat-card success h-100 p-3">
            <div class="stat-icon mb-2" style="font-size:1.25rem; width:36px; height:36px;"><i class="bi bi-wallet2"></i></div>
            <div class="stat-value fs-5">@Model.TotalAccounts.ToString("N0")</div>
            <div class="stat-label small">Total Accounts</div>
        </div>
    </div>
    <div class="col-lg-3 col-md-6">
        <div class="stat-card h-100 p-3" style="background: linear-gradient(135deg, #f59e0b 0%, #b45309 100%); border: none; color: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(245,158,11,0.2);">
            <div class="d-flex justify-content-between align-items-start mb-2">
                <div class="stat-label small fw-bold" style="color:rgba(255,255,255,0.9); text-transform: uppercase; letter-spacing: 0.5px;">Total Collection Held with Agents (Pending)</div>
                <div class="stat-icon" style="background: rgba(255,255,255,0.2); color: white; font-size:1rem; width:28px; height:28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; flex-shrink: 0; margin-left: 8px;"><i class="bi bi-cash-stack"></i></div>
            </div>
            <div class="stat-value fs-4 mb-2" style="color:white; font-weight: 700;">₹@(Model.CollectionHeld?.TotalAmount.ToString("N2") ?? "0.00")</div>
            <div class="d-flex align-items-center gap-3 small" style="color:rgba(255,255,255,0.9);">
                <div><i class="bi bi-person-fill me-1"></i>@(Model.CollectionHeld?.AgentCount ?? 0) Agents</div>
                <div><i class="bi bi-person-check-fill me-1"></i>@(Model.AcMasterData != null ? Model.AcMasterData.TotalCollectionAccounts.ToString("N0") : (Model.AccountsCollectedToday != null ? Model.AccountsCollectedToday.ToString("N0") : "0")) A/Cs</div>
            </div>
        </div>
    </div>
    <div class="col-lg-3 col-md-6">
        <div class="stat-card h-100 p-3" style="background: linear-gradient(135deg, #10b981 0%, #047857 100%); border: none; color: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(16,185,129,0.2);">
            <div class="d-flex justify-content-between align-items-start mb-2">
                <div class="stat-label small fw-bold" style="color:rgba(255,255,255,0.9); text-transform: uppercase; letter-spacing: 0.5px;">Deposited Today</div>
                <div class="stat-icon" style="background: rgba(255,255,255,0.2); color: white; font-size:1rem; width:28px; height:28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; flex-shrink: 0; margin-left: 8px;"><i class="bi bi-check-circle-fill"></i></div>
            </div>
            <div class="stat-value fs-4 mb-2" style="color:white; font-weight: 700;">₹@(Model.CollectionDeposited?.TotalAmount.ToString("N2") ?? "0.00")</div>
            <div class="d-flex align-items-center gap-3 small" style="color:rgba(255,255,255,0.9);">
                <div><i class="bi bi-person-fill me-1"></i>@(Model.CollectionDeposited?.AgentCount ?? 0) Agents</div>
            </div>
        </div>
    </div>
</div>
"@

    $text = $text -replace '(?s)<!-- Summary Cards -->.*?<!-- New Summary Metrics \(Chart \+ Table\) -->', "$newCards`r`n`r`n<!-- New Summary Metrics (Chart + Table) -->"
    $text = $text -replace '(?s)<!-- New Summary Metrics \(Chart \+ Table\) -->.*?<!-- Agent Registry Tables -->', "<!-- Agent Registry Tables -->"
    $text = $text -replace '(?s)// Import Status Chart.*?// Trend Chart', '// Trend Chart'

    Set-Content -Path $file -Value $text -Encoding UTF8
}

Update-DashboardView 'c:\Users\Administrator\Desktop\PigmyPro\PigmyPro.Web\Views\Dashboard\BankAdmin.cshtml' $false
Update-DashboardView 'c:\Users\Administrator\Desktop\PigmyPro\PigmyPro.Web\Views\Dashboard\BranchAdmin.cshtml' $true

# For SuperAdmin, just remove Total Balance and rename Field Agents to Total Active Agents, and Active Accounts to Total Accounts
$saFile = 'c:\Users\Administrator\Desktop\PigmyPro\PigmyPro.Web\Views\Dashboard\SuperAdmin.cshtml'
$saText = Get-Content -Raw -Path $saFile -Encoding UTF8
$saText = $saText -replace '(?s)<div class="col-xl-2 col-md-4">\s*<div class="stat-card info">\s*<div class="stat-icon"><i class="bi bi-wallet-fill"></i></div>\s*<div class="stat-value">₹@\(Model\.AcMasterData\?\.TotalBalance\.ToString\("N2"\) \?\? "0\.00"\)</div>\s*<div class="stat-label">Total Balance</div>\s*</div>\s*</div>', ''
$saText = $saText -replace '<div class="stat-label">Field Agents</div>', '<div class="stat-label">Total Active Agents</div>'
$saText = $saText -replace '<div class="stat-label">Active Accounts</div>', '<div class="stat-label">Total Accounts</div>'
Set-Content -Path $saFile -Value $saText -Encoding UTF8

