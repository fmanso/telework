const { app, BrowserWindow, shell } = require('electron');
const path = require('path');
const { spawn } = require('child_process');

let mainWindow;
let dotnetProcess;

const DOTNET_PORT = 5123;
const isDev = process.argv.includes('--dev');

// Get the command and arguments to start the .NET application
function getDotnetCommand() {
    if (isDev) {
        // Development: use 'dotnet run' from the project directory
        return {
            command: 'dotnet',
            args: ['run', '--no-build'],
            cwd: path.join(__dirname, '..', 'TeleworkApp')
        };
    } else {
        // Production: run the compiled exe from resources
        const exePath = path.join(process.resourcesPath, 'dotnet', 'TeleworkApp.exe');
        return {
            command: exePath,
            args: [],
            cwd: path.dirname(exePath)
        };
    }
}

// Start the .NET Blazor server
function startDotnetServer() {
    return new Promise((resolve, reject) => {
        const { command, args, cwd } = getDotnetCommand();
        console.log('Starting .NET server:', command, args.join(' '));
        console.log('Working directory:', cwd);

        dotnetProcess = spawn(command, args, {
            cwd: cwd,
            shell: true,
            env: {
                ...process.env,
                ASPNETCORE_ENVIRONMENT: isDev ? 'Development' : 'Production',
                DOTNET_ENVIRONMENT: isDev ? 'Development' : 'Production'
            }
        });

        let serverStarted = false;

        dotnetProcess.stdout.on('data', (data) => {
            const output = data.toString();
            console.log('.NET:', output);
            
            // Check if the server is ready
            if (!serverStarted && (output.includes('Now listening on') || output.includes('Application started'))) {
                serverStarted = true;
                resolve();
            }
        });

        dotnetProcess.stderr.on('data', (data) => {
            console.error('.NET Error:', data.toString());
        });

        dotnetProcess.on('error', (error) => {
            console.error('Failed to start .NET process:', error);
            reject(error);
        });

        dotnetProcess.on('close', (code) => {
            console.log('.NET process exited with code:', code);
            if (!serverStarted && code !== 0 && code !== null) {
                reject(new Error(`Process exited with code ${code}`));
            }
        });

        // Timeout fallback - assume server is ready after 10 seconds
        setTimeout(() => {
            if (!serverStarted) {
                console.log('Timeout reached, assuming server is ready...');
                serverStarted = true;
                resolve();
            }
        }, 10000);
    });
}

// Stop the .NET server
function stopDotnetServer() {
    if (dotnetProcess) {
        console.log('Stopping .NET server (PID:', dotnetProcess.pid, ')...');
        
        // On Windows, we need to kill the process tree
        if (process.platform === 'win32') {
            // Kill the process tree forcefully
            try {
                spawn('taskkill', ['/pid', String(dotnetProcess.pid), '/f', '/t'], { 
                    shell: true,
                    detached: true,
                    stdio: 'ignore'
                });
            } catch (e) {
                console.error('Error killing process:', e);
            }
            
            // Also kill any process using our port
            try {
                spawn('cmd', ['/c', 'for /f "tokens=5" %a in (\'netstat -aon ^| findstr :5123 ^| findstr LISTENING\') do taskkill /F /PID %a'], {
                    shell: true,
                    detached: true,
                    stdio: 'ignore'
                });
            } catch (e) {
                console.error('Error killing port process:', e);
            }
        } else {
            dotnetProcess.kill('SIGTERM');
            setTimeout(() => {
                if (dotnetProcess) {
                    dotnetProcess.kill('SIGKILL');
                }
            }, 2000);
        }
        
        dotnetProcess = null;
    }
}

// Create the main browser window
function createWindow() {
    mainWindow = new BrowserWindow({
        width: 1400,
        height: 900,
        minWidth: 1024,
        minHeight: 768,
        title: 'Telework Tracker',
        icon: path.join(__dirname, 'icon.ico'),
        webPreferences: {
            nodeIntegration: false,
            contextIsolation: true,
            preload: path.join(__dirname, 'preload.js')
        },
        autoHideMenuBar: true,
        show: false // Don't show until ready
    });

    // Load the Blazor app
    mainWindow.loadURL(`http://localhost:${DOTNET_PORT}`);

    // Show window when ready
    mainWindow.once('ready-to-show', () => {
        mainWindow.show();
    });

    // Handle external links - open in default browser
    mainWindow.webContents.setWindowOpenHandler(({ url }) => {
        shell.openExternal(url);
        return { action: 'deny' };
    });

    // Handle window close
    mainWindow.on('closed', () => {
        mainWindow = null;
    });

    // Open DevTools in development
    if (isDev) {
        mainWindow.webContents.openDevTools();
    }
}

// App ready event
app.whenReady().then(async () => {
    try {
        console.log('Starting Telework Tracker...');
        console.log('Development mode:', isDev);
        await startDotnetServer();
        console.log('.NET server started successfully');
        createWindow();
    } catch (error) {
        console.error('Failed to start application:', error);
        app.quit();
    }
});

// Quit when all windows are closed
app.on('window-all-closed', () => {
    stopDotnetServer();
    app.quit();
});

// Clean up on app quit
app.on('before-quit', () => {
    stopDotnetServer();
});

// Handle activation (macOS)
app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
        createWindow();
    }
});

// Handle uncaught exceptions
process.on('uncaughtException', (error) => {
    console.error('Uncaught exception:', error);
    stopDotnetServer();
});
