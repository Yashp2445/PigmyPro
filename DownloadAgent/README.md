# PigmyPro Download Agent

This is a local background service written in Node.js that runs on client PCs. It integrates with the PigmyPro Banking Application to automatically download `PCRX.DAT` files directly to a configured local folder (e.g., `D:\PigmyDownloads` or fallback directories) without opening a browser "Save As" file picker.

## Configuration

You can configure the target download folder and port inside the `config.json` file:

```json
{
  "downloadFolder": "D:\\PigmyDownloads",
  "port": 32560
}
```

* If the directory specified in `downloadFolder` does not exist, the agent will attempt to create it.
* If writing to the folder fails or the drive (like `D:\`) does not exist, the agent will gracefully fall back to creating and writing to `%USERPROFILE%\PigmyDownloads` (typically `C:\Users\username\PigmyDownloads`).

---

## How to Package the Agent as a Standalone Windows Executable (.exe)

To deploy the agent on client machines without requiring them to install Node.js manually, you can compile the JavaScript agent into a single binary.

### Method 1: Using `pkg` (Recommended)

`pkg` is a popular open-source tool that packages your Node.js project into an executable file.

1. **Install Node.js** (only on your development machine to package it).
2. **Install the `pkg` tool globally**:
   ```bash
   npm install -g pkg
   ```
3. **Compile the agent**:
   Navigate to the `DownloadAgent` directory and run:
   ```bash
   pkg agent.js --targets node18-win-x64 --output PigmyDownloadAgent.exe
   ```
4. This will generate `PigmyDownloadAgent.exe` in the directory. You can distribute this single `.exe` file along with the `config.json` file to the client PCs.

### Method 2: Using Node.js Native Single Executable Applications (SEA)
*(Available in Node.js v20.0.0 and above)*

1. Create a preparation JSON file (e.g., `sea-config.json`):
   ```json
   {
     "main": "agent.js",
     "output": "sea-prep.blob"
   }
   ```
2. Generate the blob:
   ```bash
   node --experimental-sea-config sea-config.json
   ```
3. Copy the node executable to your target name:
   ```powershell
   copy $(where.exe node) PigmyDownloadAgent.exe
   ```
4. Inject the blob into the executable using a tool like `postject`:
   ```bash
   npx postject PigmyDownloadAgent.exe NODE_SEA_BLOB sea-prep.blob --sentinel-fuse NODE_SEA_FUSE_f1eeb08dd2912d83e25674e3d401dbd2
   ```

---

## How to Run it on Startup on Client Machines

To make sure the agent runs automatically whenever a user starts their PC:

1. Press `Win + R`, type `shell:startup`, and press Enter. This opens the Windows **Startup** folder.
2. Create a shortcut to `PigmyDownloadAgent.exe` (or a batch file starting it) and paste it into this folder.
3. Alternatively, you can register it as a Windows Service using tools like **NSSM (Non-Sucking Service Manager)**:
   ```bash
   nssm install PigmyDownloadAgent C:\path\to\PigmyDownloadAgent.exe
   nssm start PigmyDownloadAgent
   ```
