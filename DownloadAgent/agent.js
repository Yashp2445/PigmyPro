const http = require('http');
const https = require('https');
const fs = require('fs');
const path = require('path');

// Load config
let config = {
  downloadFolder: "D:\\PigmyDownloads"
};

try {
  const configPath = path.join(__dirname, 'config.json');
  if (fs.existsSync(configPath)) {
    config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
  }
} catch (err) {
  console.error("Failed to load config.json, using defaults.", err);
}

// Ensure download folder exists
function getDownloadFolder(customFolder) {
  try {
    let folder = customFolder || config.downloadFolder || "D:\\PigmyDownloads";
    if (!fs.existsSync(folder)) {
      fs.mkdirSync(folder, { recursive: true });
    }
    return folder;
  } catch (e) {
    console.error(`Cannot access or create: ${customFolder || config.downloadFolder}. Falling back to default C:\\PigmyDownloads`);
    let fallbackFolder = path.join(process.env.USERPROFILE || 'C:', 'PigmyDownloads');
    if (!fs.existsSync(fallbackFolder)) {
      fs.mkdirSync(fallbackFolder, { recursive: true });
    }
    return fallbackFolder;
  }
}

const server = http.createServer((req, res) => {
  // Set CORS headers
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
  res.setHeader('Access-Control-Allow-Private-Network', 'true');

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'POST' && req.url === '/download') {
    let body = '';
    req.on('data', chunk => {
      body += chunk;
    });
    
    req.on('end', () => {
      try {
        const payload = JSON.parse(body);
        const { token, serverUrl, fileName, folderPath } = payload;
        
        if (!token || !serverUrl || !fileName) {
          res.writeHead(400, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ success: false, error: 'Missing required parameters: token, serverUrl, fileName' }));
          return;
        }

        const targetFolder = getDownloadFolder(folderPath);
        const targetPath = path.join(targetFolder, fileName);
        
        // Request file from IIS/Web server
        const downloadUrl = `${serverUrl}/MobileImport/DownloadByToken?token=${token}`;
        console.log(`Downloading from: ${downloadUrl}`);
        
        const clientModule = downloadUrl.startsWith('https') ? https : http;
        const requestOptions = {
          rejectUnauthorized: false
        };
        
        const request = clientModule.get(downloadUrl, requestOptions, (downloadRes) => {
          if (downloadRes.statusCode !== 200) {
            res.writeHead(502, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ success: false, error: `Server returned HTTP status ${downloadRes.statusCode}` }));
            return;
          }

          const fileStream = fs.createWriteStream(targetPath);
          downloadRes.pipe(fileStream);

          fileStream.on('finish', () => {
            fileStream.close();
            console.log(`Saved file to: ${targetPath}`);
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ success: true, path: targetPath }));
          });

          fileStream.on('error', (err) => {
            console.error('File write error:', err);
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ success: false, error: `Disk write error: ${err.message}` }));
          });
        });

        request.on('error', (err) => {
          console.error('Network request error:', err);
          res.writeHead(502, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ success: false, error: `Network error fetching file: ${err.message}` }));
        });

      } catch (err) {
        console.error('Request processing error:', err);
        res.writeHead(500, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ success: false, error: err.message }));
      }
    });
  } else {
    res.writeHead(404, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ error: 'Not Found' }));
  }
});

const PORT = 32560;
server.listen(PORT, () => {
  console.log(`PigmyPro Download Agent running on http://localhost:${PORT}`);
  console.log(`Configured download folder: ${config.downloadFolder}`);
});
