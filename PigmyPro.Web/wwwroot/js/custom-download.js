(function () {
    const AGENT_PORT = 32560;
    const AGENT_URL = `http://127.0.0.1:${AGENT_PORT}/download`;

    document.addEventListener("DOMContentLoaded", function () {
        // Locate download forms using action attributes
        const forms = document.querySelectorAll('form[action*="DownloadExport"], form[action*="DownloadRedownload"]');
        
        forms.forEach(form => {
            form.addEventListener("submit", function (e) {
                // Intercept only standard submissions, don't double-intercept
                if (form.dataset.customHandling === 'true') {
                    return;
                }
                
                e.preventDefault();
                handleDownload(form);
            });
        });
    });

    async function handleDownload(form) {
        // Show loading state immediately
        Swal.fire({
            title: 'Preparing Download',
            text: 'Please wait...',
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        try {
            // Fetch token and folderPath from ASP.NET MVC Server via AJAX
            const formData = new FormData(form);
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`Server returned status ${response.status}`);
            }

            // Check if server redirected or returned HTML instead of JSON
            const contentType = response.headers.get("content-type");
            if (!contentType || !contentType.includes("application/json")) {
                // Validation failed or redirected (normal server-side behavior)
                Swal.close();
                triggerStandardDownload(form);
                return;
            }

            const data = await response.json();
            if (!data.token) {
                throw new Error('No download token received from server.');
            }

            // Send request to the local Node.js Download Agent
            let agentResponse;
            try {
                agentResponse = await fetch(AGENT_URL, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        token: data.token,
                        serverUrl: window.location.origin,
                        fileName: data.fileName || 'PCRX.DAT',
                        folderPath: data.folderPath
                    })
                });
            } catch (agentErr) {
                console.error("Local agent connection failed:", agentErr);
                // Agent is down - show fallback alert and trigger standard browser download
                await Swal.fire({
                    title: 'Agent Not Running',
                    text: 'The local Download Agent is not running. Falling back to standard browser download.',
                    icon: 'warning',
                    confirmButtonText: 'Proceed',
                    confirmButtonColor: '#3085d6',
                    allowOutsideClick: false
                });

                triggerStandardDownload(form);
                return;
            }

            if (!agentResponse.ok) {
                const errData = await agentResponse.json().catch(() => ({}));
                throw new Error(errData.error || `Agent returned status ${agentResponse.status}`);
            }

            const agentData = await agentResponse.json();
            if (agentData.success) {
                // Success confirmation alert
                await Swal.fire({
                    title: 'Success!',
                    text: `File saved successfully to: ${agentData.path}`,
                    icon: 'success',
                    confirmButtonColor: '#16a34a',
                    allowOutsideClick: false
                });
                
                // Redirect back to Dashboard
                window.location.href = '/Dashboard/Index';
            } else {
                throw new Error(agentData.error || 'Unknown agent error.');
            }
        } catch (err) {
            console.error(err);
            Swal.fire({
                title: 'Download Failed',
                text: err.message || 'An error occurred during download.',
                icon: 'error',
                confirmButtonColor: '#dc2626'
            });
        }
    }

    async function triggerStandardDownload(form) {
        Swal.fire({
            title: 'Downloading...',
            text: 'Starting standard download',
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        try {
            const formData = new FormData(form);
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData
            });

            if (!response.ok) {
                throw new Error('Failed to fetch file stream from server.');
            }

            const blob = await response.blob();
            const blobUrl = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = blobUrl;
            a.download = 'PCRX.DAT';
            document.body.appendChild(a);
            a.click();
            
            // Clean up resources
            window.URL.revokeObjectURL(blobUrl);
            document.body.removeChild(a);

            Swal.close();
            
            // Redirect back to Dashboard
            window.location.href = '/Dashboard/Index';
        } catch (err) {
            console.error(err);
            Swal.fire({
                title: 'Download Failed',
                text: 'An error occurred during standard download.',
                icon: 'error',
                confirmButtonColor: '#dc2626'
            });
        }
    }
})();
