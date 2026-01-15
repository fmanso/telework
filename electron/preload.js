// Preload script for Electron
// This runs in a sandboxed environment with access to Node.js APIs

const { contextBridge, ipcRenderer } = require('electron');

// Expose safe APIs to the renderer process if needed
contextBridge.exposeInMainWorld('electron', {
    // Add any APIs you want to expose to the Blazor app here
    // For now, we don't need any special APIs
    platform: process.platform,
    versions: {
        node: process.versions.node,
        electron: process.versions.electron
    }
});

// Log when preload script runs
console.log('Preload script loaded');
