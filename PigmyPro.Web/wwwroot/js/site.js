// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function showToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type === 'error' ? 'danger' : 'success'} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;

    container.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast, { delay: 5000 });
    bsToast.show();

    toast.addEventListener('hidden.bs.toast', () => {
        toast.remove();
    });
}

document.addEventListener("DOMContentLoaded", function () {
    var sidebar = document.getElementById("sidebar");
    if (sidebar) {
        var scrollPos = sessionStorage.getItem("sidebarScrollPos");
        if (scrollPos) {
            sidebar.scrollTop = scrollPos;
        }

        sidebar.addEventListener("scroll", function () {
            sessionStorage.setItem("sidebarScrollPos", sidebar.scrollTop);
        });
    }
});

function setupTableSortAndSearch(tableId, searchInputId) {
    const table = document.getElementById(tableId);
    const searchInput = document.getElementById(searchInputId);
    if (!table) return;

    // Search functionality
    if (searchInput) {
        searchInput.addEventListener('keyup', function() {
            const term = this.value.toLowerCase();
            const rows = table.querySelectorAll('tbody tr');
            
            rows.forEach(row => {
                const text = row.textContent.toLowerCase();
                row.style.display = text.includes(term) ? '' : 'none';
            });
        });
    }

    // Sort functionality
    const headers = table.querySelectorAll('thead th.sortable');
    headers.forEach((header, index) => {
        header.style.cursor = 'pointer';
        // Ensure there's an icon for visual feedback
        if (!header.querySelector('.sort-icon')) {
            header.innerHTML += ' <i class="bi bi-arrow-down-up text-muted ms-1 sort-icon" style="font-size:0.8em"></i>';
        }

        header.addEventListener('click', () => {
            const isAsc = !header.classList.contains('asc');
            
            // Clear other headers
            headers.forEach(h => {
                h.classList.remove('asc', 'desc');
                const icon = h.querySelector('.sort-icon');
                if (icon) icon.className = 'bi bi-arrow-down-up text-muted ms-1 sort-icon';
            });

            // Set current header
            header.classList.add(isAsc ? 'asc' : 'desc');
            const icon = header.querySelector('.sort-icon');
            if (icon) icon.className = isAsc ? 'bi bi-arrow-up text-primary ms-1 sort-icon' : 'bi bi-arrow-down text-primary ms-1 sort-icon';

            const tbody = table.querySelector('tbody');
            const rows = Array.from(tbody.querySelectorAll('tr'));

            rows.sort((a, b) => {
                // Adjust index if there are hidden columns or weird spans, assuming simple tables here
                // We use header cellIndex to be safe
                const colIndex = header.cellIndex;
                let aCol = a.cells[colIndex]?.textContent.trim() || '';
                let bCol = b.cells[colIndex]?.textContent.trim() || '';

                // Try numeric sort first
                const aNum = parseFloat(aCol.replace(/[^0-9.-]+/g, ""));
                const bNum = parseFloat(bCol.replace(/[^0-9.-]+/g, ""));

                if (!isNaN(aNum) && !isNaN(bNum)) {
                    return isAsc ? aNum - bNum : bNum - aNum;
                }

                return isAsc ? aCol.localeCompare(bCol) : bCol.localeCompare(aCol);
            });

            rows.forEach(row => tbody.appendChild(row));
        });
    });
}

function setupDashboardAutoRefresh(toggleSelector, intervalMinutes = 5) {
    const toggle = document.querySelector(toggleSelector);
    if (!toggle) return;

    let refreshTimer = null;
    const intervalMs = intervalMinutes * 60 * 1000;

    // Load state from localStorage
    const isAutoRefresh = localStorage.getItem('dashboardAutoRefresh') === 'true';
    toggle.checked = isAutoRefresh;

    function handleToggle() {
        if (toggle.checked) {
            localStorage.setItem('dashboardAutoRefresh', 'true');
            refreshTimer = setInterval(() => {
                window.location.reload();
            }, intervalMs);
        } else {
            localStorage.setItem('dashboardAutoRefresh', 'false');
            if (refreshTimer) clearInterval(refreshTimer);
        }
    }

    toggle.addEventListener('change', handleToggle);

    // Initial setup
    handleToggle();
}
