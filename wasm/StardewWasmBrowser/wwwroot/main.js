import { dotnet } from './_framework/dotnet.js';

const out = document.getElementById('out');
const setupContainer = document.getElementById('setup-container');
const canvas = document.getElementById('canvas');
const progressBar = document.getElementById('progress-bar');
const progressContainer = document.getElementById('progress-container');
const percentageText = document.getElementById('percentage');

let moduleAPI = null;

function getOrCreateErrorOverlay() {
    let overlay = document.getElementById('error-overlay');
    if (overlay) {
        return overlay;
    }

    overlay = document.createElement('pre');
    overlay.id = 'error-overlay';
    overlay.style.position = 'fixed';
    overlay.style.left = '12px';
    overlay.style.right = '12px';
    overlay.style.bottom = '12px';
    overlay.style.maxHeight = '45vh';
    overlay.style.overflow = 'auto';
    overlay.style.padding = '12px';
    overlay.style.margin = '0';
    overlay.style.whiteSpace = 'pre-wrap';
    overlay.style.wordBreak = 'break-word';
    overlay.style.background = 'rgba(0, 0, 0, 0.85)';
    overlay.style.color = '#ff8a80';
    overlay.style.border = '1px solid #ff5252';
    overlay.style.borderRadius = '8px';
    overlay.style.fontFamily = 'Consolas, monospace';
    overlay.style.fontSize = '13px';
    overlay.style.zIndex = '9999';
    overlay.style.display = 'none';
    document.body.appendChild(overlay);
    return overlay;
}

function showPersistentError(prefix, message) {
    const overlay = getOrCreateErrorOverlay();
    const time = new Date().toLocaleTimeString();
    overlay.textContent = `[${time}] ${prefix}: ${message}`;
    overlay.style.display = 'block';
}

function showError(err, prefix = 'Error loading WASM') {
    const message = err && err.stack ? err.stack : String(err);
    out.textContent = `${prefix}: ${message}`;
    showPersistentError(prefix, message);

    // If startup failed after the setup UI was hidden, bring it back.
    if (setupContainer) {
        setupContainer.style.display = 'block';
        setupContainer.style.opacity = '1';
    }
    if (canvas) {
        canvas.style.display = 'none';
    }
    console.error(err);
}

window.addEventListener('error', (event) => {
    showError(event.error || event.message, 'Runtime error');
});

window.addEventListener('unhandledrejection', (event) => {
    showError(event.reason || 'Unhandled promise rejection', 'Async runtime error');
});

// The rest of Emscripten/WASM logic will be injected when we implement IDBFS sync
try {
    const runtime = await dotnet
        .withApplicationArgumentsFromQuery()
        .withModuleConfig({ canvas })
        .create();

    const Module = runtime.Module || globalThis.Module;
    if (!Module || !Module.FS) {
        throw new Error('Runtime started but Module.FS is unavailable.');
    }

    moduleAPI = Module;

    out.textContent = 'WASM Engine Booted. Connecting to Cloud Assets...';
    progressContainer.style.display = 'block';

    // Start automated asset download loop
    const response = await fetch('assets/catalog.json');
    const catalog = await response.json();
    
    let chunks = [];
    let numChunks = Object.values(catalog).reduce((max, info) => {
        const endChunk = Number(info.chunk_start) + Number(info.chunk_count || 1) - 1;
        return Math.max(max, endChunk);
    }, -1) + 1;

    if (!Number.isFinite(numChunks) || numChunks <= 0) {
        throw new Error('Asset catalog appears invalid: unable to determine chunk count.');
    }
    
    for(let i = 0; i < numChunks; i++) {
        const percent = Math.round((i/numChunks) * 100);
        out.textContent = `Downloading cloud data... Chunk ${i + 1}/${numChunks}`;
        progressBar.style.width = percent + "%";
        percentageText.innerText = percent + "%";

        let res = await fetch(`assets/chunk_${i}.bin`);
        chunks[i] = await res.arrayBuffer();
    }
    
    out.textContent = 'Reconstructing File System...';
    if (!Module.FS.analyzePath('/Content').exists) Module.FS.mkdir('/Content');
    
    let entries = Object.entries(catalog);
    let totalFiles = entries.length;
    let processedFiles = 0;
    
    for (const [path, info] of entries) {
        const parts = path.split('/');
        let currentDir = '/Content';
        for(let j = 0; j < parts.length - 1; j++) {
            currentDir += '/' + parts[j];
            if (!Module.FS.analyzePath(currentDir).exists) Module.FS.mkdir(currentDir);
        }
        
        let bytesLeft = info.length;
        let pChunk = info.chunk_start;
        let pOffset = info.offset_start;
        
        let fileData = new Uint8Array(bytesLeft);
        let dstOffset = 0;
        
        while(bytesLeft > 0) {
            let chunkData = chunks[pChunk];
            let bytesToCopy = Math.min(bytesLeft, chunkData.byteLength - pOffset);
            let view = new Uint8Array(chunkData, pOffset, bytesToCopy);
            fileData.set(view, dstOffset);
            
            dstOffset += bytesToCopy;
            bytesLeft -= bytesToCopy;
            pChunk++;
            pOffset = 0;
        }
        
        Module.FS.writeFile('/Content/' + path, fileData);
        processedFiles++;
        
        if (processedFiles % 100 === 0) {
            const percent = Math.round((processedFiles/totalFiles)*100);
            out.textContent = `Finalizing game world... (${processedFiles}/${totalFiles} files)`;
            progressBar.style.width = percent + "%";
            percentageText.innerText = percent + "%";
        }
    }
    
    out.textContent = 'Launch Sequence Ready! Booting Game...';
    progressBar.style.width = "100%";
    percentageText.innerText = "100%";
    
    const runtimeConfig = typeof runtime.getConfig === 'function' ? runtime.getConfig() : null;
    const mainAssemblyName = runtimeConfig && runtimeConfig.mainAssemblyName ? runtimeConfig.mainAssemblyName : null;

    // Auto-boot sequence
    setTimeout(async () => {
        try {
            canvas.style.display = 'block';
            if (typeof runtime.runMain === 'function' && mainAssemblyName) {
                const runPromise = runtime.runMain(mainAssemblyName, []);
                setupContainer.style.opacity = '0';
                setTimeout(() => { setupContainer.style.display = 'none'; }, 800);
                await runPromise;
            } else if (typeof runtime.runMainAndExit === 'function' && mainAssemblyName) {
                const runPromise = runtime.runMainAndExit(mainAssemblyName, []);
                setupContainer.style.opacity = '0';
                setTimeout(() => { setupContainer.style.display = 'none'; }, 800);
                await runPromise;
            } else if (typeof runtime.run === 'function') {
                const runPromise = runtime.run();
                setupContainer.style.opacity = '0';
                setTimeout(() => { setupContainer.style.display = 'none'; }, 800);
                await runPromise;
            } else {
                throw new Error('No supported runtime entrypoint found (expected runMain/runMainAndExit).');
            }
        } catch (runErr) {
            showError(runErr, 'Failed to start game runtime');
        }
    }, 500);

    // Mount IDBFS for save files when available in this runtime flavor.
    const idbfs = Module.IDBFS || globalThis.IDBFS || (Module.FS.filesystems && Module.FS.filesystems.IDBFS);
    if (idbfs) {
        if (!Module.FS.analyzePath('/home/web_user').exists) Module.FS.mkdir('/home/web_user');
        Module.FS.mount(idbfs, {}, '/home/web_user');
        Module.FS.syncfs(true, (err) => {
            if(err) console.error("IDBFS init error: ", err);
            else console.log("IDBFS initialized!");
        });
    } else {
        console.warn('IDBFS is unavailable in this runtime. Save sync is disabled for this session.');
    }
    
    window.SyncSaves = () => {
        if(Module.FS) {
            Module.FS.syncfs(false, (err) => {
                if(err) console.error("Failed to sync save: ", err);
                else console.log("Save synced to IndexedDB!");
            });
        }
    };

} catch (err) {
    showError(err);
}